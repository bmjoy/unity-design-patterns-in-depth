#pragma warning disable IDE0066 // Convert switch statement to expression

using AdvancedSceneManager.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>Manages persistent scenes.</summary>
    public static class PersistentUtility
    {

        #region Indicator

#if UNITY_EDITOR

        static bool showPersistentIndicator
        {
            get => PlayerPrefs.GetInt("AdvancedSceneManager.Appearance.PersistentIndicator", 1) == 1;
            set => PlayerPrefs.SetInt("AdvancedSceneManager.Appearance.PersistentIndicator", value ? 1 : 0);
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

            SettingsTab.instance.Add(() =>
            {
                var toggle = new Toggle("Display persistent indicator:");
                toggle.SetValueWithoutNotify(showPersistentIndicator);
                toggle.RegisterValueChangedCallback(e => { showPersistentIndicator = e.newValue; HierarchyGUIUtility.Repaint(); });
                return toggle;
            }, header: SettingsTab.instance.DefaultHeaders.Appearance);

            HierarchyGUIUtility.AddSceneGUI(OnSceneGUI, width: 64, index: 1);

        }

        static bool OnSceneGUI(Rect position, scene scene)
        {

            if (showPersistentIndicator && GetPersistentOption(scene) != SceneCloseBehavior.Close)
            {
                var c = GUI.color;
                GUI.color = new Color(0.65f, 0.65f, 0.65f);
                GUI.Label(position, "Persistent");
                GUI.color = c;
                return true;
            }

            return false;

        }

#endif

        #endregion

        static readonly Dictionary<scene, SceneCloseBehavior> behaviors = new Dictionary<scene, SceneCloseBehavior>();

        /// <summary>Set <see cref="SceneCloseBehavior"/> for this scene.</summary>
        public static void Set(scene scene, SceneCloseBehavior behavior = SceneCloseBehavior.KeepOpenAlways) =>
            behaviors.Set(scene, behavior);

        /// <summary>Unset and revert to default <see cref="SceneCloseBehavior"/> for this scene.</summary>
        public static void Unset(scene scene) =>
            behaviors.Remove(scene);

        /// <summary>Unsets <see cref="SceneCloseBehavior"/> for all scenes.</summary>
        public static void UnsetAll() =>
            behaviors.Clear();

        /// <summary>Gets the persistent option that is set for this <see cref="scene"/>.</summary>
        public static SceneCloseBehavior GetPersistentOption(scene scene) =>
            behaviors.GetValue(scene);

        /// <summary>Gets if the scene should stay open.</summary>
        internal static bool KeepOpen(this scene scene, params Scene[] scenesToOpen)
        {

            switch (behaviors.GetValue(scene))
            {
                case SceneCloseBehavior.Close:
                    return false;
                case SceneCloseBehavior.KeepOpenIfNextCollectionAlsoContainsScene:
                    return scenesToOpen.Any(s => s.path == scene.path);
                case SceneCloseBehavior.KeepOpenAlways:
                    return true;
                default:
                    return false;
            }

        }

        internal static bool KeepClosed(this scene scene) =>
            KeepClosed(scene.Scene().scene);

        internal static bool KeepClosed(this Scene scene) =>
            scene && scene.tag.openBehavior == SceneOpenBehavior.DoNotOpenInCollection;

    }

}
