#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    public class PromptName : GenericPrompt<string, PromptName>
    {

        public override string title => "Pick a name...";

        public override void OnContentGUI(ref string value)
        {

            EditorGUILayout.LabelField("Name:");
            GUI.SetNextControlName("text");
            value = EditorGUILayout.TextField(value);

            EditorGUI.FocusTextInControl("text");

        }

    }

}

#endif
