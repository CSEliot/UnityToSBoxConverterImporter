using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace ImportUnityPackage;

public sealed class UnityPackageWindow : Widget
{
	UnityArchive archive;
	ImportCatalog catalog;
	ImportPlan plan;
	UnityAsset describedAsset;
	CancellationTokenSource cancellation;
	Task<UnityArchive> readTask;
	Task<ImportResult> importTask;
	ImportPreparationResult preparation;
	ImportCompletion completion;
	string reportJson;
	string[] completionInformation = Array.Empty<string>();
	readonly Button exportReport;
	readonly Button showReport;
	volatile ImportProgress latestProgress;
	bool closeWhenCancelled;
	readonly Button browse;
	readonly Button browseFolder;
	readonly Button confirm;
	readonly Button cancel;
	readonly Button reimport;
	readonly Button done;
	readonly Label packageLabel;
	readonly Label status;
	readonly Label stageLabel;
	readonly Label selection;
	readonly Checkbox additionalTerrain;
	readonly Checkbox importScenes;
	readonly Button selectAll;
	readonly Button selectNone;
	readonly Button expandAll;
	readonly Button collapseAll;
	readonly IconButton expandLayer;
	readonly IconButton collapseLayer;
	readonly Button advancedToggle;
	readonly Widget advanced;
	readonly Label details;
	readonly UnityAssetTree tree;
	readonly Widget progressBar;
	bool Busy => readTask != null || importTask != null;
	ImportOptions Options => ImportOptions.Auto( additionalTerrain.Value, importScenes.Value );

