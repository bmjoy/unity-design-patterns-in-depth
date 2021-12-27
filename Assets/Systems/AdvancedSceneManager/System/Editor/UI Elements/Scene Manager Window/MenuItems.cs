using AdvancedSceneManager.Editor.Utility;
using System;
using UnityEditor;

namespace AdvancedSceneManager.Editor
{

    static class MenuItems
    {

        [MenuItem("Tools/Advanced Scene Manager/Window/Scene Manager Window", priority = 40)] private static void Item5() => MenuItemHelper.InvokeThisMenuItem();
        [MenuItem("Tools/Advanced Scene Manager/Window/Scene overview", priority = 41)] private static void Item10() => MenuItemHelper.InvokeThisMenuItem();
        [MenuItem("Tools/Advanced Scene Manager/Window/Cross-scene reference debugger", priority = 52)] private static void Item7() => MenuItemHelper.InvokeThisMenuItem();
        [MenuItem("Tools/Advanced Scene Manager/Window/Callback analyzer", priority = 53)] private static void Item11() => MenuItemHelper.InvokeThisMenuItem();
        [MenuItem("Tools/Advanced Scene Manager/Window/Package Manager", priority = 64)] private static void Item6() => MenuItemHelper.InvokeThisMenuItem();

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

            MenuItemHelper.Setup(((Action)Item5).Method, onClick: SceneManagerWindow.Open);
            MenuItemHelper.Setup(((Action)Item6).Method, onClick: () => UnityEditor.PackageManager.UI.Window.Open("plugin.asm.package-manager"));
            MenuItemHelper.Setup(((Action)Item7).Method, onClick: CrossSceneDebugger.Open);
            MenuItemHelper.Setup(((Action)Item11).Method, onClick: Callbacks.CallbackUtility.Open);
            MenuItemHelper.Setup(((Action)Item10).Method, onClick: SceneOverviewWindow.Open);

        }

    }

}
