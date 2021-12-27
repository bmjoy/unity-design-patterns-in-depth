#pragma warning disable CS0414

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System.Collections.ObjectModel;
using System.Collections.Generic;

using Object = UnityEngine.Object;
using Scene = AdvancedSceneManager.Models.Scene;
using static AdvancedSceneManager.SceneManager;
using System.Collections;
using Lazy.Utility;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Core
{

    /// <summary>Manages assets related to ASM.</summary>
    public class AssetManagement
    {

        internal Dictionary<Type, (string resources, string assets)> defaultPaths = new Dictionary<Type, (string resources, string assets)>()
        {
            { typeof(SceneCollection),  (DefaultResourcesPath + "Collections", DefaultAssetPath + "Collections") },
            { typeof(Scene),            (DefaultResourcesPath + "Scenes",      DefaultAssetPath + "Scenes") },
            { typeof(Profile),          (DefaultResourcesPath + "Profiles",    DefaultAssetPath + "Profiles") },
        };

        internal (string resources, string assets) path<T>()
        {
            defaultPaths.TryGetValue(typeof(T), out var r);
            return r;
        }

        internal const string DefaultAssetPath = "Assets/Settings/Resources/AdvancedSceneManager/";
        internal const string DefaultResourcesPath = "AdvancedSceneManager/";

        /// <summary>The collections in this project.</summary>
        public ReadOnlyCollection<SceneCollection> collections => new ReadOnlyCollection<SceneCollection>(AssetRef.instance.collections);

        /// <summary>The scenes in this project.</summary>
        public ReadOnlyCollection<Scene> scenes => new ReadOnlyCollection<Scene>(AssetRef.instance.scenes);

        /// <summary>The scenes in this project.</summary>
        public ReadOnlyCollection<Profile> profiles => new ReadOnlyCollection<Profile>(AssetRef.instance.profiles);

        /// <summary>Called when assets changed.</summary>
        public event Action AssetsChanged = default;

        /// <summary>Called when assets are cleared, by either <see cref="Clear"/> or from ui.</summary>
        public event Action AssetsCleared = default;

        /// <summary>If <see langword="false"/>, then assets will not be refreshed, this will mean that no Scene ScriptableObject will be created when a SceneAsset added, and a Scene will also not be removed when its associated SceneAsset is removed.</summary>
        public bool allowAutoRefresh { get; set; } = true;

        readonly List<Func<bool>> delay = new List<Func<bool>>();
        internal void DelayOnCollectionTitleChanged(Func<bool> canContinue) =>
            delay.Add(canContinue);

        readonly Dictionary<ISceneObject, string> collectionsRenaming = new Dictionary<ISceneObject, string>();
        internal void Rename<T>(T obj, string newName) where T : ScriptableObject, ISceneObject
        {

#if UNITY_EDITOR

            Coroutine().StartCoroutine();
            IEnumerator Coroutine()
            {

                if (obj is SceneCollection collection &&
                    newName.IndexOf(" - ") == -1 &&
                    collection.FindProfile() is Profile profile)
                    newName = profile.prefix + newName;

                if (collectionsRenaming.ContainsKey(obj))
                {
                    collectionsRenaming[obj] = newName;
                    yield break;
                }
                collectionsRenaming.Add(obj, newName);

                bool IsBlocked()
                {
                    foreach (var delay in delay.ToArray())
                    {
                        if (delay?.Invoke() == true)
                            assetManagement.delay.Remove(delay);
                        else
                            return true;
                    }
                    return false;
                }

                while (IsBlocked())
                    yield return null;

                newName = GetPath(obj, collectionsRenaming.GetValue(obj));
                var name = Path.GetFileNameWithoutExtension(newName);
                obj.SetName(name);

                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), name);

                collectionsRenaming.Remove(obj);
                obj.OnPropertyChanged();
                if (obj)
                    EditorUtility.SetDirty(obj);

            }

#endif

        }

        #region Add / Remove

#if UNITY_EDITOR

        internal List<string> ignore = new List<string>();

        /// <summary>Ignores the scene at the specified path.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Ignore(string path) =>
            ignore.Add(path);

#endif

        /// <summary>Find the <see cref="Scene"/> with the associated path (this is the path to the <see cref="SceneAsset"/>).</summary>
        public Scene FindSceneByPath(string path) =>
            scenes.FirstOrDefault(s => s && s.path == path);

