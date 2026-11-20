using Editor.MeshEditor;
using Editor.ShaderGraph;

namespace PathTool.Editor;

/// Create a new path
/// Left Click = Add Node
/// Escape = Delete Node
/// Enter = Create Path
[EditorTool]
[Title( "Path" )]
[Icon( "route" )]
[Group( "9" )]
[Order(10000)]
public partial class PathTool : EditorTool
{
	public bool InProgress => nodes == null || nodes.Count <= 1;

	Vector3 origin;
	Type pathType;
	Type nodeType;
	Path path;
	List<PathNode> nodes => path?.Nodes;
	PathNode selectedNode;
	bool hasDeleted = false;
	public PathTool()
	{
		AllowGameObjectSelection = false;
	}

	public override void OnEnabled()
	{
		base.OnEnabled();

		if(pathType == null || !pathType.IsAssignableTo( typeof( Path ) ) ) 
		{
			SelectPathType( EditorTypeLibrary.GetType<Path>() );
		}

		AllowGameObjectSelection = false;
		Selection.Clear();
		Selection.Set( this );
	}

	public override void OnUpdate()
	{
		const float NODE_RADIUS = 2.5f;
		const float ARROW_WIDTH = 1.5f;
		const float ARROW_LENGTH = 4f;

		float textSize = 22 * Gizmo.Settings.GizmoScale * Application.DpiScale;

		bool nodeSelected = false;
		if ( nodes.Count > 0 )
		{
			using(Gizmo.Scope("Path", origin))
			{
				for ( int i = 0; i < nodes.Count; i++ )
				{
					var node = nodes[i];
					Vector3 pos = node.Position;
					using(Gizmo.Scope($"Node{i}", pos ) )
					{
						Gizmo.Draw.Color = Color.White;

						if(i < nodes.Count - 1)
						{
							Gizmo.Draw.Arrow( Vector3.Zero, nodes[i + 1].Position - pos, ARROW_LENGTH, ARROW_WIDTH );
						}

						if(!nodeSelected && Gizmo.Pressed.This)
						{
							SelectNode( node );
						}

						if (node == selectedNode)
						{
							Gizmo.Draw.Color = Color.Green;
							if(Gizmo.Control.Position( "NodePosition", Vector3.Zero, out Vector3 delta ))
							{
								node.Position += Gizmo.Snap(delta, delta);
							}
						}

						Gizmo.Draw.ScreenText( $"{i + 1}", Gizmo.Camera.ToScreen( pos + origin ) + Vector2.Up * NODE_RADIUS * 3, size: textSize );
						Gizmo.Draw.SolidSphere( Vector3.Zero, NODE_RADIUS );
						Gizmo.Hitbox.Sphere( new Sphere( Vector3.Zero, NODE_RADIUS ) );
					}
				}

				Gizmo.Draw.Color = Color.Yellow;
				Gizmo.Draw.Arrow( nodes[nodes.Count - 1].Position, nodes[0].Position, ARROW_LENGTH, ARROW_WIDTH );
			}
		}

		if ( !Gizmo.Pressed.Any && Gizmo.WasLeftMousePressed )
		{
			CreateNode();
		}

		if ( Application.FocusWidget is not null )
		{
			if ( Application.IsKeyDown( KeyCode.Enter ) )
			{
				var obj = CreatePath();
				Selection.Clear();
				Selection.Add( obj );

				EditorToolManager.CurrentModeName = null;
			}
			else if(!hasDeleted)
			{
				if ( Application.IsKeyDown( KeyCode.Escape ) )
				{
					hasDeleted = true;
					RemoveLastNode();
				}
				else if(selectedNode != null && Application.IsKeyDown(KeyCode.Backspace))
				{
					hasDeleted = true;
					RemoveNode( selectedNode );
				}
			}

			if ( !Application.IsKeyDown( KeyCode.Escape ) )
			{
				hasDeleted = false;
			}
		}
	}

	public override void OnSelectionChanged()
	{
		base.OnSelectionChanged();

		if ( !Selection.OfType<GameObject>().Any() )
		{
			Selection.Set( this );
			return;
		}

		EditorToolManager.CurrentModeName = "object";
		//_finished = true;
	}

	private void CreateNode()
	{
		var tr = Trace.UseRenderMeshes( true )
			.UsePhysicsWorld( true )
			.Run();

		if ( !tr.Hit ) return;

		Vector3 position = tr.EndPosition;
		if ( Gizmo.Settings.SnapToGrid != Gizmo.IsCtrlPressed )
		{
			position = position.SnapToGrid( Gizmo.Settings.GridSpacing );
		}

		if(nodes.Count == 0)
		{
			origin = position;
			position = Vector3.Zero;
		}
		else
		{
			position = position - origin;
		}

		PathNode node = EditorTypeLibrary.Create<PathNode>( nodeType );
		node.Position = position;
		nodes.Add( node );
		SelectNode( node );


		UpdateStatus();
	}

	private void SelectNode(PathNode node)
	{
		//Log.Info( node );
		selectedNode = node;
		BuildControlSheet();
	}

	private void RemoveLastNode()
	{
		if ( nodes.Count > 0 )
		{
			RemoveNode( nodes[nodes.Count - 1] );
		}
	}

	public void RemoveNode(PathNode node)
	{
		nodes.Remove( node );

		Selection.Clear();
		Selection.Set( this );

		UpdateStatus();
	}
	private GameObject CreatePath()
	{
		GameObject obj = Scene.CreateObject();
		obj.Name = "Path";
		obj.MakeNameUnique();
		obj.Transform.Position = origin;

		var comp = obj.Components.Create(EditorTypeLibrary.GetType(pathType));
		try
		{
			comp.GetSerialized().GetProperty( "Nodes" ).SetValue( nodes );
		}
		catch(Exception e)
		{
			Log.Info($"Error while serializing: {e.Message}");
		}

		return obj;
	}

	private IEnumerable<TypeDescription> GetPathTypes()
	{
		return EditorTypeLibrary.GetTypes<Path>();
	}
}