	public UnityPackageWindow() : base( null, true )
	{
		WindowTitle = "Import Unity Package";
		var assetsDirectory = Project.Current?.GetAssetsPath();
		if ( !string.IsNullOrEmpty( assetsDirectory ) )
		{
			// Cleanup of stale staging folders must never stop the window from opening.
			try { foreach ( var warning in ImportWorkspace.CleanupAbandoned( assetsDirectory ) ) Log.Warning( warning ); }
			catch ( Exception ex ) when ( ex is IOException or UnauthorizedAccessException ) { Log.Warning( $"Could not clean up earlier temporary import files: {ex.Message}" ); }
		}
		SetWindowIcon( "move_to_inbox" );
		Size = new Vector2( 760, 680 );
		MinimumSize = new Vector2( 580, 500 );
		Layout = Layout.Column();
		Layout.Margin = 16;
		Layout.Spacing = 10;
		var fileRow = Layout.AddRow();
		fileRow.Spacing = 8;
		packageLabel = fileRow.Add( new Label( "Choose a .unitypackage file" ), 1 );
		browse = fileRow.Add( new Button( "Browse…" ) );
		browse.Clicked = Browse;
		browseFolder = fileRow.Add( new Button( "Browse folder…" ) );
		browseFolder.Clicked = BrowseFolder;
		var contents = Layout.AddRow();
		contents.Spacing = 8;
		contents.Add( new Label( "Package contents" ), 1 );
		expandAll = contents.Add( new Button( "Expand all" ) );
		collapseAll = contents.Add( new Button( "Collapse all" ) );
		expandAll.Clicked = () => tree.SetExpanded( true );
		collapseAll.Clicked = () => tree.SetExpanded( false );
		collapseLayer = contents.Add( new IconButton( "remove", () => tree.ChangeExpandedLayer( false ) ) { ToolTip = "Collapse one layer" } );
		expandLayer = contents.Add( new IconButton( "add", () => tree.ChangeExpandedLayer( true ) ) { ToolTip = "Expand one layer" } );
		var shortcuts = Layout.AddRow();
		shortcuts.Spacing = 8;
		selectAll = shortcuts.Add( new Button( "Select all" ) );
		selectNone = shortcuts.Add( new Button( "Clear" ) );
		shortcuts.AddStretchCell();
		selectAll.Clicked = () => SetSelection( a => a.Kind != UnityAssetKind.Unsupported );
		selectNone.Clicked = () => SetSelection( _ => false );
		var instructions = Layout.Add( new Label( "Select files or folders. Required materials and textures are included automatically; original sources are kept." ) );
		instructions.WordWrap = true;
		tree = Layout.Add( new UnityAssetTree( this, () => plan, RefreshSelection, DescribeAsset ), 1 );
		tree.MinimumHeight = 200;
		details = Layout.Add( new Label( "Select a row to see its output and dependency details." ) );
		details.WordWrap = true;
		selection = Layout.Add( new Label( "No package loaded." ) );
		selection.WordWrap = true;
		advancedToggle = Layout.Add( new Button( "Advanced ▸" ) );
		advanced = Layout.Add( new Widget( this ) );
		advanced.Layout = Layout.Column();
		additionalTerrain = advanced.Layout.Add( new Checkbox( "Also generate terrain materials (TMAT)" ) );
		additionalTerrain.ToolTip = "Adds TMAT output while keeping VMATs for models. Unity terrain layers always generate TMATs. This does not recreate Unity terrain.";
		additionalTerrain.StateChanged += _ => RefreshSelection();
		importScenes = advanced.Layout.Add( new Checkbox( "Also import Unity scenes (.unity → .scene)" ) );
		importScenes.ToolTip = "Writes an s&box scene for each selected Unity scene after its prefabs, models and materials are generated. Lights, cameras, skybox and fog are approximated; scripts, terrain, lightmaps and post-processing are not converted.";
		importScenes.StateChanged += _ => RefreshSelection();
		advanced.Visible = false;
		advancedToggle.Clicked = () => { advanced.Visible = !advanced.Visible; advancedToggle.Text = advanced.Visible ? "Advanced ▾" : "Advanced ▸"; };
		var scope = Layout.Add( new Label( "Custom shader appearance, scripts, terrain and lightmaps are not reproduced. Further conversion issues appear in the import report." ) );
		scope.WordWrap = true;
		stageLabel = Layout.Add( new Label( "" ) { Visible = false } );
		stageLabel.SetStyles( "color: #7fce83; font-weight: bold;" );
		progressBar = Layout.Add( new Widget( this ) { FixedHeight = 16 } );
		progressBar.OnPaintOverride = PaintProgress;
		status = Layout.Add( new Label( "Destination: original package folders under Assets/Imported." ) );
		status.WordWrap = true;
		var buttons = Layout.AddRow();
		buttons.Spacing = 8;
		showReport = buttons.Add( new Button( "Details" ) { Visible = false } );
		showReport.Clicked = ShowDetails;
		exportReport = buttons.Add( new Button( "Export report" ) { Visible = false } );
		exportReport.Clicked = ExportReport;
		reimport = buttons.Add( new Button( "Reimport" ) { Visible = false } );
		reimport.Clicked = Import;
		buttons.AddStretchCell();
		cancel = buttons.Add( new Button( "Cancel" ) );
		cancel.Clicked = Cancel;
		confirm = buttons.Add( new Button.Primary( "Confirm import" ) );
		confirm.Clicked = Import;
		done = buttons.Add( new Button.Primary( "Done" ) { Visible = false } );
		done.Clicked = () => Close();
		RefreshSelection();
	}

	public void Browse()
	{
		if ( Busy ) return;
		var dialog = new FileDialog( this ) { Title = "Import Unity Package", DefaultSuffix = ".unitypackage" };
		dialog.SetFindFile();
		dialog.SetNameFilter( "Unity Package (*.unitypackage)" );
		var appData = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
		var unityDirectory = Path.Combine( appData, "Unity" );
		if ( !string.IsNullOrEmpty( appData ) && Directory.Exists( unityDirectory ) )
			dialog.Directory = unityDirectory;
		if ( !dialog.Execute() ) return;
		LoadPackage( dialog.SelectedFile );
	}

	/// <summary>Pick a Unity project directory and ask which immediate Assets/ subfolders to import.</summary>
	public async void BrowseFolder()
	{
		if ( Busy ) return;
		var dialog = new FileDialog( this ) { Title = "Import Unity Project Folder" };
		dialog.SetFindDirectory();
		var appData = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
		var unityDirectory = Path.Combine( appData, "Unity" );
		if ( !string.IsNullOrEmpty( appData ) && Directory.Exists( unityDirectory ) ) dialog.Directory = unityDirectory;
		if ( !dialog.Execute() ) return;
		var project = dialog.SelectedFile;
		if ( !UnityProjectFolders.IsValidUnityFolder( project ) )
		{
			EditorUtility.DisplayDialog( "Not a Unity project", $"{project}\nA Unity project needs an Assets/ directory and a ProjectSettings/ directory." );
			return;
		}
		string[] folders;
		try { folders = await ChooseFolders( project ); }
		catch ( Exception ex ) { EditorUtility.DisplayDialog( "Could not choose folders", ex.Message ); return; }
		if ( folders == null ) return;
		LoadArchive( Path.GetFileName( project ), ( p, t, a ) => UnityArchive.ReadDirectory( project, p, t, a, folders ) );
	}

