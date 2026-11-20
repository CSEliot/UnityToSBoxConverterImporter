namespace PathTool.Editor;

[CanEdit( typeof( PathTool ) )]
public partial class PathToolInspector : InspectorWidget
{
	public PathToolInspector( SerializedObject so ) : base( so )
	{
		if ( so.Targets.FirstOrDefault() is not PathTool tool )
			return;

		Layout = Layout.Column();
		Layout.Add( tool.BuildUI() );
	}
}
public partial class PathTool
{
	private StatusWidget Header { get; set; }
	private Layout ControlLayout { get; set; }

	public Widget BuildUI()
	{
		var widget = new Widget( null );
		widget.Layout = Layout.Column();
		widget.Layout.Margin = 4;

		Header = new StatusWidget( widget );
		UpdateStatus();
		widget.Layout.Add( Header );
		widget.Layout.AddSpacingCell( 8 );

		{
			var layout = widget.Layout.AddRow();
			layout.Spacing = 4;

			var pathTypeComboBox = new ComboBox();
			pathTypeComboBox.MinimumHeight = 40f;
			foreach ( var type in GetPathTypes() )
			{
				var displayInfo = DisplayInfo.ForType( type.TargetType );
				if ( !displayInfo.Browsable ) continue;

				pathTypeComboBox.AddItem( displayInfo.Name, displayInfo.Icon ?? "square", () => SelectPathType(type) );
			}

			layout.Add( pathTypeComboBox, 1 );
		}

		widget.Layout.AddSpacingCell( 8 );

		ControlLayout = widget.Layout.AddColumn();
		BuildControlSheet();

		widget.Layout.AddSpacingCell( 8 );
		widget.Layout.AddStretchCell();

		return widget;
	}
	public void OnEdited( SerializedProperty property )
	{
		
	}
	private void SelectPathType(TypeDescription t)
	{
		var target = t.TargetType;
		if ( !target.IsAssignableTo( typeof( Path ) ) )
		{
			Log.Warning( $"Type {t} isnt a path!" );
			return;
		}

		pathType = target;
		if(t.IsGenericType)
		{
			nodeType = target.GetGenericArguments()[0];
		}
		else
		{
			nodeType = typeof( PathNode );
		}

		var nodes = path?.Nodes ?? new List<PathNode>();
		path = EditorTypeLibrary.Create<Path>( pathType );
		path.Nodes = nodes;
	}

	private void BuildControlSheet()
	{
		if ( !ControlLayout.IsValid() )
			return;

		ControlLayout.Clear( true );

		var props = path.GetSerialized().Where( ShowPathProperty );

		if(props.Any())
		{
			var pathHeader = new Label( "Properties" )
			{
				Margin = 2f
			};
			var pathSheet = new ControlSheet();

			foreach(var prop in props)
			{
				pathSheet.AddRow( prop );
			}

			ControlLayout.Add( pathHeader );
			ControlLayout.Add( pathSheet );
		}

		if ( selectedNode != null )
		{
			var selectedHeader = new Label( "Selected Node" )
			{
				Margin = 2f
			};

			var so = selectedNode.GetSerialized();
			so.OnPropertyChanged += OnEdited;

			var selectedSheet = new ControlSheet();
			selectedSheet.AddObject( so );

			ControlLayout.Add( selectedHeader );
			ControlLayout.Add( selectedSheet );
		}
	}

	private static readonly string[] ignoredProperties = new string[] { "Enabled", "Nodes" };
	private bool ShowPathProperty(SerializedProperty prop)
	{
		if ( !EditorTypeLibrary.TryGetType( prop.PropertyType, out var type ) )
			return false;

		var description = EditorTypeLibrary.GetType( prop.Parent.TypeName ).GetProperty( prop.Name );
		if ( description == null ) return false;

		bool show = prop.IsEditable && description.IsGetMethodPublic && description.IsSetMethodPublic && 
										!ignoredProperties.Contains(prop.Name);
		if(show)
		{
			Log.Info($"Prop: {prop.Name} ({type})");
		}

		return show;
	}

	private void UpdateStatus()
	{
		Header.Text = $"{(InProgress ? "Placing" : "Create")} Path";
		Header.LeadText = InProgress ? "Place nodes to create a path." : "Press Enter to complete the path.";
		Header.Color = InProgress ? Theme.Blue : Theme.Green;
		Header.Icon = InProgress ? "check_circle_outline" : "view_in_ar";
		Header.Update();
	}

	private class StatusWidget : Widget
	{
		public string Icon { get; set; }
		public string Text { get; set; }
		public string LeadText { get; set; }
		public Color Color { get; set; }

		public StatusWidget( Widget parent ) : base( parent )
		{
			MinimumSize = 48;
			SetSizeMode( SizeMode.Default, SizeMode.CanShrink );
		}

		protected override void OnPaint()
		{
			var rect = new Rect( 0, Size );

			Paint.ClearPen();
			// Paint.SetBrush( );
			Paint.DrawRect( rect );

			rect.Left += 8;

			Paint.SetPen( Color );
			var iconRect = Paint.DrawIcon( rect, Icon, 24, TextFlag.LeftCenter );

			rect.Top += 8;
			rect.Left = iconRect.Right + 8;

			Paint.SetPen( Color );
			Paint.SetDefaultFont( 10, 500 );
			var titleRect = Paint.DrawText( rect, Text, TextFlag.LeftTop );

			rect.Top = titleRect.Bottom + 2;

			Paint.SetPen( Color.WithAlpha( 0.6f ) );
			Paint.SetDefaultFont( 8, 400 );
			Paint.DrawText( rect, LeadText, TextFlag.LeftTop );
		}
	}
}
