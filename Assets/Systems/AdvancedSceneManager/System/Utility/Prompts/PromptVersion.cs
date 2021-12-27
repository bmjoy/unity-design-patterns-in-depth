#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    public class PromptVersion : GenericPrompt<string, PromptVersion>
    {

        public override string title => "Enter a version...";

        protected override (bool isValid, string message) Validate(string value) =>
            Version.TryParse(value, out var _)
            ? (true, "")
            : (false, "Invalid version");

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
