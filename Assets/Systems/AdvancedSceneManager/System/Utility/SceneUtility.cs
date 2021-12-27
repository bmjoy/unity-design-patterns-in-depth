#pragma warning disable IDE0054 // Use compound assignment
#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Core;
using System.Collections;
using Lazy.Utility;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>An utility class to perform actions on scenes.</summary>
    public static class SceneUtility
    {

        /// <summary>Get all open unity scenes.</summary>
        public static IEnumerable<scene> GetAllOpenUnityScenes()
        {
            for (int i = 0; i < sceneManager.sceneCount; i++)
                yield return sceneManager.GetSceneAt(i);
        }

        /// <summary>Gets if there are any scenes open that are not dynamically created, and not yet saved to disk.</summary>
        public static bool hasAnyScenes => sceneManager.sceneCount > 0 && !(sceneCount == 1 && string.IsNullOrWhiteSpace(sceneManager.GetSceneAt(0).name));

        /// <inheritdoc cref="sceneManager.sceneCount"/>
        public static int sceneCount => sceneManager.sceneCount;

        /// <inheritdoc cref="sceneManager.MoveGameObjectToScene(GameObject, scene)"/>
        public static void MoveObject(GameObject obj, OpenSceneInfo scene) =>
            MoveObject(obj, scene.unityScene ?? default);

        /// <inheritdoc cref="sceneManager.MoveGameObjectToScene(GameObject, scene)"/>
        public static void MoveObject(GameObject obj, scene scene)
        {

            if (!scene.IsValid())
                return;

            sceneManager.MoveGameObjectToScene(obj, scene);

        }

        #region Create

        /// <summary>Creates a scene at runtime, that is not saved to disk.</summary>
        public static OpenSceneInfo CreateDynamic(string name, UnityEngine.SceneManagement.LocalPhysicsMode localPhysicsMode = UnityEngine.SceneManagement.LocalPhysicsMode.None)
        {

            var scene = sceneManager.CreateScene(name, new UnityEngine.SceneManagement.CreateSceneParameters(localPhysicsMode));
            return new OpenSceneInfo(null, scene, SceneManager.standalone);

        }

#if UNITY_EDITOR

        /// <summary>Creates a scene, using save prompt for path. Returns <see langword="null"/> if save dialog cancelled. Only usable in editor.</summary>
        /// <param name="collection">The collection to add the scene to.</param>
        /// <param name="index">The index of the scene in <paramref name="collection"/>, no effect if <paramref name="collection"/> is <see langword="null"/>.</param>
        /// <param name="replaceIndex">Replaces the scene at the specified index, rather than insert it.</param>
        /// <param name="save">Save collection to disk.</param>
        public static void Create(Action<Scene> onCreated, SceneCollection collection = null, int? index = null, bool replaceIndex = false, bool save = true)
        {

            Coroutine().StartCoroutine();
            IEnumerator Coroutine()
            {

                if (!CreateAndPromptSaveNewScene(out var path))
                    yield break;

                SceneAsset asset = null;

                while (!asset)
                {
                    yield return null;
                    asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                }

                var scene = SceneManager.assetManagement.Add(asset);
                AddToCollection(scene, collection, index, replaceIndex, save);
                onCreated?.Invoke(scene);

            }

        }

        /// <summary>Creates a scene at the specified path.</summary>
        /// <param name="path">The path that the scene should be saved to.</param>
        /// <param name="collection">The collection to add the scene to.</param>
        /// <param name="index">The index of the scene in <paramref name="collection"/>, no effect if <paramref name="collection"/> is <see langword="null"/>.</param>
        /// <param name="replaceIndex">Replaces the scene at the specified index, rather than insert it.</param>
        /// <param name="save">Save collection to disk.</param>
        public static Scene Create(string path, SceneCollection collection = null, int? index = null, bool replaceIndex = false, bool save = true) =>
            Create(path, collection, index, replaceIndex, save, createSceneScriptableObject: true);

        /// <inheritdoc cref="Create(string, SceneCollection, int?, bool, bool)"/>
        /// <param name="createSceneScriptableObject">If <see langword="false"/>, no <see cref="Scene"/> <see cref="ScriptableObject"/> will be created, scene also won't be added to <paramref name="collection"/>. Returns <see langword="null"/>.</param>
        public static Scene Create(string path, SceneCollection collection = null, int? index = null, bool replaceIndex = false, bool save = true, bool createSceneScriptableObject = true)
        {

            if (path is null)
                throw new ArgumentNullException(nameof(path));

            path = path.Replace('\\', '/');

            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path;

            if (!path.EndsWith(".unity"))
                path += ".unity";

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (!sceneAsset)
            {

                const string template = "" +
                    "%YAML 1.1\n" +
                    "%TAG !u! tag:unity3d.com,2011:";

                Directory.GetParent(path).Create();

                if (File.Exists(path) && File.ReadAllText(path) == template)
                    return null;

                File.WriteAllText(path, template);

                AssetDatabase.ImportAsset(path);

                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            }

            if (!sceneAsset)
                throw new Exception("Something went wrong when creating scene.");

            if (!createSceneScriptableObject)
                return null;

            var Scene = SceneManager.assetManagement.Add(sceneAsset);
            AddToCollection(Scene, collection, index, replaceIndex, save);
            return Scene;

        }

        static bool CreateAndPromptSaveNewScene(out string path)
        {

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var saved = EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene });
            path = scene.path;
            _ = EditorSceneManager.CloseScene(scene, true);

            return saved;

        }

        static void AddToCollection(Scene scene, SceneCollection collection = null, int? index = null, bool replaceIndex = false, bool save = true)
        {

            if (collection)
            {

                var scenes = collection.scenes;

                if (index.HasValue && replaceIndex) //Replace
                    scenes[index.Value] = scene;
                else if (index.HasValue) //Insert
                    ArrayUtility.Insert(ref scenes, index.Value, scene);
                else //Add
                    ArrayUtility.Add(ref scenes, scene);

                collection.scenes = scenes;

                if (save)
                {
                    EditorUtility.SetDirty(collection);
                    AssetDatabase.SaveAssets();
                }

            }

        }

