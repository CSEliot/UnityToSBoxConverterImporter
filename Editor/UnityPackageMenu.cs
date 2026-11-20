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
}
