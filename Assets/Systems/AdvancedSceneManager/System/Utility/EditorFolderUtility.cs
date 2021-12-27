#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Contains functions for folders in editor.</summary>
    public static class EditorFolderUtility
    {

        public static void EnsureFolderExists(string folder)
        {

            folder = folder.Replace(@"\", "/");

            if (folder.StartsWith(Application.dataPath))
                folder = folder.Replace(Application.dataPath, "");

            if (string.IsNullOrEmpty(folder))
                return;

            if (!folder.StartsWith("Assets/"))
                folder = "Assets/" + folder;

            folder = folder.Replace("//", "/");

            var segments = folder.Split('/');
            var path = segments.FirstOrDefault();
            bool isFirst = true;
            foreach (var f in segments)
            {
                if (f != "Assets" && !AssetDatabase.IsValidFolder(path + "/" + f))
                    AssetDatabase.CreateFolder(path, f);
                if (!isFirst)
                    path += "/" + f;
                isFirst = false;
            }

        }

    }

}
#endif
