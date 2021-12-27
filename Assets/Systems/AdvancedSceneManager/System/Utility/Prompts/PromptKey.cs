#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS1998  // Async Method lacks await

#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    public class PromptKey : GenericPrompt<(EventModifiers modifiers, KeyCode key), PromptKey>
    {

        public override string title => "Pick a key...";

        readonly KeyCode[] ignoreKeys =
        {
            KeyCode.LeftAlt, KeyCode.RightAlt,
            KeyCode.LeftCommand, KeyCode.RightCommand,
            KeyCode.LeftControl, KeyCode.RightControl,
            KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.CapsLock,
            KeyCode.Numlock,
            KeyCode.ScrollLock,
        };

        const EventModifiers ignoreModifiers =
            EventModifiers.FunctionKey |
            EventModifiers.Numeric |
            EventModifiers.CapsLock;

        public override void OnContentGUI(ref (EventModifiers modifiers, KeyCode key) value)
        {

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.None)
            {
                if (!ignoreKeys.Contains(Event.current.keyCode))
                    value.key = Event.current.keyCode;
                value.modifiers = Event.current.modifiers & ~ignoreModifiers;
                Repaint();
            }

            var modifiers =
                value.modifiers != EventModifiers.None
                ? value.modifiers.ToString() + " + "
                : "";

            var key = value.key.ToString();

            EditorGUILayout.LabelField("Press key:");
            EditorGUILayout.LabelField(modifiers + key);

        }

    }

}
#endif
