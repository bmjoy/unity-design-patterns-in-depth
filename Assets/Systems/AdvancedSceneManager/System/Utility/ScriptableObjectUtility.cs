using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Utility
{

    public static class ScriptableObjectUtility
    {

        /// <summary>
        /// <para>Saves the <see cref="ScriptableObject"/>.</para>
        /// <para>Safe to call from outside editor, but has no effect.</para>
        /// </summary>
        public static void Save(this ScriptableObject obj)
        {

#if UNITY_EDITOR

            if (!obj)
                return;

            EditorUtility.SetDirty(obj);

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrWhiteSpace(path))
                return;
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();

#endif

        }

        #region Singleton

        static readonly Dictionary<Type, ScriptableObject> m_current = new Dictionary<Type, ScriptableObject>();
        public static T GetSingleton<T>(string assetPath, string resourcesPath) where T : ScriptableObject
        {
            if (m_current.TryGetValue(typeof(T), out var value) && value)
                return (T)value;
            return FindAsset<T>(assetPath, resourcesPath);
        }

        static T LoadFromResources<T>(string resourcesPath) where T : ScriptableObject =>
            Resources.Load<T>(resourcesPath);

        static T FindAsset<T>(string assetPath, string resourcesPath) where T : ScriptableObject
        {

            if (LoadFromResources<T>(resourcesPath) is T value && value)
                return (T)m_current.Set(typeof(T), value);
            else
            {

#if UNITY_EDITOR

                var o = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (o)
                    return o;

                EditorFolderUtility.EnsureFolderExists(Path.GetDirectoryName(assetPath));

                var obj = ScriptableObject.CreateInstance<T>();
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.Refresh();
                AssetDatabase.CreateAsset(obj, assetPath);
                AssetDatabase.Refresh();

                return (T)m_current.Set(typeof(T), AssetDatabase.LoadAssetAtPath<T>(assetPath));

#else
                var so = ScriptableObject.CreateInstance<T>();
                m_current.Set(typeof(T), so);
                return so;
#endif

            }

        }

        #endregion

    }

}
