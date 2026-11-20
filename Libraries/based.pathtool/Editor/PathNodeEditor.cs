namespace PathTool.Editor;


[CustomEditor( typeof( PathNode ) )]
public class PathNodeEditor : ControlWidget
{
	SerializedObject obj;
	public PathNodeEditor( SerializedProperty property ) : base( property )
	{
		if ( !property.TryGetAsObject( out obj ) ) return;
		TranslucentBackground = true;

		ControlSheet properties = new ControlSheet();
		properties.AddObject( obj );

		Layout = properties;
	}
}
