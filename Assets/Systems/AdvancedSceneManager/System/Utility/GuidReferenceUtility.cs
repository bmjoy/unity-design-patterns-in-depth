using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>An utility for referencing objects globally.</summary>
    public class GuidReferenceUtility
    {

        static readonly Hashtable references = new Hashtable();

        /// <summary>Adds a reference to the object, returns the id that will be used to find it again.</summary>
        public static string AddRuntime(Object obj)
        {

            if (references.ContainsValue(obj))
                return references.Keys.OfType<string>().FirstOrDefault(k => (Object)references[k] == obj);

            var id = Guid.NewGuid().ToString();
            references.Add(id, obj);
            return id;

        }

        /// <summary>Removes the reference to this object.</summary>
        public static void RemoveRuntime(Object obj)
        {
            if (references.ContainsValue(obj))
                RemoveRuntime(references.Keys.OfType<string>().FirstOrDefault(k => (Object)references[k] == obj));
        }

        /// <summary>Removes the reference to the object with this id.</summary>
        public static void RemoveRuntime(string id)
        {
            if (references.ContainsKey(id))
                references.Remove(id);
        }

        internal static void Add(GuidReference reference)
        {
            if (reference && !HasReference(reference.guid))
                references.Add(reference.guid, reference.gameObject);
        }

        internal static void Remove(GuidReference reference)
        {
            if (reference)
                references.Remove(reference.guid);
        }

        /// <summary>Gets if reference exists.</summary>
        public static bool HasReference(string id) =>
            references.ContainsKey(id) && (Object)references[id];

        /// <summary>Gets if reference exists.</summary>
        public static bool TryFind<T>(string id, out T obj) where T : Object
        {
            obj = TryFind(id, out var o) && o is T t ? t : default;
            return obj;
        }

        /// <summary>Tries to find the reference.</summary>
        public static Object TryFind(string id, out Object obj)
        {
            obj = Find(id);
            return obj;
        }

        /// <summary>Finds a reference if it exists.</summary>
        public static Object Find(string id) =>
            references.ContainsKey(id)
            ? (Object)references[id]
            : null;

        /// <summary>Finds a reference if it exists.</summary>
        public static T Find<T>(string id) where T : Object =>
            references.ContainsKey(id)
            ? references[id] as T
            : null;

        /// <summary>Finds a reference if it exists.</summary>
        public static IEnumerator Find(string id, Action<Object> callback)
        {
            while (!HasReference(id))
                yield return null;
            callback?.Invoke(Find(id));
        }

#if UNITY_EDITOR

        /// <summary>
        /// <para>Adds a persistent reference to this <see cref="GameObject"/>.</para>
        /// <para>Only usable in editor.</para>
        /// </summary>
        public static string AddPersistent(GameObject obj)
        {

            if (obj.TryGetComponent<GuidReference>(out var guidReference))
                return guidReference.guid;

            guidReference = obj.AddComponent<GuidReference>();

            EditorSceneManager.MarkSceneDirty(obj.scene);
            EditorSceneManager.SaveScene(obj.scene);

            return guidReference.guid;

        }

        /// <summary>
        /// <para>Removes a persistent reference to this <see cref="GameObject"/>.</para>
        /// <para>Only usable in editor.</para>
        /// </summary>
        public static void RemovePersistent(GameObject obj, bool saveScene)
        {

            if (!obj.TryGetComponent<GuidReference>(out var guidReference))
                return;

            Object.DestroyImmediate(guidReference);

            EditorSceneManager.MarkSceneDirty(obj.scene);
            if (saveScene)
                EditorSceneManager.SaveScene(obj.scene);

        }

#endif

        /// <summary>Finds the persistent reference in the currently open scenes.</summary>
        /// <param name="forceHierarchyScan">Outside of playmode, the hierarchy will be scanned since registration with Start() is unreliable, setting this parameter to true will force this during even during playmode. Note that this is slow though.</param>
        public static GameObject FindPersistent(string guid, bool forceHierarchyScan = false)
        {

            if (Application.isPlaying && !forceHierarchyScan)
                return Find<GameObject>(guid);
            else
                foreach (var scene in SceneUtility.GetAllOpenUnityScenes().Where(s => s.isLoaded).ToArray())
                    foreach (var rootObj in scene.GetRootGameObjects())
                        foreach (var obj in rootObj.GetComponentsInChildren<GuidReference>(includeInactive: true))
                            if (obj.guid == guid)
                                return obj.gameObject;

            return null;

        }

        /// <summary>Finds the persistent reference in the currently open scenes.</summary>
        /// <param name="forceHierarchyScan">Outside of playmode, the hierarchy will be scanned since registration with Start() is unreliable, setting this parameter to true will force this during even during playmode. Note that this is slow though.</param>
        public static bool TryFindPersistent(string guid, out GameObject obj, bool forceHierarchyScan = false)
        {
            obj = FindPersistent(guid, forceHierarchyScan);
            return obj;
        }

    }

}
