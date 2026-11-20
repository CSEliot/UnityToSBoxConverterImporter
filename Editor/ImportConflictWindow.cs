using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace ImportUnityPackage;

/// <summary>Review destination conflicts without changing package selection.</summary>
public sealed class ImportConflictWindow : Widget
{
	readonly TaskCompletionSource<bool> completion = new();
	readonly CancellationToken token;
	public static async Task Review( string title, string destination, IEnumerable<ImportFileChoice> entries, CancellationToken cancel )
	{
		var window = new ImportConflictWindow( title, destination, entries.ToArray(), cancel );
		window.Show();
		if ( !await window.completion.Task ) throw new OperationCanceledException( "Import review cancelled", cancel );
	}

	ImportConflictWindow( string title, string destination, ImportFileChoice[] entries, CancellationToken cancel ) : base( null, true )
	{
		token = cancel;
		WindowTitle = title;
		Size = new Vector2( 850, 600 );
		MinimumSize = new Vector2( 600, 350 );
		Layout = Layout.Column();
		Layout.Margin = 16;
		Layout.Spacing = 8;
		Layout.Add( new Label( $"Destination: {destination}" ) { WordWrap = true } );
		var summary = Layout.Add( new Label( "" ) { WordWrap = true } );
		summary.SetStyles( "font-weight: bold;" );
		var hasConflicts = entries.Any( c => c.Conflict );
		Layout.Add( new Label( "Conflicts keep the existing file unless Replace is ticked. Replacing shared files can change models and scenes that already use them. Each row below shows the action that will be taken for it." ) { WordWrap = true, Visible = hasConflicts } );
		var controls = Layout.AddRow();
		var keep = controls.Add( new Button( "Keep all existing" ) { Visible = hasConflicts } );
		var replace = controls.Add( new Button( "Replace all conflicts" ) { Visible = hasConflicts } );
		controls.AddStretchCell();
		var scroll = Layout.Add( new ScrollArea( this ), 1 );
		scroll.Canvas = new Widget( this );
		scroll.Canvas.SetSizeMode( SizeMode.Default, SizeMode.CanGrow );
		scroll.Canvas.Layout = Layout.Column();
		scroll.Canvas.Layout.Spacing = 8;
		var rows = new List<(ImportFileChoice Entry, Checkbox Box)>();
		void Refresh()
		{
			foreach ( var row in rows ) row.Box.Text = $"{row.Entry.Action} · {row.Entry.Status} · {row.Entry.Path}";
			summary.Text = $"{entries.Count( c => c.Conflict && c.Replace )} will be replaced · {entries.Count( c => c.Conflict && !c.Replace )} kept · {entries.Count( c => c.ExistingHash == null )} new · {entries.Count( c => c.ExistingHash != null && !c.Conflict )} identical";
		}
		foreach ( var entry in entries.OrderByDescending( c => c.Conflict ).ThenBy( c => c.Path ) )
		{
			if ( entry.Conflict )
			{
				var box = scroll.Canvas.Layout.Add( new Checkbox( $"{entry.Action} · {entry.Status} · {entry.Path}" ) { Value = entry.Replace } );
				box.StateChanged += _ => { entry.Replace = box.Value; Refresh(); };
				rows.Add( (entry, box) );
				var detail = entry.Detail;
				var asset = AssetSystem.FindByPath( UnityImport.DiskPath( destination, entry.Path ) );
				if ( asset != null )
				{
					var users = asset.GetDependants( true ).Select( a => a.Path ).Distinct().ToArray();
					if ( users.Length > 0 ) detail += " Project references: " + string.Join( ", ", users.Take( 6 ) ) + (users.Length > 6 ? $" (+{users.Length - 6} more)" : "");
				}
				if ( !string.IsNullOrWhiteSpace( detail ) ) scroll.Canvas.Layout.Add( new Label( detail ) { WordWrap = true } );
			}
			else scroll.Canvas.Layout.Add( new Label( $"{entry.Action} · {entry.Path}" ) );
		}
		scroll.Canvas.Layout.AddStretchCell();
		void SetAll( bool value )
		{
			foreach ( var entry in entries.Where( e => e.Conflict ) ) entry.Replace = value;
			foreach ( var row in rows ) row.Box.Value = value;
			Refresh();
		}
		keep.Clicked = () => SetAll( false );
		replace.Clicked = () => SetAll( true );
		Refresh();
		var buttons = Layout.AddRow();
		buttons.AddStretchCell();
		buttons.Add( new Button( "Cancel import" ) ).Clicked = () => Close();
		buttons.Add( new Button.Primary( "Continue" ) ).Clicked = () => { completion.TrySetResult( true ); Close(); };
	}

	[EditorEvent.Frame]
	void Tick() { if ( IsValid && token.IsCancellationRequested ) Close(); }
	protected override bool OnClose() { completion.TrySetResult( false ); return true; }
}
