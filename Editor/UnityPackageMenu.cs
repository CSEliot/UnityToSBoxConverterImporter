using Editor;

namespace ImportUnityPackage;

public static class UnityPackageMenu
{
	[Menu( "Editor", "Import Unity Package/Import .unitypackage..." )]
	public static void Open()
	{
		var window = new UnityPackageWindow();
		window.Show();
		window.Browse();
	}

	[Menu( "Editor", "Import Unity Package/Import Unity project folder..." )]
	public static void OpenFolder()
	{
		var window = new UnityPackageWindow();
		window.Show();
		window.BrowseFolder();
	}
}
