using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ImportUnityPackage;

/// <summary>
/// Unity's built-in primitive meshes: the fileIDs in Unity's default resources, the ModelDoc shape each one
/// becomes, and the size Unity gives it in metres. Unity's own primitives are a 1 m cube and sphere, a
/// 10 x 10 m plane, a 1 x 2 m capsule and cylinder, and a 1 x 1 m quad; every dimension here is that size
/// times <c>unitsPerMetre</c>, because a generated model bakes the inches in rather than carrying the
/// 39.37 modifier an imported FBX needs.
/// </summary>
internal static class UnityPrimitives
{
	/// <summary>Unity's default resources, where the primitive meshes live.</summary>
	internal const string DefaultResourcesGuid = "0000000000000000e000000000000000";
	/// <summary>Unity's builtin extra resources: the default skybox material and sprite meshes.</summary>
	internal const string BuiltinExtraGuid = "0000000000000000f000000000000000";

	/// <summary>The engine material a generated primitive shows until a scene or prefab overrides slot 0.</summary>
	internal const string PlaceholderMaterial = "materials/dev/primary_white.vmat";

	/// <summary>One Unity primitive: its name, how it is drawn, and its Unity size in metres (x, y, z).</summary>
	internal sealed record Shape( string Name, RenderKind Kind, double X, double Y, double Z );

	/// <summary>How ModelDoc draws a shape. It has render primitives for a box, a plane and a sphere only.</summary>
	internal enum RenderKind { Box, Plane, Sphere, Capsule, Cylinder, Quad }

	// Verified against Unity 6000.6.9f1: a batchmode scene holding one GameObject.CreatePrimitive of each
	// kind writes these fileIDs for m_Mesh, all under DefaultResourcesGuid.
	static readonly Dictionary<long, Shape> shapes = new()
	{
		[10202] = new( "cube", RenderKind.Box, 1, 1, 1 ),
		[10206] = new( "cylinder", RenderKind.Cylinder, 1, 2, 1 ),
		[10207] = new( "sphere", RenderKind.Sphere, 1, 1, 1 ),
		[10208] = new( "capsule", RenderKind.Capsule, 1, 2, 1 ),
		[10209] = new( "plane", RenderKind.Plane, 10, 10, 0 ),
		[10210] = new( "quad", RenderKind.Quad, 1, 1, 0 ),
	};

	static readonly Dictionary<string, Shape> byName = BuildByName();

	static Dictionary<string, Shape> BuildByName()
	{
		var map = new Dictionary<string, Shape>( StringComparer.OrdinalIgnoreCase );
		foreach ( var shape in shapes.Values ) map[shape.Name] = shape;
		return map;
	}

	/// <summary>The primitive a MeshFilter or MeshCollider reference names, or null for any other mesh.</summary>
	internal static Shape Find( string guid, long fileId ) =>
		guid.Equals( DefaultResourcesGuid, StringComparison.OrdinalIgnoreCase ) && shapes.TryGetValue( fileId, out var shape ) ? shape : null;

	/// <summary>The primitive called <paramref name="name"/>, or null.</summary>
	internal static Shape Find( string name ) => name == null ? null : byName.GetValueOrDefault( name );

	/// <summary>True for either of Unity's built-in resource files, which hold meshes no package can supply.</summary>
	internal static bool IsBuiltin( string guid ) =>
		guid.Equals( DefaultResourcesGuid, StringComparison.OrdinalIgnoreCase ) || guid.Equals( BuiltinExtraGuid, StringComparison.OrdinalIgnoreCase );

	/// <summary>True when ModelDoc has no render primitive for the shape and the model needs a generated mesh.
	/// The quad is here because ModelDoc's plane lies in s&amp;box XY, which is Unity's XZ: it is Unity's Plane
	/// that orientation fits, not Unity's Quad, which stands in Unity's XY plane.</summary>
	internal static bool NeedsMesh( Shape shape ) => shape.Kind is RenderKind.Capsule or RenderKind.Cylinder or RenderKind.Quad;