#endif

        #endregion
        #region Remove

#if UNITY_EDITOR

        /// <summary>Removes the <see cref="SceneAsset"/> at the specified path and its associated <see cref="Scene"/>, and removes any references to it from any <see cref="SceneCollection"/>.</summary>
        public static void Remove(string path)
        {

            if (path is null)
                throw new ArgumentNullException(nameof(path));

            path = path.Replace('\\', '/');

            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path;

            if (!path.EndsWith(".unity"))
                path += ".unity";


            AssetDatabase.DisallowAutoRefresh();

            foreach (var collection in SceneManager.assetManagement.collections)
                if (collection.m_scenes.Contains(path))
                {
                    ArrayUtility.Remove(ref collection.m_scenes, path);
                    EditorUtility.SetDirty(collection);
                }

            SceneManager.assetManagement.Remove(SceneManager.assetManagement.FindSceneByPath(path));
            AssetDatabase.DeleteAsset(path);

            AssetDatabase.AllowAutoRefresh();
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

        }

        /// <summary>Removes the <paramref name="scene"/> and its associated <see cref="SceneAsset"/>, and removes any references to it from any <see cref="SceneCollection"/>.</summary>
        public static void Remove(Scene scene) =>
            Remove(scene ? scene.path : null);

#endif

        #endregion
        #region Find

        /// <summary>Find open scenes by name.</summary>
        public static IEnumerable<Scene> FindOpen(string name) =>
            FindOpen(s => s.name == name);

        /// <summary>Find open scenes by predicate.</summary>
        public static IEnumerable<Scene> FindOpen(Func<Scene, bool> predicate) =>
            GetScenes(openOnly: true).Where(predicate);

        /// <summary>Find scenes by name, in the specified collection or profile, if defined.</summary>
        public static IEnumerable<Scene> Find(string name, SceneCollection inCollection = null, Profile inProfile = null) =>
            Find(s => s.name == name, inCollection, inProfile);

        /// <summary>Find scenes by predicate, in the specified collection or profile, if defined.</summary>
        public static IEnumerable<Scene> Find(Func<Scene, bool> predicate, SceneCollection inCollection = null, Profile inProfile = null) =>
            GetScenes(inCollection, inProfile).Where(predicate);

        static Scene[] GetScenes(SceneCollection collection = null, Profile profile = null, bool openOnly = false)
        {

            if (openOnly)
                return GetAllOpenUnityScenes().Select(s => s.Scene().scene).Where(s => s).ToArray();
            else if (profile && collection)
                return profile.collections.Contains(collection)
                    ? collection.scenes
                    : Array.Empty<Scene>();
            else if (profile)
                return profile.scenes.ToArray();
            else if (collection)
                return collection.scenes;
            else
                return SceneManager.assetManagement.scenes.ToArray();

        }

        #endregion