	/// <summary>Checkbox dialog: immediate subfolders of Assets/, all selected by default.</summary>
	static Task<string[]> ChooseFolders( string project )
	{
		var available = UnityProjectFolders.ListAssetFolders( project );
		var completion = new TaskCompletionSource<string[]>( TaskCreationOptions.RunContinuationsAsynchronously );
		var window = new Widget( null, true ) { WindowTitle = $"Import folders from {Path.GetFileName( project )}", Size = new Vector2( 420, 480 ), MinimumSize = new Vector2( 300, 260 ) };
		window.Layout = Layout.Column(); window.Layout.Margin = 16; window.Layout.Spacing = 8;
		window.Layout.Add( new Label( "Which Assets/ subfolders should be imported? Files directly in Assets/ are always included." ) { WordWrap = true } );
		var scroll = window.Layout.Add( new ScrollArea( window ), 1 );
		scroll.Canvas = new Widget( window );
		scroll.Canvas.SetSizeMode( SizeMode.Default, SizeMode.CanGrow );
		scroll.Canvas.Layout = Layout.Column();
		var boxes = new Checkbox[available.Length];
		for ( int i = 0; i < available.Length; i++ ) boxes[i] = scroll.Canvas.Layout.Add( new Checkbox( available[i] ) { Value = true } );
		scroll.Canvas.Layout.AddStretchCell();
		var buttons = window.Layout.AddRow();
		buttons.AddStretchCell();
		buttons.Add( new Button( "Cancel" ) ).Clicked = () => { completion.TrySetResult( null ); window.Close(); };
		buttons.Add( new Button.Primary( "Continue" ) ).Clicked = () =>
		{
			completion.TrySetResult( available.Where( ( _, i ) => boxes[i].Value ).ToArray() );
			window.Close();
		};
		window.Show();
		return completion.Task;
	}

	/// <summary>Load a package into the selection window, including from other editor tools.</summary>
	public void LoadPackage( string file )
	{
		if ( Busy ) return;
		LoadArchive( Path.GetFileName( file ), ( p, t, a ) => UnityArchive.Read( file, p, t, a ) );
	}

	/// <summary>Shared load path for .unitypackage files and Unity project directories.</summary>
	void LoadArchive( string label, Func<IProgress<ImportProgress>, CancellationToken, string, UnityArchive> read )
	{
		if ( Busy ) return;
		var assetsDirectory = Project.Current?.GetAssetsPath();
		if ( string.IsNullOrEmpty( assetsDirectory ) )
		{
			EditorUtility.DisplayDialog( "Import unavailable", "Open a local s&box project before importing." );
			return;
		}
		ReleaseArchive();
		catalog = null;
		plan = null;
		describedAsset = null;
		tree.SetItems( Array.Empty<TreeNode>() );
		ClearCompletion();
		WindowTitle = "Import Unity Package";
		packageLabel.Text = label;
		packageLabel.ToolTip = label;
		cancellation = new CancellationTokenSource();
		latestProgress = new( 0, "Reading package…", "Scanning package" );
		stageLabel.Text = latestProgress.Stage; stageLabel.Visible = true;
		readTask = Task.Run( () =>
		{
			var loaded = read( new ProgressSink( this, stage: "Scanning package" ), cancellation.Token, assetsDirectory );
			try
			{
				latestProgress = new( 1, "Resolving package dependencies…", "Finding dependencies" );
				catalog = ImportCatalog.Read( loaded, cancellation.Token );
				return loaded;
			}
			catch { DisposeAndReport( loaded ); throw; }
		} );
		RefreshSelection();
	}

