#if UNITY_EDITOR

using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

using UnityEditor;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Contains proxy functions for internal <see cref="EditorGUIUtility"/> functions that should have a public counterpart.</summary>
    public static class EditorGUIUtilityExt
    {
         
        public static void PingOrOpenAsset(Object targetObject, int clickCount)
        {
            if (clickCount == 1)
                EditorGUIUtility.PingObject(targetObject);
            else if (clickCount == 2)
            {
                AssetDatabase.OpenAsset(targetObject);
                Selection.activeObject = targetObject;
            }

        }

        public static Color GetDefaultBackgroundColor() => (Color)Invoke();

        static object Invoke([CallerMemberName] string name = "", params object[] parameters)
        {
           return typeof(EditorGUIUtility).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, parameters);
        }

    }

}
#endif