#if UNITY_EDITOR
        #region Split

        static scene? newScene;
        [MenuItem("GameObject/Move game objects to new scene...", false, 11)]
        static void MoveToNewSceneItem() =>
            MoveToNewScene(Selection.objects.OfType<GameObject>().ToArray());

        [MenuItem("GameObject/Move game objects to new scene...", true)]
        static bool ValidateMoveToNewSceneItem() =>
            Selection.objects.Any();

        /// <summary>
        /// <para>Moves the object to a new scene.</para>
        /// <para>Only available in editor.</para>
        /// </summary>
        public static void MoveToNewScene(params GameObject[] objects)
        {

            newScene = newScene ?? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            foreach (var obj in objects)
            {
                obj.transform.SetParent(null, worldPositionStays: true);
                sceneManager.MoveGameObjectToScene(obj, newScene.Value);
            }

            EditorSceneManager.MarkSceneDirty(newScene.Value);

        }

        class AssetModificationProcessor : UnityEditor.AssetModificationProcessor
        {

            static string[] OnWillSaveAssets(string[] paths)
            {
                newScene = null;
                return paths;
            }

        }

        #endregion
        #region Merge

        [MenuItem("Assets/Merge scenes...", priority = 200)]
        static void MergeSceneItem() =>
            MergeScenes(Selection.objects.OfType<SceneAsset>().Select(a => AssetDatabase.GetAssetPath(a)).ToArray());

        [MenuItem("Assets/Merge scenes...", validate = true)]
        static bool ValidateMergeSceneItem() =>
            Selection.objects.OfType<SceneAsset>().Count() > 1;

        /// <summary>
        /// <para>Merges the scenes together, the first scene in the list will be the output scene.</para>
        /// <para>Only available in editor.</para>
        /// </summary>
        public static void MergeScenes(params string[] scenes)
        {

            if (scenes.Count() < 2)
                return;

            var displayNames = scenes.Distinct().Select(s => s.Replace("Assets/", "").Replace(".unity", "")).ToArray();
            if (GenericPrompt.Prompt(title: "Combining scenes", message: "Are you sure you wish to combine the following scenes? This is not reversable." + Environment.NewLine +
                string.Join(Environment.NewLine, displayNames) + Environment.NewLine + Environment.NewLine +
                "Resulting scene: " + scenes.First())
                && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {

                var firstScene = EditorSceneManager.OpenScene(scenes.First());

                foreach (var scene in scenes)
                    if (firstScene.path != scene)
                    {
                        sceneManager.MoveGameObjectToScene(new GameObject("--" + Path.GetFileNameWithoutExtension(scene) + "--"), firstScene);
                        var openScene = EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);
                        foreach (var obj in openScene.GetRootGameObjects())
                        {
                            obj.transform.SetParent(null, worldPositionStays: true);
                            sceneManager.MoveGameObjectToScene(obj, firstScene);
                            obj.transform.SetAsLastSibling();
                        }
                    }

                EditorSceneManager.MarkSceneDirty(firstScene);
                EditorSceneManager.SaveScene(firstScene);

                foreach (var scene in GetAllOpenUnityScenes().Skip(1).ToArray())
                    EditorSceneManager.CloseScene(scene, true);

                foreach (var scene in scenes.Skip(1).ToArray())
                    AssetDatabase.DeleteAsset(scene);

                AssetDatabase.Refresh();

            }

            CoroutineUtility.Run(AssetRefreshHelper.Refresh, 0.5f);

        }

        #endregion
#endif

    }

}
