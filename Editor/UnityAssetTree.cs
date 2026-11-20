using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace ImportUnityPackage;

public sealed class UnityAssetTree : TreeView
{
	readonly Func<ImportPlan> plan;
	readonly Action changed;
	readonly Action<UnityAsset> describe;
	public UnityAssetTree( Widget parent, Func<ImportPlan> plan, Action changed, Action<UnityAsset> describe ) : base( parent )
	{
		this.plan = plan;
		this.changed = changed;
		this.describe = describe;
		MultiSelect = false;
	}

	public void Load( IEnumerable<UnityAsset> assets )
	{
		var root = new AssetNode( "Assets", this );
		foreach ( var asset in assets )
		{
			var parts = asset.Path.Split( '/' );
			var node = root;
			foreach ( var part in parts[..^1] )
			{
				var folder = node.Children.OfType<AssetNode>().FirstOrDefault( c => c.Asset == null && c.Name == part );
				if ( folder == null ) { folder = new AssetNode( part, this ); node.AddItem( folder ); }
				node = folder;
			}
			node.AddItem( new AssetNode( parts[^1], this ) { Asset = asset } );
		}
		SetItems( new[] { root } );
		Open( root );
	}

	public void SetExpanded( bool expanded )
	{
		foreach ( var root in Items.OfType<TreeNode>() )
		{
			if ( expanded ) Open( root, recursive: true );
			else Close( root, recursive: true );
		}
	}

	public void ChangeExpandedLayer( bool expand )
	{
		var candidates = new List<(TreeNode Node, int Depth)>();
		void Visit( TreeNode node, int depth )
		{
			var children = node.Children.ToArray();
			if ( children.Length == 0 ) return;
			// A child's layout position reflects the actual open state, including
			// changes made with folder arrows or the keyboard, even offscreen.
			var open = TryGetItemRect( children[0], out _ );
			if ( expand != open ) candidates.Add( (node, depth) );
			if ( open ) foreach ( var child in children ) Visit( child, depth + 1 );
		}
		foreach ( var root in Items.OfType<TreeNode>() ) Visit( root, 0 );
		if ( candidates.Count == 0 ) return;
		var layer = expand ? candidates.Min( c => c.Depth ) : candidates.Max( c => c.Depth );
		foreach ( var (node, depth) in candidates.Where( c => c.Depth == layer ) )
		{
			// Clear remembered child expansion so opening one layer cannot reveal several.
			Close( node, recursive: true );
			if ( expand ) Open( node );
		}
	}

	protected override bool OnItemPressed( VirtualWidget item, MouseEvent e )
	{
		if ( !base.OnItemPressed( item, e ) ) return false; // Leave folder expand arrows functional.
		if ( e.LeftMouseButton && item.Object is AssetNode node ) node.ToggleSelection();
		return true;
	}

	sealed class AssetNode : TreeNode
	{
		readonly UnityAssetTree owner;
		public UnityAsset Asset { get; init; }
		public AssetNode( string name, UnityAssetTree owner ) { Name = name; this.owner = owner; }
		IEnumerable<UnityAsset> Leaves => Asset != null ? new[] { Asset } : Children.OfType<AssetNode>().SelectMany( c => c.Leaves );
		/// <summary>Selectable: supported, and for a scene only while scene import is on.</summary>
		bool Eligible( UnityAsset asset ) => asset.Kind != UnityAssetKind.Unsupported && (asset.Kind != UnityAssetKind.Scene || owner.plan()?.Options.Scenes == true);
		public void ToggleSelection()
		{
			owner.describe( Asset );
			// Dependencies remain included until their last selected parent is removed.
			if ( Asset != null && !Asset.Selected && owner.plan()?.Find( Asset ) != null ) return;
			var eligible = Leaves.Where( Eligible ).ToArray();
			var select = !eligible.All( a => owner.plan()?.Find( a ) != null );
			foreach ( var asset in eligible ) asset.Selected = select;
			owner.changed();
		}
		public override void OnKeyPress( KeyEvent e )
		{
			if ( e.Key == KeyCode.Space ) { ToggleSelection(); e.Accepted = true; }
		}
		public override void OnPaint( VirtualWidget item )
		{
			PaintSelection( item );
			var plan = owner.plan();
			var entry = Asset == null ? null : plan?.Find( Asset );
			var eligible = Leaves.Where( Eligible ).ToArray();
			var count = eligible.Count( a => plan?.Find( a ) != null );
			var active = eligible.Length > 0;
			var checkbox = !active || count == 0 ? "check_box_outline_blank" : count == eligible.Length ? "check_box" : "indeterminate_check_box";
			if ( entry is { Explicit: false } ) checkbox = "link";
			Paint.SetPen( active ? Theme.Text : Theme.Text.WithAlpha( 0.35f ) );
			Paint.DrawIcon( item.Rect, checkbox, 18, TextFlag.LeftCenter );
			Paint.DrawIcon( item.Rect.Shrink( 24, 0, 0, 0 ), Asset == null ? "folder" : Asset.Kind switch
			{
				UnityAssetKind.Material => "palette", UnityAssetKind.Texture => "image", UnityAssetKind.Model => "view_in_ar", UnityAssetKind.Prefab => "account_tree", UnityAssetKind.Scene => "public", _ => "description"
			}, 18, TextFlag.LeftCenter );
			var annotation = Asset?.Kind == UnityAssetKind.Unsupported ? " (unsupported)" : entry == null ? Asset?.Kind == UnityAssetKind.Scene && plan?.Options.Scenes != true ? " (scene import off)" : "" :
				$" · {entry.OutputLabel}" + (!entry.Explicit ? " · dependency" : "") + (plan.Issues.Any( i => i.Asset == Asset.Path ) ? " · issue" : "");
			Paint.DrawText( item.Rect.Shrink( 48, 0, 0, 0 ), Name + annotation, TextFlag.LeftCenter );
		}
	}
}
