#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    public class PromptNameAndMessage : GenericPrompt<(string name, string message), PromptNameAndMessage>
    {

        public override string title => "Pick a name and message...";
        public override float extraHeight => 82;

        public override void OnContentGUI(ref (string name, string message) value)
        {

            EditorGUILayout.LabelField("Name:");
            GUI.SetNextControlName("text");
            value.name = EditorGUILayout.TextField(value.name);

            EditorGUILayout.LabelField("Message:");
            value.message = EditorGUILayout.TextArea(value.message, GUILayout.Height(64));

        }

    }

}

#endif