	/// <summary>
	/// The generated model document for a primitive, in inches. <paramref name="mesh"/> is the resource path
	/// of the generated .obj when ModelDoc cannot draw the shape itself. The model keeps one material slot,
	/// so a ModelRenderer's indexed override 0 reaches it whatever the slot is called.
	/// </summary>
	internal static string Model( Shape shape, string mesh, double unitsPerMetre )
	{
		var x = Number( shape.X * unitsPerMetre );
		var y = Number( shape.Y * unitsPerMetre );
		var z = Number( shape.Z * unitsPerMetre );
		var bounds = shape.Kind == RenderKind.Plane
			? $"dimensions = [ {x}, {y} ]\n\t\t\t\t\t\tsegments_x = 1\n\t\t\t\t\t\tsegments_y = 1\n"
			: $"dimensions = [ {x}, {y}, {z} ]\n" + (shape.Kind is RenderKind.Box or RenderKind.Sphere ? "\t\t\t\t\t\tsegments_x = 1\n\t\t\t\t\t\tsegments_y = 1\n\t\t\t\t\t\tsegments_z = 1\n" : "");
		// A render primitive draws the shape itself; a capsule or cylinder has no primitive, so its generated
		// mesh is imported instead and the model is named after the slice of it that was written.
		var node = shape.Kind switch
		{
			RenderKind.Box => $"""
					_class = "RenderPrimitiveBox"
					name = {UnityMaterial.Quote( shape.Name )}
					parent_bone = ""
					material_name = {UnityMaterial.Quote( PlaceholderMaterial )}
					origin = [ 0.0, 0.0, 0.0 ]
					angles = [ 0.0, 0.0, 0.0 ]
					max_u = 1.0
					max_v = 1.0
				{bounds}invert = false
				""",
			RenderKind.Plane => $"""
					_class = "RenderPrimitivePlane"
					name = {UnityMaterial.Quote( shape.Name )}
					parent_bone = ""
					material_name = {UnityMaterial.Quote( PlaceholderMaterial )}
					origin = [ 0.0, 0.0, 0.0 ]
					angles = [ 0.0, 0.0, 0.0 ]
					max_u = 1.0
					max_v = 1.0
				{bounds}invert = false
				""",
			RenderKind.Sphere => $"""
					_class = "RenderPrimitiveSphere"
					name = {UnityMaterial.Quote( shape.Name )}
					parent_bone = ""
					material_name = {UnityMaterial.Quote( PlaceholderMaterial )}
					origin = [ 0.0, 0.0, 0.0 ]
					angles = [ 0.0, 0.0, 0.0 ]
					max_u = 1.0
					max_v = 1.0
				{bounds}invert = false
				""",
			_ => $$"""
					_class = "RenderMeshFile"
					name = {{UnityMaterial.Quote( shape.Name )}}
					filename = {{UnityMaterial.Quote( mesh )}}
					import_translation = [ 0.0, 0.0, 0.0 ]
					import_rotation = [ 0.0, 0.0, 0.0 ]
					import_scale = 1.0
					align_origin_x_type = "None"
					align_origin_y_type = "None"
					align_origin_z_type = "None"
					parent_bone = ""
					import_filter = 
					{
						exclude_by_default = false
						exception_list = 
						[
						]
					}
				""",
		};
		// ModelDoc property lists are flat, so the node is re-indented to the RenderMeshList's depth rather than
		// carrying the indentation of the snippets above.
		var lines = node.Split( '\n' ).Where( l => l.Trim().Length > 0 ).ToArray();
		var body = string.Join( "\n", lines.Select( ( l, i ) => (i == 0 ? "\t\t\t\t\t" : "\t\t\t\t\t\t") + l.Trim() ) );
		return $$"""
			<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:modeldoc30:version{8c2d7a91-9c42-4bf0-883a-5a3b1762d4f1} -->
			{
				rootNode = 
				{
					_class = "RootNode"
					children = 
					[
						{
							_class = "RenderMeshList"
							children = 
							[
								{
			{{body}}
								},
							]
						},
						{
							_class = "MaterialGroupList"
							children = 
							[
								{
									_class = "DefaultMaterialGroup"
									remaps = 
									[
									]
									use_global_default = true
									global_default_material = {{UnityMaterial.Quote( PlaceholderMaterial )}}
								},
							]
						},
					]
					model_archetype = ""
					primary_associated_entity = ""
					anim_graph_name = ""
					base_model_name = ""
				}
			}
			""";
	}

