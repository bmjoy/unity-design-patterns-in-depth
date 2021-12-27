#pragma warning disable IDE0063 // Use simple 'using' statement
#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.IO;
using UnityEngine;
using AdvancedSceneManager.Models;

using scene = UnityEngine.SceneManagement.Scene;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>A utility for storing scene related data. Data can only be saved to disk in editor.</summary>
    public static class SceneDataUtility
    {

        #region Update when scene is moved

#if UNITY_EDITOR

        class PostProcessor : AssetPostprocessor
        {

            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
            {

                foreach (var (source, to) in movedAssets.Select((item, i) => (source: movedFromPath[i], to: item)).ToArray())
                {
                    var path = AssetPath(source);
                    var toPath = AssetPath(to);
                    if (File.Exists(path))
                    {
                        if (File.Exists(AssetPath(to)))
                            File.Delete(AssetPath(to));
                        File.Move(path, AssetPath(to));
                        AssetDatabase.ImportAsset(toPath);
                        if (File.Exists(path + ".meta"))
                        {
                            if (File.Exists(toPath + ".meta"))
                                File.Delete(toPath + ".meta");
                            File.Move(path + ".meta", toPath + ".meta");
                        }
                    }
                }

                AssetDatabase.Refresh();

            }

        }

#endif

        #endregion

        [Serializable]
        internal class SceneData
        {

            [NonSerialized] public string path;
            [SerializeField] public SerializableDictionary data = new SerializableDictionary();

            [Serializable]
            public class SerializableDictionary : SerializableDictionary<string, string>
            { }

        }

        /// <summary>Enumerates all <typeparamref name="T"/> on all scenes.</summary>
        public static IEnumerable<T> Enumerate<T>(string key)
        {
            var resources = Resources.LoadAll<TextAsset>(ResourcesPath());
            foreach (var resource in resources)
            {
                var data = GetDirect(resource);
                if (data.data.ContainsKey(key) && Get<T>(data, key, out var value))
                    yield return value;
            }
        }

        /// <summary>Gets the value with the specified key, for the specified scene.</summary>
        public static T Get<T>(Scene scene, string key, T defaultValue = default) =>
            Get(scene ? scene.path : "", key, defaultValue);

        /// <summary>Gets the value with the specified key, for the specified scene. This is the direct version, all values are stores as string, which means <see cref="Get{T}(string, string, T)"/> must convert value beforehand, this method doesn't.</summary>
        public static string GetDirect(Scene scene, string key) =>
            GetDirect(scene ? scene.path : "", key);

        /// <summary>Sets the value with the specified key, for the specified scene.</summary>
        public static void Set<T>(Scene scene, string key, T value) =>
            Set(scene ? scene.path : "", key, value);

        /// <summary>Sets the value with the specified key, for the specified scene. This is the direct version, all values are stores as string, which means <see cref="Get{T}(string, string, T)"/> must convert value beforehand, this method doesn't.</summary>
        public static void SetDirect(Scene scene, string key, string value) =>
            SetDirect(scene ? scene.path : "", key, value);

        /// <summary>Unsets the value with the specified key, for the specified scene.</summary>
        public static void Unset(Scene scene, string key) =>
            Unset(scene ? scene.path : "", key);

        /// <summary>Gets the value with the specified key, for the specified scene.</summary>
        public static T Get<T>(string scene, string key, T defaultValue = default)
        {
            var data = Get(scene);
            return Get<T>(data, key, out var value) ? value : defaultValue;
        }

        static bool Get<T>(SceneData data, string key, out T value)
        {

            if (data.data.ContainsKey(key))
                if (Type.GetTypeCode(typeof(T)) != TypeCode.Object)
                    return TryConvert<T>(data.data[key], out value);
                else
                    return TryDeserialize<T>(data.data[key], out value);

            value = default;
            return false;

        }

        static bool TryConvert<T>(object obj, out T value)
        {
            try
            {
                value = (T)Convert.ChangeType(obj, typeof(T));
                return true;
            }
            catch (Exception)
            { }
            value = default;
            return false;
        }

        static bool TryDeserialize<T>(string json, out T value)
        {
            try
            {
                value = JsonUtility.FromJson<T>(json);
                return true;
            }
            catch (Exception)
            { }
            value = default;
            return false;
        }

        /// <summary>Gets the value with the specified key, for the specified scene. This is the direct version, all values are stores as string, which means <see cref="Get{T}(string, string, T)"/> must convert value beforehand, this method doesn't.</summary>
        public static string GetDirect(string scene, string key) =>
            Get(scene).data.GetValue(key);

        /// <summary>Sets the value with the specified key, for the specified scene.</summary>
        public static void Set<T>(string scene, string key, T value)
        {
            if (Convert.GetTypeCode(value) != TypeCode.Object)
                SetDirect(scene, key, Convert.ToString(value));
            else
                SetDirect(scene, key, JsonUtility.ToJson(value));
        }

        /// <summary>Sets the value with the specified key, for the specified scene. This is the direct version, all values are stores as string, which means <see cref="Get{T}(string, string, T)"/> must convert value beforehand, this method doesn't.</summary>
        public static void SetDirect(string scene, string key, string value)
        {
            var data = Get(scene);
            data.data.Set(key, value);
            Save(data);
        }

        /// <summary>Unsets the value with the specified key, for the specified scene.</summary>
        public static void Unset(string scene, string key)
        {
            var data = Get(scene);
            data.data.Remove(key);
            Save(data);
        }

        static SceneData Get(string path)
        {
            var resource = Resources.Load<TextAsset>(ResourcesPath(path));
            var data = GetDirect(resource);
            data.path = path;
            return data;
        }

        static SceneData GetDirect(TextAsset resource) =>
            GetDirect(resource ? resource.text : "");

        static SceneData GetDirect(string json) =>
            JsonUtility.FromJson<SceneData>(json) ?? new SceneData();

        static void Save(SceneData data)
        {

#if UNITY_EDITOR

            if (Application.isPlaying)
                return;

            var path = AssetPath(data.path);
            var json = JsonUtility.ToJson(data);
            if (!File.Exists(path) || File.ReadAllText(path) != json)
            {
                File.WriteAllText(path, json);
                AssetDatabase.Refresh();
            }

#endif

        }

        static string GetShortName(string scenePath)
        {

            if (string.IsNullOrWhiteSpace(scenePath))
                return "";

            using (var md5 = MD5.Create())
            {
                var i = BitConverter.ToInt32(md5.ComputeHash(Encoding.Unicode.GetBytes(scenePath)), 0);
                return i.ToString().TrimStart('-');
            }

        }

        static string ResourcesPath(string scenePath) =>
            ResourcesPath() + "/" + GetShortName(scenePath);

        static string ResourcesPath() =>
            "AdvancedSceneManager/SceneData";

#if UNITY_EDITOR

        static string AssetPath(string scenePath)
        {
            var folder = "Assets/Settings/Resources/AdvancedSceneManager/SceneData";
            return Directory.CreateDirectory(folder).FullName + "/" + GetShortName(scenePath) + ".json";
        }

#endif
    }

}
