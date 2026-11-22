using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImportUnityPackage;

/// <summary>
/// Engine-free detection and folder enumeration for Unity project directories.
/// Shared by the headless harness and the in-editor window so both use the same rules.
/// </summary>
public static class UnityProjectFolders
{
	// Exposed criteria (Stylistic Task Notes: tweakable, never inline magic).
	public const string AssetsFolderName = "Assets";
	public const string ProjectSettingsFolderName = "ProjectSettings";

	/// <summary>Files directly in Assets/ are not offered as a subfolder; this controls their inclusion.</summary>
	public static bool IncludeAssetsRootFiles = true;

	/// <summary>A Unity project needs both an Assets/ and a ProjectSettings/ directory.</summary>
	public static bool IsValidUnityFolder( string folder )
	{
		if ( string.IsNullOrWhiteSpace( folder ) || !Directory.Exists( folder ) ) return false;
		return Directory.Exists( Path.Combine( folder, AssetsFolderName ) ) &&
			Directory.Exists( Path.Combine( folder, ProjectSettingsFolderName ) );
	}

	/// <summary>Immediate subfolder names of Assets/, sorted for a stable checkbox list.</summary>
	public static string[] ListAssetFolders( string folder )
	{
		if ( !IsValidUnityFolder( folder ) ) return Array.Empty<string>();
		return Directory.EnumerateDirectories( Path.Combine( folder, AssetsFolderName ), "*", SearchOption.TopDirectoryOnly )
			.Select( Path.GetFileName ).OrderBy( n => n, StringComparer.OrdinalIgnoreCase ).ToArray();
	}

	/// <summary>Is a SafePath-relative asset path covered by the selected Assets/ subfolders?</summary>
	public static bool IsPathSelected( string path, IReadOnlyCollection<string> selectedFolders )
	{
		var slash = path.IndexOf( '/' );
		if ( slash < 0 ) return IncludeAssetsRootFiles;
		var root = path[..slash];
		return selectedFolders != null && selectedFolders.Any( f => string.Equals( f, root, StringComparison.OrdinalIgnoreCase ) );
	}
}
