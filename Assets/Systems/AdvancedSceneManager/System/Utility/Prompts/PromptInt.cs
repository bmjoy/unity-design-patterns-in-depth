#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    public class PromptInt : GenericPrompt<string, PromptInt>
    {

        public override string title => "Enter a version...";

        protected override (bool isValid, string message) Validate(string value) =>
            (int.TryParse(value, out _), "Must be an int.");

        public override void OnContentGUI(ref string value)
        {

            EditorGUILayout.LabelField("Version:");
            GUI.SetNextControlName("text");
            value = EditorGUILayout.TextField(value);

            EditorGUI.FocusTextInControl("text");

        }

    }

}

#endif
