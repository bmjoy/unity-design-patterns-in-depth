#if UNITY_EDITOR

using System.Linq;
using AdvancedSceneManager.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{


    /// <summary>
    /// <para>An utility class to automatically open persistent scenes in editor.</para>
    /// <para>Not safe to use in build.</para>
    /// </summary>
    public static class PersistentSceneInEditorUtility
    {

        public static OpenInEditorSetting GetPersistentOption(Scene scene)
        {
            var json = EditorPrefs.GetString("AdvancedSceneManager.OpenInEditorSetting+" + scene.name);
            if (string.IsNullOrWhiteSpace(json))
                return new OpenInEditorSetting() { scene = scene.path, name = scene.name };
            else
                return JsonUtility.FromJson<OpenInEditorSetting>(json);
        }

        public static void OpenAssociatedPersistentScenes(Scene scene, bool promptSave = false)
        {

            if (!scene)
                return;

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scenes = GetAssociatedScenes(scene);
            foreach (var persistentScene in scenes)
                SceneManager.editor.Open(persistentScene, promptSave: false);

        }

        public static Scene[] GetAssociatedScenes(Scene scene) =>
            SceneManager.assetManagement.scenes.Where(s =>
            {

                var option = GetPersistentOption(s);

                if (option.option == OpenInEditorOption.AnySceneOpens)
                    return true;
                else if (option.option == OpenInEditorOption.WhenAnyOfTheFollowingScenesOpen)
                    return option.list.Contains(scene.path);
                else if (option.option == OpenInEditorOption.WhenAnySceneOpensExcept)
                    return !option.list.Contains(scene.path);

                return false;

            }).ToArray();

        public enum OpenInEditorOption
        {
            Never, AnySceneOpens, WhenAnyOfTheFollowingScenesOpen, WhenAnySceneOpensExcept
        }

        public sealed class OpenInEditorSetting
        {

            public void Save()
            {
                var json = JsonUtility.ToJson(this);
                EditorPrefs.SetString("AdvancedSceneManager.OpenInEditorSetting+" + name, json);
            }

            public string name;
            public string scene;
            public OpenInEditorOption option;
            public string[] list;

        }

    }

}
#endif
