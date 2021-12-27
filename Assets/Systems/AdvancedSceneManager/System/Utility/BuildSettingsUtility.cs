#if UNITY_EDITOR
#pragma warning disable IDE0051 // Remove unused private members

using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using static AdvancedSceneManager.SceneManager;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>
    /// <para>An utility class for managing build settings scene order.</para>
    /// <para>Cannot use in build.</para>
    /// </summary>
    public static class BuildSettingsUtility
    {

        #region Extensibility

        //Required for support packages

        #region Override scenes

        public enum Reason
        {
            None, NotIncludedInProfile, IncludedInProfile, IsAddressable, Overriden
        }

        static readonly Dictionary<string, (bool? state, Reason reason)> overridenScenes = new Dictionary<string, (bool? state, Reason reason)>();

        internal static void ClearAddressableScenes()
        {
            foreach (var scene in overridenScenes.ToArray())
                if (scene.Value.reason == Reason.IsAddressable)
                    overridenScenes.Remove(scene.Key);
        }

        internal static void OverrideSceneEnabledState(string scene, bool? enabled, Reason reason = Reason.Overriden) =>
            overridenScenes.Set(scene, (enabled, reason));

        static bool IsOverriden(string path) =>
            overridenScenes.GetValue(path).state.HasValue;

        #endregion
        #region Callbacks

        static readonly List<Action> changeCallbacks = new List<Action>();
        internal static void AddBuildSettingsCallback(Action callback)
        {
            if (!changeCallbacks.Contains(callback))
                changeCallbacks.Add(callback);
        }

        internal static void RemoveBuildSettingsCallback(Action callback) =>
            changeCallbacks.Remove(callback);

        internal static void Callbacks()
        {
            overridenScenes.Clear();
            foreach (var callback in changeCallbacks)
                callback?.Invoke();
        }

        #endregion

        #endregion

        /// <summary>Gets the enabled state of a scene.</summary>
        public static (bool enabled, Reason reason) GetEnabledState(string path)
        {
            var (state, reason) = overridenScenes.GetValue(path);
            if (state.HasValue)
                return (state.Value, reason);
            else if (Profile.current && Profile.current.scenes.Any(s => s.path == path))
                return (true, Reason.IncludedInProfile);
            else
                return (false, Reason.NotIncludedInProfile);
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

            EditorBuildSettings.sceneListChanged -= OnBuildSettingsChanged;
            EditorBuildSettings.sceneListChanged += OnBuildSettingsChanged;

            EditorApplication.playModeStateChanged += mode =>
            {
                if (mode == PlayModeStateChange.ExitingEditMode)
                    UpdateBuildSettings();
            };

        }

        /// <summary>Enable or disable editing of scene build settings.</summary>
        public static bool AllowEditingOfBuildSettings
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.AllowEditingOfBuildSettings", true);
            set => EditorPrefs.SetBool("AdvancedSceneManager.AllowEditingOfBuildSettings", value);
        }

        static readonly List<object> isUpdatingBuildSettings = new List<object>();
        static void OnBuildSettingsChanged()
        {

            if (isUpdatingBuildSettings.Any())
                return;

            Callbacks();

            var modifiedScenes = ModifiedScenes(GetOrderedList().Select(s => s.buildScene), EditorBuildSettings.scenes).ToArray();
            var containsInvalidChanges = false;
            foreach (var scene in modifiedScenes)
            {

                var s = assetManagement.FindSceneByPath(scene.scene.path);
                if (!scene.isChangeAllowed)
                    containsInvalidChanges = true;
                else if (assetManagement.scenes.Find(scene.scene.path) is Scene s1 && Profile.current)
                    Profile.current.SetStandalone(s1, scene.scene.enabled);

            }

            if (AllowEditingOfBuildSettings || !containsInvalidChanges)
                return;

            UpdateBuildSettings();

        }

        /// <summary>Updates the scene build settings.</summary>
        public static void UpdateBuildSettings()
        {

            var o = new object();
            isUpdatingBuildSettings.Add(o);

            var buildScenes =
                SceneManager.assetManagement.scenes.Where(s => s && s.path.Contains("/AdvancedSceneManager/System/LoadingScreens/")).Select(s => new EditorBuildSettingsScene(s.path, true)).
                Concat(GetOrderedList().Select(s => s.buildScene)).ToList();

            buildScenes.Insert(0, new EditorBuildSettingsScene(DefaultSceneUtility.GetStartupScene(), true));

            EditorBuildSettings.scenes = buildScenes.ToArray();

            isUpdatingBuildSettings.Remove(o);


        }

        /// <summary>Gets the scenes that was modified, and as to whatever this change was allowed, since since scenes that are contained in collections are forced to be included.</summary>
        static IEnumerable<(EditorBuildSettingsScene scene, bool isChangeAllowed)>
        ModifiedScenes(IEnumerable<EditorBuildSettingsScene> oldScenes, IEnumerable<EditorBuildSettingsScene> newScenes)
        {

            if (!Profile.current)
                return Array.Empty<(EditorBuildSettingsScene, bool)>();

            var profile = Profile.current.scenes.ToArray();

            return oldScenes.
                Select(s => (oldScene: s, newScene: newScenes.FirstOrDefault(s1 => s1.path == s.path))).
                Where(s => s.newScene != null).
                Where(s => s.oldScene.enabled != s.newScene.enabled).
                Select(s =>
                {
                    var isOverriden = IsOverriden(s.newScene.path);
                    var isInProfile = profile.Any(s1 => s1.path == s.newScene.path);
                    var isChangeAllowed = !isOverriden && !isInProfile;
                    return (s.newScene, isChangeAllowed);
                });


        }

        /// <summary>Get an ordered list of all scenes that would be set as scene build settings.</summary>
        public static IEnumerable<(Scene scene, EditorBuildSettingsScene buildScene, Reason reason)> GetOrderedList()
        {

            if (!Profile.current)
                return Array.Empty<(Scene scene, EditorBuildSettingsScene buildScene, Reason reason)>();

            return Profile.current.scenes.
                Where(s => s).
                Distinct().
                OrderByDescending(s => s == Profile.current.splashScreen).
                ThenByDescending(s => s == Profile.current.loadingScreen).
                Select(s =>
                {
                    var enabled = GetEnabledState(s.path);
                    return (
                        scene: s,
                        buildScene: new EditorBuildSettingsScene(s.path, enabled.enabled),
                        enabled.reason);
                }).
                OrderByDescending(s => s.buildScene.enabled);

        }

        /// <summary>Get if scene is included in build.</summary>
        public static bool IsIncluded(Scene scene) =>
            EditorBuildSettings.scenes.Any(s => s.path == scene.path && (s.enabled || (overridenScenes.GetValue(s.path).state ?? false)));

    }

}
#endif