	void Import()
	{
		if ( Busy || plan == null || plan.Assets.Count == 0 ) return;
		var assets = Project.Current?.GetAssetsPath();
		if ( string.IsNullOrEmpty( assets ) )
		{
			EditorUtility.DisplayDialog( "Import unavailable", "Open a local s&box project before importing." );
			return;
		}
		var confirmedPlan = plan;
		cancellation = new CancellationTokenSource();
		latestProgress = new( 0, "Preparing import…", "Preparing assets" );
		stageLabel.Text = latestProgress.Stage; stageLabel.Visible = true;
		ClearCompletion();
		WindowTitle = "Import Unity Package";
		importTask = ImportAndPrepare( confirmedPlan, assets, cancellation.Token );
		RefreshSelection();
	}

	async Task<ImportResult> ImportAndPrepare( ImportPlan confirmedPlan, string assets, CancellationToken token )
	{
		ImportMergePlan merge = null;
		ImportResult result = null;
		try
		{
			// A directory archive has no extracted workspace: never re-read it as a gzip tar.
			if ( !archive.HasExtractedFiles && archive.SourceDirectory == null )
			{
				var selected = confirmedPlan.Assets.Where( a => a.Explicit ).Select( a => a.Asset.Path ).ToHashSet( StringComparer.OrdinalIgnoreCase );
				var options = confirmedPlan.Options;
				archive = await Task.Run( () => UnityArchive.Read( archive.FileName, new ProgressSink( this, 0, 0.15, "Scanning package" ), token, assets ) );
				foreach ( var item in archive.Assets ) item.Selected = selected.Contains( item.Path );
				latestProgress = new( 0.15, "Resolving package dependencies…", "Finding dependencies" );
				catalog = await Task.Run( () => ImportCatalog.Read( archive, token ) );
				confirmedPlan = catalog.CreatePlan( options );
				tree.Load( archive.Assets );
			}
			merge = await Task.Run( () => ImportMergePlan.Create( confirmedPlan, assets, token ) );
			await Task.Run( () => merge.Prepare( new ProgressSink( this, 0.15, 0.55 ), token, extractChannels: TextureChannels.ExtractBatch ) );
			latestProgress = new( 0.7, "Review import files…", "Waiting for confirmation" );
			await ImportConflictWindow.Review( "Review import files", merge.Destination, merge.Outputs, token );
			result = await Task.Run( () => merge.Commit( new ProgressSink( this, 0.7, 0.1, "Writing files" ), token ) );
			latestProgress = new( 0.8, "Preparing imported resources…", "Preparing s&box resources" );
			preparation = await ImportAssetPreparation.RunImport( result, new ProgressSink( this, 0.8, 0.18, "Preparing s&box resources" ), token );
		}
		finally
		{
			latestProgress = new( latestProgress?.Fraction ?? 0, "Removing temporary files…", "Cleaning up" );
			await Task.Run( () => { merge?.Dispose(); archive?.Dispose(); } );
			if ( result == null )
				foreach ( var warning in (merge?.CleanupWarnings ?? Array.Empty<string>()).Concat( archive?.CleanupWarnings ?? Array.Empty<string>() ) ) Log.Warning( warning );
		}
		return result with { Warnings = result.Warnings.Concat( merge.CleanupWarnings ).Concat( archive.CleanupWarnings ).Distinct().ToArray() };
	}

	void Cancel()
	{
		if ( !Busy ) { Close(); return; }
		cancellation.Cancel();
		cancel.Enabled = false;
		status.Text = "Cancelling…";
	}

	[EditorEvent.Frame]
	void Tick()
	{
		if ( !IsValid ) return;
		if ( Busy && latestProgress != null )
		{
			status.Text = cancellation.IsCancellationRequested ? "Cancelling…" : $"{latestProgress.Fraction:P0} — {latestProgress.Message}";
			stageLabel.Text = cancellation.IsCancellationRequested ? "Cancelling / cleaning up" : latestProgress.Stage ?? "Preparing assets";
			progressBar.Update();
		}
		if ( readTask?.IsCompleted == true )
		{
			var task = readTask;
			readTask = null;
			FinishOperation( () =>
			{
				archive = task.GetAwaiter().GetResult();
				tree.Load( archive.Assets );
				stageLabel.Text = "Scan complete — select assets";
				status.Text = "Ready. Original folders will be preserved under Assets/Imported. Existing files are reviewed before writing.";
			} );
		}
		if ( importTask?.IsCompleted == true )
		{
			var task = importTask;
			importTask = null;
			FinishOperation( () =>
			{
				var result = task.GetAwaiter().GetResult();
				SetCompletion( result );
				EditorUtility.DisplayDialog( completion.Title, completion.Message );
			} );
		}
	}

