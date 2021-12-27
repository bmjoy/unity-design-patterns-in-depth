#pragma warning disable CS0414

using System.Linq;
using UnityEngine;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Scene = AdvancedSceneManager.Models.Scene;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Core
{

    class AssetRef : ScriptableObject
    {

        public Profile[] profiles = Array.Empty<Profile>();
        public Scene[] scenes = Array.Empty<Scene>();
        public SceneCollection[] collections = Array.Empty<SceneCollection>();

        #region Add / Remove

#if UNITY_EDITOR

        public void Add(object obj)
        {
            if (obj is Profile p)
                Add(p);
            else if (obj is SceneCollection c)
                Add(c);
            else if (obj is Scene s)
                Add(s);
        }

        public void Remove(object obj)
        {
            if (obj is Profile p)
                Remove(p);
            else if (obj is SceneCollection c)
                Remove(c);
            else if (obj is Scene s)
                Remove(s);
        }

        public void Add(Profile profile) => Add(profile, ref profiles);
        public void Add(Scene scene) => Add(scene, ref scenes);
        public void Add(SceneCollection collection) => Add(collection, ref collections);

        public void Remove(Profile profile) => Remove(profile, ref profiles);
        public void Remove(Scene scene) => Remove(scene, ref scenes);
        public void Remove(SceneCollection collection) => Remove(collection, ref collections);

        void Add<T>(T obj, ref T[] list)
        {
            if (list == null) list = Array.Empty<T>();
            if (!list.Contains(obj))
            {
                ArrayUtility.Add(ref list, obj);
                CleanUp();
                EditorUtility.SetDirty(this);
            }
        }

        void Remove<T>(T obj, ref T[] list)
        {
            if (list?.Contains(obj) ?? false)
            {
                ArrayUtility.Remove(ref list, obj);
                CleanUp();
                EditorUtility.SetDirty(this);
            }
        }

        internal void CleanUp()
        {
            instance.profiles = instance.profiles.Where(p => p).Distinct().ToArray();
            instance.scenes = instance.scenes.Where(s => s).Distinct().ToArray();
            instance.collections = instance.collections.Where(c => c).Distinct().ToArray();
        }

#endif

        #endregion
        #region ScriptableObject

        const string assetPath = "Assets/Settings/Resources/AdvancedSceneManager/Assets.asset";
        const string resourcesPath = "AdvancedSceneManager/Assets";

        public static AssetRef instance => ScriptableObjectUtility.GetSingleton<AssetRef>(assetPath, resourcesPath);

        #endregion

    }

}