#if UNITY_EDITOR

        #region Profile

        /// <summary>Duplicates active profile and assigns it as active.</summary>
        public void DuplicateProfileAndAssign()
        {
            var profile = DuplicateProfile();
            if (profile)
                Profile.current = profile;
        }

        /// <summary>Creates a new profile and assigns it as active.</summary>
        public void CreateProfileAndAssign()
        {
            var profile = CreateProfile();
            if (profile)
                Profile.current = profile;
        }

        /// <summary>Duplicates the active profile.</summary>
        public Profile DuplicateProfile()
        {

            if (!Profile.current)
                return null;

            var profile = CreateProfile(() => Object.Instantiate(Profile.current));
            if (!profile)
                return null;

            var i = 0f;

            profile.m_collections.Clear();
            foreach (var collection in Profile.current.collections)
            {
                i += 1;
                EditorUtility.DisplayProgressBar("Duplicating profile...", "", i / Profile.current.collections.Count);
                var c = Object.Instantiate(collection);
                Add(c, profile, false);
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(c), c.name.Replace("(Clone)", ""));
            }

            EditorUtility.ClearProgressBar();

            return profile;

        }

        /// <summary>Create a new profile.</summary>
        public Profile CreateProfile()
        {
            var profile = CreateProfile(() => ScriptableObject.CreateInstance<Profile>());
            if (!profile)
                return null;
            profile.loadingScreen = LoadingScreenUtility.fade;
            profile.startupLoadingScreen = LoadingScreenUtility.fade;
            AssetRef.instance.Add(profile);
            return profile;
        }

        Profile CreateProfile(Func<Profile> create)
        {

            var profiles = Resources.LoadAll<Profile>("");
            var name = PromptName.Prompt("", CheckDuplicates, CheckValidFilename);

            (bool isValid, string message) CheckDuplicates(string value) =>
                (isValid: !profiles.Any(p => p.name.ToLower() == value.ToLower()), "The name is already in use.");

            (bool isValid, string message) CheckValidFilename(string value) =>
                (isValid: !Path.GetInvalidFileNameChars().Any(c => value.Contains(c)), "The following characters are not valid: " + string.Join("", Path.GetInvalidFileNameChars()));

            if (name.successful)
            {

                var path = path<Profile>().assets + "/" + name.value + ".asset";
                var obj = create?.Invoke();

                EditorFolderUtility.EnsureFolderExists(Path.GetDirectoryName(path));

                AssetDatabase.CreateAsset(obj, path);
                AssetDatabase.ImportAsset(path);

                return obj;

            }

            return null;

        }

        #endregion

        /// <summary>Find the <typeparamref name="T"/> with the associated asset ID.</summary>
        public T FindAssetByID<T>(string assetID) where T : Object =>
            FindAssetByPath<T>(AssetDatabase.GUIDToAssetPath(assetID));

        /// <summary>Find the <typeparamref name="T"/> with the specified path.</summary>
        public T FindAssetByPath<T>(string path) where T : Object
        {
            if (!string.IsNullOrWhiteSpace(path))
                return AssetDatabase.LoadAssetAtPath<T>(path);
            return null;
        }

        string GetPath<T>(T obj, Profile profile = null) where T : ScriptableObject, ISceneObject =>
            GetPath(obj, obj.name, profile);

        string GetPath<T>(T obj, string name, Profile profile = null) where T : ISceneObject
        {
            if (obj is SceneCollection collection)
            {
                if (!profile)
                    profile = collection.FindProfile();
                if (!profile)
                    return "";
                return GetPath(obj, name, path<SceneCollection>().assets + "/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile)), profile.collections.Where(c => c && c != collection).Select(c => ((ScriptableObject)c).name).ToArray());
            }
            else if (obj is Scene scene)
                return GetPath(obj, name, path<Scene>().assets, scenes.Where(s => s != scene).Select(s => s.name).ToArray());
            return "";
        }

        string GetPath<T>(T obj, string name, string path, string[] names) where T : ISceneObject
        {
            if (obj == null)
                return "";
            name = ObjectNames.GetUniqueName(names, name);
            obj.SetName(name);
            return path + "/" + name + ".asset";
        }

        /// <summary>Adds the asset.</summary>
        public void Add<T>(T obj, Profile profile = null, bool import = true) where T : ScriptableObject, ISceneObject
        {

            if (!profile)
                profile = Profile.current;

            //Add collection to profile before getting path, since path relies on profiles
            if (obj is SceneCollection c && profile)
                profile.m_collections.Add(c);

            var path = GetPath(obj, profile);
            if (path == "")
                throw new Exception("Collection was not associated with a profile, please manually add it to one.");

            EditorFolderUtility.EnsureFolderExists(Path.GetDirectoryName(path));

            AssetDatabase.CreateAsset(obj, path);
            if (import)
                AssetDatabase.ImportAsset(path);

            AssetRef.instance.Add(obj);
            AssetsChanged?.Invoke();

        }

        /// <summary>Removes the asset.</summary>
        public void Remove<T>(T obj) where T : ScriptableObject, ISceneObject
        {

            Coroutine().StartCoroutine();
            IEnumerator Coroutine()
            {

                if (obj == null)
                    yield break;

                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrWhiteSpace(path))
                    yield break;

                AssetDatabase.DeleteAsset(path);

                AssetRef.instance.Remove(obj);
                if (obj is SceneCollection c)
                {
                    foreach (var profile in Profile.FindAll())
                        if (profile.collections.Contains(c))
                        {
                            profile.Remove(c);
                            profile.OnPropertyChanged();
                        }
                }

                yield return null;
                AssetDatabase.Refresh();
                AssetsChanged?.Invoke();

            }

        }

        /// <summary>Remove all null refs from <see cref="collections"/> and <see cref="scenes"/>.
        /// <para>Returns true if any assets were removed.</para></summary>
        internal bool RemoveNull(Profile profile = null)
        {

            if (!profile)
                profile = Profile.current;
            if (!profile)
                return false;

            profile.m_collections.RemoveAll(c => !c);
            var list = profile.m_standalone?.Where(s => !string.IsNullOrEmpty(s))?.ToArray();
            if (list?.SequenceEqual(profile.m_standalone) ?? true)
            {
                profile.m_standalone = list;
                AssetRef.instance.Remove(collection: null);
                return true;
            }

            return false;

        }

        /// <summary>Adds the <see cref="SceneAsset"/> to asm. Returns existing <see cref="Scene"/> if already exist.</summary>
        /// <remarks>Returns <see langword="null"/> if scene has been added to <see cref="Ignore(string)"/>.</remarks>
        public Scene Add(SceneAsset asset)
        {

            var path = AssetDatabase.GetAssetPath(asset);
            if (ignore.Contains(path))
                return null;

            var scene = FindSceneByPath(path);
            if (scene)
                return scene;

            var id = AssetDatabase.AssetPathToGUID(path);
            scene = Create<Scene>(Path.GetFileNameWithoutExtension(path), s => s.UpdateAsset(id, path));

            return scene;

        }

        public T Create<T>(string name, Action<T> initializeBeforeSave = null) where T : ScriptableObject, ISceneObject =>
            Create<T>(name, profile: null, initializeBeforeSave);

        /// <summary>Create and add an asset.</summary>
        public T Create<T>(string name, Profile profile = null, Action<T> initializeBeforeSave = null) where T : ScriptableObject, ISceneObject
        {

            if (!profile)
                profile = Profile.current;
            if (!profile && typeof(T) == typeof(SceneCollection))
                return null;

            var obj = ScriptableObject.CreateInstance<T>();
            if (obj is SceneCollection collection)
            {
                collection.SetTitle(name);
                collection.SetName(profile.prefix + name);
            }
            else
                obj.name = name;

            initializeBeforeSave?.Invoke(obj);
            Add(obj, profile);

            return obj;

        }

        /// <summary>Clear assets.</summary>
        public void Clear()
        {

            foreach (var asset in collections.Cast<ScriptableObject>().Concat(scenes))
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(asset));

            foreach (var profile in Profile.FindAll())
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(profile));

            ArrayUtility.Clear(ref AssetRef.instance.profiles);
            ArrayUtility.Clear(ref AssetRef.instance.collections);
            ArrayUtility.Clear(ref AssetRef.instance.scenes);

            AssetDatabase.Refresh();
            AssetsCleared?.Invoke();

        }

#endif
        #endregion

    }

}
