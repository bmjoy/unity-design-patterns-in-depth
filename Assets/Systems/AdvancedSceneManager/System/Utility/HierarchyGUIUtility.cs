#if UNITY_EDITOR

using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>An utility for adding extra icons to scene fields in the heirarchy window. Only available in editor.</summary>
    public static class HierarchyGUIUtility
    {

        #region Callbacks

        /// <summary>Called after reserving a rect in hierarchy scene field. Return true to indicate that something was drawn, false means that the rect will be re-used for next OnGUI callback.</summary>
        public delegate bool HierarchySceneGUI(Rect position, scene scene);
        /// <summary>Called after reserving a rect in hierarchy game object field. Return true to indicate that something was drawn, false means that the rect will be re-used for next OnGUI callback.</summary>
        public delegate bool HierarchyGameObjectGUI(Rect position, GameObject gameObject);

        static readonly Dictionary<HierarchySceneGUI, (float? width, int? index)> onSceneGUI = new Dictionary<HierarchySceneGUI, (float? width, int? index)>();
        static readonly Dictionary<HierarchyGameObjectGUI, (float? width, int? index)> onGameObjectGUI = new Dictionary<HierarchyGameObjectGUI, (float? width, int? index)>();

        /// <summary>Adds a onGUI call for <see cref="Scene"/> fields.</summary>
        /// <param name="width">The width of the region to reserve. Null means width will be the same as height.</param>
        public static void AddSceneGUI(HierarchySceneGUI onGUI, float? width = null, int? index = 0)
        {
            _ = onSceneGUI.Set(onGUI, (width, index));
            Repaint();
        }

        /// <summary>Remove a onGUI call for a <see cref="Scene"/>.</summary>
        public static void RemoveSceneGUI(HierarchySceneGUI onGUI)
        {
            _ = onSceneGUI.Remove(onGUI);
            Repaint();
        }

        /// <summary>Adds a onGUI call for <see cref="GameObject"/> fields.</summary>
        /// <param name="width">The width of the region to reserve. Null means width will be the same as height.</param>
        public static void AddGameObjectGUI(HierarchyGameObjectGUI onGUI, float? width = null, int? index = null)
        {
            _ = onGameObjectGUI.Set(onGUI, (width, index));
            Repaint();
        }

        /// <summary>Remove a onGUI call for a <see cref="GameObject"/>.</summary>
        public static void RemoveGameObjectGUI(HierarchyGameObjectGUI onGUI)
        {
            _ = onGameObjectGUI.Remove(onGUI);
            Repaint();
        }

        #endregion

        [InitializeOnLoadMethod]
        static void OnLoad() =>
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;

        /// <inheritdoc cref="EditorApplication.RepaintHierarchyWindow"/>
        public static void Repaint() =>
            EditorApplication.RepaintHierarchyWindow();

        static readonly Dictionary<int, scene> scenes = new Dictionary<int, scene>();

        static void OnHierarchyGUI(int instanceID, Rect position)
        {

            if (EditorUtility.InstanceIDToObject(instanceID) is GameObject obj && obj)
            {
                var rect = new Rect(position.xMax, 0, 0, 0);
                foreach (var onGUI in onGameObjectGUI.OrderBy(onGUI => onGUI.Value.index).ToArray())
                {

                    var width = onGUI.Value.index ?? position.height;
                    var r = new Rect(position.xMax - width, position.y, width, position.height);
                    if (onGUI.Key?.Invoke(r, obj) ?? false)
                        rect = r;

                }
            }
            else
            {

                if (!scenes.TryGetValue(instanceID, out var scene))
                    scene = scenes.Set(instanceID, SceneUtility.GetAllOpenUnityScenes().FirstOrDefault(s => s.GetHashCode() == instanceID));

                var rect = new Rect(position.xMax, 0, 0, 0);
                foreach (var onGUI in onSceneGUI.OrderBy(onGUI => onGUI.Value.index).ToArray())
                {

                    var width = onGUI.Value.width ?? position.height;
                    var r = new Rect(rect.xMin - width, position.y, width, position.height);
                    if (onGUI.Key?.Invoke(r, scene) ?? false)
                        rect = r;

                }

            }


        }

    }

}
#endif
