The keep/replace options don't give any visual feedback upon clicking nor does it appear they actually do anything.
When passing a rather large file, the process bar isn't verbose enough, just hangs at 80% until the job is done.

In the Proton-Wrapped-Exe version of the editor, clicking the "Import Unity Package" button produces this error:
Exception has been thrown by the target of an invocation.
‘System.10.1OException: import destination cannot pass through a symbolic link: Z:\nome
ImportUnityPackage.Unityimport.CheckDirectory
ZAhome\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\Editor\Cora\Unityimport.cs:424
ImportUnityPackage.importWorkspace.CleanupAbandoned
ZAhome\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\Editor\Core\importWorkspace.cs:34
ImportUnityPackage.UnityPackageWindow..ctor
ZAhome\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\Editor\UnityPackageWindow.cs:57
ImportUnityPackage.UnityPackageMenu.Open
ZAhome\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\Editor\UnityPackageMenu.cs:11
at System-RuntimeMethodHandle.invokeMethod (ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
at System Reflection.MethodBaseinvoker.interpretedinvoke_Method (Object obj, IntPtr* args)
at System Reflection.RuntimeMethodinfo.invoke(Object obj, BindingFiags invokeAttr, Binder binder, Object[] parameters, Cultureinfo culture)s



Import issues I found:

Strange Model Colliders:
Z:{cseliot}/1-Development/1-SBox/2-Projects/unityimporterplus/Assets/imported/polygoncity/prefabs/environments/sm_env_road_patch_01.prefab 

Missing Scale from Unity Prefab:
Z:{cseliot}/1-Development/1-SBox/2-Projects/unityimporterplus/Assets/imported/polygoncity/prefabs/environments/sm_env_road_patch_01.prefab

Missing Box Collider:
Z:{cseliot}/1-Development/1-SBox/2-Projects/unityimporterplus/Assets/imported/polygoncity/prefabs/environments/sm_env_road_yellowlines_01.prefab
Z:{cseliot}/1-Development/1-SBox/2-Projects/unityimporterplus/Assets/imported/polygoncity/prefabs/environments/sm_env_fence_01.prefab
Z:{cseliot}/1-Development/1-SBox/2-Projects/unityimporterplus/Assets/imported/polygoncity/prefabs/environments/sm_env_bridge_underside_01.prefab

