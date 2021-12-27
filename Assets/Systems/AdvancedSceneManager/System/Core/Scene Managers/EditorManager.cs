#pragma warning disable IDE0051 // Remove unused private members

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.SceneManager;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Core
{

    /// <summary>A simplified scene manager for managing scenes in editor.</summary>
    /// <remarks>Only available in editor.</remarks>
    public class Editor
    {

        public event Action scenesUpdated;

        /// <summary>When <see langword="true"/>: opens the first found collection that a scene is contained in when opening an SceneAsset in editor.</summary>
        public bool openAssociatedCollectionOnSceneAssetOpen
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.OpenAssociatedCollectionOnSceneAssetOpen", true);
            set => EditorPrefs.SetBool("AdvancedSceneManager.OpenAssociatedCollectionOnSceneAssetOpen", value);
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

            SettingsTab.instance.Add(header: SettingsTab.instance.DefaultHeaders.Options_Local, callback: () =>
            {

                var element = new Toggle("Open collection when SceneAsset opened:");
                element.tooltip = "This opens the first found collection that a scene is contained in when opening an SceneAsset in editor.";
                element.SetValueWithoutNotify(editor.openAssociatedCollectionOnSceneAssetOpen);
                _ = element.RegisterValueChangedCallback(e => editor.openAssociatedCollectionOnSceneAssetOpen = e.newValue);
                return element;

            });

            if (!Application.isPlaying)
                return;

            Coroutine().StartCoroutine();
            IEnumerator Coroutine()
            {

                while (!runtime.isInitialized || !Application.isPlaying)
                    yield return null;

                if (!Profile.current)
                    yield break;

                var scenes = SceneUtility.GetAllOpenUnityScenes().ToArray();
                var collections = Profile.current.collections.Where(c => c && c.scenes.Where(s => s && s.tag.openBehavior == SceneOpenBehavior.OpenNormally).All(s => scenes.Any(s1 => s1.path == s.path))).ToArray();

                //Since play mode was entered through regular play button, 
                //lets try to collection and standalone scenes and add them to the scene manager
                if (Application.isPlaying && !runtime.wasStartedAsBuild)
                {

                    var collection = collections.OrderByDescending(c => c.scenes.Any(s => s && s.isActive)).FirstOrDefault();

                    if (collection)
                        SceneManager.collection.Set(collection, SceneManager.standalone.openScenes.Where(s => collection.scenes.Contains(s.scene)).ToArray());

                    var scenesInCollection = collection ? scenes.Where(s => collection.scenes.Any(s1 => s1 ? s1.path == s.path : false)).ToArray() : Array.Empty<scene>();

                    var scenesToAdd = scenes.
                        Except(scenesInCollection).
                        Where(s => !standalone.openScenes.Any(s1 => s1.scene.path == s.path) && !DefaultSceneUtility.IsDefaultScene(s)).ToArray();

                    foreach (var scene in utility.openScenes)
                        if (scene.unityScene.HasValue)
                            PersistentUtility.Set(scene.unityScene.Value, scene.scene.tag.closeBehavior);

                    foreach (var scene in scenesToAdd)
                        if (assetManagement.FindSceneByPath(scene.path) is Scene s)
                            standalone.Add(new OpenSceneInfo(s, scene, standalone));

                    foreach (var scene in scenesInCollection)
                        if (assetManagement.FindSceneByPath(scene.path) is Scene s)
                            SceneManager.collection.Add(new OpenSceneInfo(s, scene, standalone));

                }

                editor.collections.AddRange(collections);
                editor.scenesUpdated?.Invoke();

            }

        }

        [OnOpenAsset]
        static bool OpenSingle(int instanceID, int _)
        {

            if (EditorUtility.InstanceIDToObject(instanceID) is SceneAsset scene)
            {

                var path = AssetDatabase.GetAssetPath(scene);
                if (editor.openAssociatedCollectionOnSceneAssetOpen && assetManagement.collections?.FirstOrDefault(c => c && (c.scenes?.Any(s => s && s.path == path) ?? false)) is SceneCollection collection)
                    editor.Open(collection, promptSave: false);
                else
                    editor.OpenSingle(scene, promptSave: false);

                return true;

            }

            return false;

        }

        public void OpenSingle(SceneAsset scene, bool promptSave = true) =>
            OpenSingle(assetManagement.scenes.Find(AssetDatabase.GetAssetPath(scene)), promptSave);

        public void OpenSingle(Scene scene, bool promptSave = true)
        {

            if (!scene)
                return;

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            CloseAll(promptSave: false);

            Open(scene.path);
            if (!Application.isPlaying)
                DefaultSceneUtility.Close();

            PersistentSceneInEditorUtility.OpenAssociatedPersistentScenes(scene, promptSave: false);
            scenesUpdated?.Invoke();

        }

        public void Open(Scene scene, bool promptSave = true)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (scene)
                Open(scene.path);
            scenesUpdated?.Invoke();

        }

        /// <summary>Opens scene without save prompts. Supports opening scene as readonly, if <see cref="LockUtility"/> is used.</summary>
        internal scene Open(string path) =>
            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        readonly List<SceneCollection> collections = new List<SceneCollection>();
        public bool IsOpen(SceneCollection collection) => collections.Contains(collection);
        public bool CanClose(SceneCollection collection) => !(collections.Count == 1 && collections.First() == collection);

        public void Open(SceneCollection collection, bool additive = false, bool promptSave = true, bool ignoreTags = false)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!additive)
                CloseAll(promptSave: false);
            else if (IsOpen(collection))
                Close(collection, promptSave: false);

            collections.Add(collection);

            var scenes = SceneUtility.GetAllOpenUnityScenes().ToArray();
            var persistentScenes = collection.scenes.SelectMany(s => PersistentSceneInEditorUtility.GetAssociatedScenes(s)).Distinct().Where(s => !scenes.Any(s1 => s1.path == s.path)).ToArray();
            foreach (var scene in persistentScenes)
                Open(scene, promptSave: false);

            foreach (var scene in collection.scenes)
                if (scene.tag.openBehavior == SceneOpenBehavior.OpenNormally || ignoreTags)
                    Open(scene, promptSave: false);

            if (!Application.isPlaying)
                DefaultSceneUtility.Close();
            scenesUpdated?.Invoke();

            var active = collection.activeScene;
            if (!active) active = collection.scenes.FirstOrDefault();
            if (active)
            {
                var uScene = SceneUtility.GetAllOpenUnityScenes().FirstOrDefault(s => s.path == active.path);
                utility.SetActive(uScene);
            }

        }

        public void Close(SceneCollection collection, bool promptSave = true)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            DefaultSceneUtility.EnsureOpen();
            collections.Remove(collection);

            var scenes = collection.scenes.Where(s => !collections.Any(c => c.scenes.Contains(s)));

            foreach (var scene in scenes)
                Close(scene, promptSave: false);

            if (!Application.isPlaying && SceneUtility.GetAllOpenUnityScenes().Count() > 1)
                DefaultSceneUtility.Close();

            scenesUpdated?.Invoke();

        }

        public void Close(Scene scene, bool promptSave = true)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (scene)
                Close(EditorSceneManager.GetSceneByPath(scene.path), promptSave: false);
            scenesUpdated?.Invoke();

        }

        public void Close(scene scene, bool promptSave = true)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!scene.IsValid())
                return;

            if (SceneUtility.GetAllOpenUnityScenes().Count() > 1)
                EditorSceneManager.CloseScene(scene, true);

            scenesUpdated?.Invoke();

        }

        public void CloseAll(bool promptSave = true)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            DefaultSceneUtility.EnsureOpen();

            foreach (var s in SceneUtility.GetAllOpenUnityScenes().ToArray())
            {
                if (DefaultSceneUtility.IsDefaultScene(s))
                    continue;
                EditorSceneManager.CloseScene(s, true);
            }

            collections.Clear();
            scenesUpdated?.Invoke();

        }

        public bool IsOpen(Scene scene) =>
            SceneUtility.GetAllOpenUnityScenes().Any(s => scene && s.path == scene.path);

    }

}
#endif