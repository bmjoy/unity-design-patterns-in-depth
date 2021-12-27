using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;
using System.Linq;
using UnityEditor;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
using UnityEditor.SceneManagement;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>
    /// <para>An utility class that manages the default scene, called 'AdvancedSceneManager'.</para>
    /// <para>The default scene allows us to more easily close all scenes when we need to, since unity requires at least one scene to be open at any time.</para>
    /// </summary>
    public static class DefaultSceneUtility
    {

        #region Scene actived callback

        static bool cancelNextActive;
        internal static void OnBeforeActiveSceneChanged(scene oldScene, scene newScene, out bool cancel)
        {

            //Prevents default scene from getting activated

            cancel = false;
            if (IsDefaultScene(newScene))
            {
                cancelNextActive = true;
                cancel = true;
                SceneManager.utility.SetActive(oldScene);
            }

            if (cancelNextActive)
            {
                cancel = true;
                cancelNextActive = false;
            }

        }

        #endregion
        #region HierarchyGUI

#if UNITY_EDITOR

        [InitializeOnLoadMethod]
        static void OnLoad() =>
            HierarchyGUIUtility.AddSceneGUI(OnGUI, width: 82, index: 10);

        static bool OnGUI(Rect position, scene scene)
        {

            if (IsDefaultScene(scene))
            {
                var c = GUI.color;
                GUI.color = new Color(0.65f, 0.65f, 0.65f);
                GUI.Label(position, "Default scene");
                GUI.color = c;
                return true;
            }

            return false;

        }

#endif

        #endregion

        public const string Name = "AdvancedSceneManager";
        static string StartupScenePath = $"Assets/Settings/{Name}.unity";

        internal static void EnsureOpen()
        {

            if (!FindOpenScene(out var scene))
                CreateScene(out scene);

            if (scene.HasValue)
            {
                if (string.IsNullOrWhiteSpace(scene.Value.path))
                {
                    var s = scene.Value;
                    s.name = Name;
                }
                PersistentUtility.Set(scene.Value, Models.SceneCloseBehavior.KeepOpenAlways);
            }
            else
                Debug.LogError("Could not open default scene. Things may not work as expected.");

        }

        static bool FindOpenScene(out scene? scene)
        {

            scene = null;
            foreach (var s in SceneUtility.GetAllOpenUnityScenes())
                if ((s.path == "" || s.path == StartupScenePath) && (s.name == Name || s.name == "") && s.IsValid())
                    scene = s;

            return scene.HasValue;

        }

        static void CreateScene(out scene? scene)
        {

            scene = null;
            if (Application.isPlaying)
            {
                scene = sceneManager.CreateScene(Name);
                PersistentUtility.Set(scene.Value, Models.SceneCloseBehavior.KeepOpenAlways);
            }
            else
            {
#if UNITY_EDITOR
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
#endif
            }

        }

        /// <summary>Gets whatever the default scene is open.</summary>
        internal static bool isOpen =>
            FindOpenScene(out _);

        /// <summary>Gets whatever the specified scene is the default scene.</summary>
        public static bool IsDefaultScene(scene scene) =>
            scene.IsValid() && scene.name == Name && (scene.path == "" || scene.path == StartupScenePath);

#if UNITY_EDITOR

        /// <summary>Close the default scene.</summary>
        ///<remarks>Only available in editor.</remarks>
        internal static void Close()
        {


            if (SceneUtility.GetAllOpenUnityScenes().Count() == 1 && isOpen)
                return;

            if (FindOpenScene(out var scene))
                if (Application.isPlaying)
                    _ = sceneManager.UnloadSceneAsync(scene.Value);
                else
                    _ = EditorSceneManager.CloseScene(scene.Value, true);

        }

        internal static string GetStartupScene()
        {

            //SceneManager.assetManagement.Ignore(StartupScenePath);
            _ = SceneUtility.Create(StartupScenePath, createSceneScriptableObject: false);

            return StartupScenePath;

        }

#endif

    }

}
