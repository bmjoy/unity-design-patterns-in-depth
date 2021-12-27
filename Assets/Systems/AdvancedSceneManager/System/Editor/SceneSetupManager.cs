using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    /// <summary>
    /// <para>An class to save and restore scene setup on editor close and open.</para>
    /// <para>Use <see cref="saveAndRestoreSceneSetupOnEditorCloseAndOpen"/> or through Scene Manager window to enable or disable.</para>
    /// </summary>
    [InitializeOnLoad]
    static class SceneSetupManager
    {

        /// <summary>When <see langword="true"/>: saves the open scenes when unity closes, and restores them when next time unity is opened.</summary>
        public static bool saveAndRestoreSceneSetupOnEditorCloseAndOpen
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.SaveAndRestoreSceneSetupOnEditorCloseAndOpen", true);
            set => EditorPrefs.SetBool("AdvancedSceneManager.SaveAndRestoreSceneSetupOnEditorCloseAndOpen", value);
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

            EditorApplication.quitting -= Restore;
            EditorApplication.quitting += Restore;
            Save();

            SettingsTab.Settings.Add(header: SettingsTab.Settings.DefaultHeaders.Options_Local, callback: () =>
            {

                var element = new Toggle("Save scene setup when closing unity:");
                element.tooltip = "This saves the open scenes when unity closes, and restores them when next time unity is opened.";
                element.SetValueWithoutNotify(saveAndRestoreSceneSetupOnEditorCloseAndOpen);
                _ = element.RegisterValueChangedCallback(e => saveAndRestoreSceneSetupOnEditorCloseAndOpen = e.newValue);
                return element;

            });

        }

        static void Save()
        {

            if (Application.isPlaying)
                return;

            if (!saveAndRestoreSceneSetupOnEditorCloseAndOpen)
                return;

            var json = JsonUtility.ToJson(new SceneManagerSetup(EditorSceneManager.GetSceneManagerSetup()));
            EditorPrefs.SetString("AdvancedSceneManager.SceneSetup", json);

        }

        class SceneManagerSetup
        {

            public SceneManagerSetup(params SceneSetup[] scenes) =>
                this.scenes = scenes;

            public SceneSetup[] scenes;

        }

        static void Restore()
        {

            if (!saveAndRestoreSceneSetupOnEditorCloseAndOpen)
                return;

            var json = EditorPrefs.GetString("AdvancedSceneManager.SceneSetup", "");
            var setup = JsonUtility.FromJson<SceneManagerSetup>(json);

            if (setup.scenes?.Any() ?? false)
                EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);

        }

    }

}