	void SetCompletion( ImportResult result )
	{
		completion = ImportCompletion.Create( result, preparation?.Warnings, preparation?.Errors, preparation?.Cancelled == true );
		foreach ( var error in completion.Errors ) Log.Error( error );
		// Detailed compatibility warnings belong in the report, not hundreds of console lines.
		Log.Info( $"{completion.Title}: {result.AssetCount} assets. {completion.Counts}." );
		reportJson = completion.AppendReport( result.ReportJson );
		completionInformation = result.Information;
		showReport.Visible = exportReport.Visible = true;
		ApplyCompletion();
	}

	void ClearCompletion()
	{
		preparation = null;
		completion = null;
		reportJson = null;
		completionInformation = Array.Empty<string>();
		showReport.Visible = exportReport.Visible = false;
	}

	static void DisposeAndReport( UnityArchive package )
	{
		if ( package == null || !package.HasExtractedFiles ) return;
		package.Dispose();
		foreach ( var warning in package.CleanupWarnings ) Log.Warning( warning );
	}

	void ReleaseArchive()
	{
		DisposeAndReport( archive );
		archive = null;
	}

	void ExportReport()
	{
		var dialog = new FileDialog( this ) { Title = "Export import report", DefaultSuffix = ".json" };
		dialog.SetFindFile(); dialog.SetModeSave(); dialog.SetNameFilter( "JSON report (*.json)" );
		if ( dialog.Execute() )
		{
			try { File.WriteAllText( dialog.SelectedFile, reportJson ); }
			catch ( Exception ex ) { EditorUtility.DisplayDialog( "Could not export report", ex.Message ); }
		}
	}

	void ShowDetails()
	{
		var window = new Widget( this, true ) { WindowTitle = completion.Title, Size = new Vector2( 720, 500 ) };
		window.Layout = Layout.Column(); window.Layout.Margin = 16;
		var scroll = window.Layout.Add( new ScrollArea( window ), 1 );
		scroll.Canvas = new Widget( window ); scroll.Canvas.Layout = Layout.Column();
		void Section( string title, string[] items )
		{
			if ( items.Length == 0 ) return;
			scroll.Canvas.Layout.Add( new Label( title ) );
			foreach ( var item in items ) scroll.Canvas.Layout.Add( new Label( item ) { WordWrap = true } );
		}
		Section( "Errors", completion.Errors ); Section( "Warnings", completion.Warnings ); Section( "Information", completionInformation );
		if ( completion.Errors.Length + completion.Warnings.Length + completionInformation.Length == 0 ) scroll.Canvas.Layout.Add( new Label( "Import completed without issues." ) );
		window.Layout.Add( new Button.Primary( "Done" ) ).Clicked = () => window.Close();
		window.Show();
	}

	void ApplyCompletion()
	{
		stageLabel.Text = completion.Title;
		stageLabel.Visible = !completion.Cancelled && completion.Errors.Length == 0;
		status.Text = completion.Status;
		WindowTitle = $"Import Unity Package — {completion.Title}";
		if ( !completion.Cancelled ) latestProgress = new( 1, completion.Title );
		progressBar.Update();
	}

	void FinishOperation( Action action )
	{
		try { action(); }
		catch ( OperationCanceledException ex )
		{
			stageLabel.Visible = false;
			status.Text = "Cancelled. Files already imported remain in the project.";
			if ( ex.Data["StagingCleanupError"] is string cleanup ) { status.Text += " Temporary files could not be removed; see the console."; Log.Warning( cleanup ); }
		}
		catch ( Exception ex )
		{
			stageLabel.Visible = false;
			status.Text = $"Import stopped: {ex.Message}\nFiles already imported remain in the project.";
			Log.Error( $"{status.Text}\n{ex}" );
			if ( ex.Data["StagingCleanupError"] is string cleanup ) Log.Warning( cleanup );
			EditorUtility.DisplayDialog( "Unity package import failed", ex.Message );
		}
		finally
		{
			cancellation.Dispose();
			cancellation = null;
			RefreshSelection();
			if ( closeWhenCancelled ) Close();
		}
	}