	/// <summary>
	/// The mesh a generated model imports for a shape ModelDoc cannot draw (a quad, a capsule, a cylinder), already
	/// in inches and written the way Unity's own mesh is: Y up, the capsule and cylinder up +Y, the quad in the XY
	/// plane facing -Z. s&amp;box's model importer applies the same Unity-to-s&amp;box basis change it applies to an
	/// FBX (Unity x, y, z becomes s&amp;box z, -x, y), so the shape arrives where a converted Unity object expects
	/// it: caps up the s&amp;box Z axis, and the quad standing in s&amp;box's YZ plane facing -X.
	/// </summary>
	internal static string Mesh( Shape shape, double unitsPerMetre )
	{
		const int sides = 16;
		var radius = shape.X * 0.5 * unitsPerMetre;
		var total = shape.Y * unitsPerMetre;
		// Unity fits the caps inside the height: a capsule is a cylinder of height (h - 2r) plus two hemispheres.
		var cylinder = shape.Kind == RenderKind.Capsule ? Math.Max( 0, total - 2 * radius ) : total;
		var half = cylinder * 0.5;
		var points = new List<double[]>();
		var normals = new List<double[]>();
		var faces = new List<int[]>();

		if ( shape.Kind == RenderKind.Quad )
		{
			var halfX = shape.X * 0.5 * unitsPerMetre;
			var halfY = shape.Y * 0.5 * unitsPerMetre;
			points.AddRange( new[] { new[] { -halfX, -halfY, 0.0 }, new[] { halfX, -halfY, 0.0 }, new[] { halfX, halfY, 0.0 }, new[] { -halfX, halfY, 0.0 } } );
			foreach ( var unused in points ) normals.Add( new[] { 0.0, 0.0, -1.0 } );
			// Unity's quad faces -Z, which is clockwise seen from +Z.
			faces.Add( new[] { 0, 3, 2, 1 } );
		}
		else if ( shape.Kind == RenderKind.Capsule )
		{
			// Rings from the bottom pole to the top pole, a quarter turn of arc per ring.
			const int capRings = 6;
			for ( var ring = 0; ring <= capRings; ring++ )
			{
				var angle = -Math.PI / 2 + Math.PI * ring / capRings / 2;
				for ( var side = 0; side < sides; side++ )
				{
					var around = Math.PI * 2 * side / sides;
					var n = new[] { Math.Cos( angle ) * Math.Cos( around ), Math.Sin( angle ), Math.Cos( angle ) * Math.Sin( around ) };
					points.Add( new[] { n[0] * radius, n[1] * radius - half, n[2] * radius } );
					normals.Add( n );
				}
			}
			for ( var ring = 0; ring <= capRings; ring++ )
			{
				var angle = Math.PI * 2 * ring / capRings / 4;
				for ( var side = 0; side < sides; side++ )
				{
					var around = Math.PI * 2 * side / sides;
					var n = new[] { Math.Cos( angle ) * Math.Cos( around ), Math.Sin( angle ), Math.Cos( angle ) * Math.Sin( around ) };
					points.Add( new[] { n[0] * radius, n[1] * radius + half, n[2] * radius } );
					normals.Add( n );
				}
			}
		}
		else
		{
			// A cylinder: two rings on the barrel, then the two cap centres.
			for ( var ring = 0; ring < 2; ring++ )
			{
				for ( var side = 0; side < sides; side++ )
				{
					var around = Math.PI * 2 * side / sides;
					points.Add( new[] { Math.Cos( around ) * radius, ring == 0 ? -half : half, Math.Sin( around ) * radius } );
					normals.Add( new[] { Math.Cos( around ), 0.0, Math.Sin( around ) } );
				}
			}
		}

		var rings = points.Count / sides;
		for ( var ring = 0; ring < rings - 1 && shape.Kind != RenderKind.Quad; ring++ )
			for ( var side = 0; side < sides; side++ )
			{
				var a = ring * sides + side;
				var b = ring * sides + (side + 1) % sides;
				var c = (ring + 1) * sides + (side + 1) % sides;
				var d = (ring + 1) * sides + side;
				faces.Add( new[] { a, b, c, d } );
			}
		// A capsule ends in its hemispheres; only the cylinder needs discs at both ends.
		if ( shape.Kind == RenderKind.Cylinder )
		{
			var bottom = points.Count;
			points.Add( new[] { 0.0, -half, 0.0 } );
			normals.Add( new[] { 0.0, -1.0, 0.0 } );
			var top = points.Count;
			points.Add( new[] { 0.0, half, 0.0 } );
			normals.Add( new[] { 0.0, 1.0, 0.0 } );
			for ( var side = 0; side < sides; side++ )
			{
				faces.Add( new[] { bottom, (side + 1) % sides, side } );
				var a = (rings - 1) * sides + side;
				var b = (rings - 1) * sides + (side + 1) % sides;
				faces.Add( new[] { top, a, b } );
			}
		}

		var text = new StringBuilder();
		text.Append( "# " ).Append( shape.Name ).Append( ": Unity's built-in primitive in s&box units (inches), Unity axes (Y up).\n" );
		text.Append( "o " ).Append( shape.Name ).Append( '\n' );
		foreach ( var point in points )
			text.Append( "v " ).Append( Number( point[0] ) ).Append( ' ' ).Append( Number( point[1] ) ).Append( ' ' ).Append( Number( point[2] ) ).Append( '\n' );
		foreach ( var normal in normals )
			text.Append( "vn " ).Append( Number( normal[0] ) ).Append( ' ' ).Append( Number( normal[1] ) ).Append( ' ' ).Append( Number( normal[2] ) ).Append( '\n' );
		foreach ( var face in faces )
			text.Append( "f " ).Append( string.Join( " ", Array.ConvertAll( face, i => (i + 1) + "//" + (i + 1) ) ) ).Append( '\n' );
		return text.ToString();
	}

	static string Number( double value ) => Math.Round( value, 5 ).ToString( "0.#####", CultureInfo.InvariantCulture );
}