namespace PathTool;

[Title("Linear Path")]
public class Path : Component
{
	public List<PathNode> Nodes { get; set; }
	public int Length => Nodes.Count;
	public PathNode GetNode( int index )
	{
		if ( Nodes == null || Nodes.Count == 0 ) return null;

		return Nodes[index % Nodes.Count];
	}

	/// <summary>
	/// Get position at specified index, decimal points = between points.
	/// </summary>
	public Vector3 GetPosition( float index )
	{
		if ( Nodes == null || !Nodes.Any() ) return default;

		if ( float.IsInteger( index ) )
		{
			return Nodes[(int)index].Position;
		}

		PathNode previous = GetNode( MathX.FloorToInt( index ) );
		PathNode next = GetNode( MathX.CeilToInt( index ) );

		if ( next == null )
		{
			return previous.Position;
		}

		float lerp = index % 1;

		return previous.GetPointBetween( next, lerp );
	}
	public Vector3 GetPositionWorld( float index )
	{
		return Transform.World.PointToWorld( GetPosition( index ) );
	}
	public int GetClosestNode( Vector3 position )
	{
		return Nodes.IndexOf( Nodes.SmallestBy( n => n.Position.DistanceSquared( position ) ) );
	}
	public PathNode GetNext( PathNode node )
	{
		int index = Nodes.IndexOf( node );
		if ( index == -1 ) return null;

		if ( index >= Nodes.Count - 1 ) return Nodes[0];

		return Nodes[index + 1];
	}
	public PathNode GetPrevious( PathNode node )
	{
		int index = Nodes.IndexOf( node );
		if ( index == -1 ) return null;

		if ( index <= 0 ) return Nodes[Nodes.Count - 1];

		return Nodes[index - 1];
	}

	protected override void DrawGizmos()
	{
		if ( Nodes == null || !Nodes.Any() ) return;

		const float NODE_RADIUS = 2.5f;

		const float DETAIL = 0.1f;
		float textSize = 22 * Gizmo.Settings.GizmoScale;
		Vector3 previous = GetPosition( 0 );
		Vector3 point;


		for ( float fraction = DETAIL; fraction <= Nodes.Count; fraction += DETAIL )
		{
			point = GetPosition( fraction );
			Gizmo.Draw.Line( previous, point );
			previous = point;
		}

		for ( int i = 0; i < Nodes.Count; i++ )
		{
			var node = Nodes[i];

			using ( Gizmo.Scope( $"Node{i}", node.Position ) )
			{
				Sphere nodeDisplay = new( Vector3.Zero, NODE_RADIUS );
				Gizmo.Draw.SolidSphere( nodeDisplay.Center, nodeDisplay.Radius );
				Gizmo.Hitbox.Sphere( nodeDisplay );
				if ( Gizmo.IsSelected )
				{
					Gizmo.Draw.ScreenText( $"{i + 1}", Gizmo.Camera.ToScreen( node.Position + Transform.Position ) + Vector2.Up * NODE_RADIUS * 3, size: textSize );
				}

				if ( Gizmo.Pressed.This )
				{
					Gizmo.Select( true, false );
				}
			}
		}
	}
}
[Hide]
public class Path<T> : Path where T : PathNode
{
	public new List<T> Nodes { get; set; }
	public new T GetNode( int index )
	{
		if ( Nodes == null || Nodes.Count == 0) return null;

		return Nodes[index % Nodes.Count];
	}
	public T GetNext( T node )
	{
		var next = base.GetNext( node );
		if ( next is T valid )
			return valid;

		return null;
	}
	public T GetPrevious( T node )
	{
		var previous = base.GetPrevious( node );
		if ( previous is T valid )
			return valid;

		return null;
	}
}