	void SetSelection( Func<UnityAsset, bool> select )
	{
		if ( Busy || archive == null ) return;
		foreach ( var asset in archive.Assets ) asset.Selected = select( asset );
		RefreshSelection();
	}

	void DescribeAsset( UnityAsset asset )
	{
		describedAsset = asset;
		if ( asset == null ) { details.Text = "Select a row to see its output and dependency details."; return; }
		var entry = plan?.Find( asset );
		var reasons = entry?.RequiredBy.Select( d => $"{d.Asset.Path} ({d.Reason})" ).Distinct().ToArray() ?? Array.Empty<string>();
		var problems = plan?.Issues.Where( i => i.Asset == asset.Path ).Select( i => i.Message ).ToArray() ?? Array.Empty<string>();
		details.Text = asset.Path + "\n" + (entry?.OutputLabel ?? (asset.Kind == UnityAssetKind.Unsupported ? "Unsupported" : "Not included")) +
			(reasons.Length == 0 ? "" : " · Required by: " + string.Join( "; ", reasons.Take( 3 ) ) + (reasons.Length > 3 ? $"; +{reasons.Length - 3} more" : "")) +
			(problems.Length == 0 ? "" : "\n" + string.Join( " ", problems.Take( 2 ) ));
		details.ToolTip = string.Join( "\n", reasons.Concat( problems ) );
	}

	void RefreshSelection()
	{
		if ( confirm == null ) return;
		browse.Enabled = browseFolder.Enabled = tree.Enabled = additionalTerrain.Enabled = importScenes.Enabled = advancedToggle.Enabled = !Busy;
		selectAll.Enabled = selectNone.Enabled = expandAll.Enabled = collapseAll.Enabled = !Busy && archive != null;
		expandLayer.Enabled = collapseLayer.Enabled = !Busy && archive != null;
		cancel.Enabled = !Busy || cancellation?.IsCancellationRequested != true;
		cancel.Text = Busy ? "Cancel operation" : "Cancel";
		cancel.Visible = confirm.Visible = completion == null || Busy;
		reimport.Visible = done.Visible = completion != null && !Busy;
		if ( !Busy ) plan = catalog?.CreatePlan( Options );
		confirm.Enabled = reimport.Enabled = !Busy && plan?.Assets.Count > 0;
		confirm.Text = "Review import";
		selection.Text = plan == null ? "No package loaded." :
			$"{plan.Assets.Count( a => a.Explicit ):N0} selected + {plan.Assets.Count( a => !a.Explicit ):N0} dependencies = {plan.Assets.Count:N0} files to import\n" +
			$"Outputs: {plan.Assets.Count( a => a.Vmdl ):N0} VMDL · {plan.Assets.Count( a => a.Vmat ):N0} VMAT · {plan.Assets.Count( a => a.Tmat ):N0} TMAT · {plan.Assets.Count( a => a.Prefab ):N0} PREFAB · {plan.Assets.Count( a => a.Scene ):N0} SCENE · {plan.Issues.Count:N0} reference / source issues · {archive.Assets.Count( a => a.Kind == UnityAssetKind.Unsupported ):N0} unsupported files";
		selection.ToolTip = plan == null ? "" : string.Join( "\n", plan.Issues.Select( i => $"{i.Asset}: {i.Message}" ) );
		DescribeAsset( describedAsset );
		tree.Update();
	}

	bool PaintProgress()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( progressBar.LocalRect, 3 );
		var rect = progressBar.LocalRect;
		rect.Width *= (float)Math.Clamp( latestProgress?.Fraction ?? 0, 0, 1 );
		Paint.SetBrush( Theme.Blue );
		Paint.DrawRect( rect, 3 );
		return false;
	}

	protected override bool OnClose()
	{
		if ( Busy ) { closeWhenCancelled = true; Cancel(); return false; }
		ReleaseArchive();
		return true;
	}

	sealed class ProgressSink( UnityPackageWindow window, double start = 0, double scale = 1, string stage = null ) : IProgress<ImportProgress>
	{
		public void Report( ImportProgress value ) => window.latestProgress = new( start + scale * value.Fraction, value.Message, value.Stage ?? stage );
	}
}
