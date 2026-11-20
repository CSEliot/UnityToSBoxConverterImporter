using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathTool;

public class PathNode
{
	public Vector3 Position { get; set; }
	public virtual Vector3 GetPointBetween( PathNode other, float lerp )
	{
		return Vector3.Lerp( Position, other.Position, lerp );
	}
}
