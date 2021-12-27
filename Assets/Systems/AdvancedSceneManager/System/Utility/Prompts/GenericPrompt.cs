#pragma warning disable IDE0051 // Remove unused private members

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    public class GenericPrompt : GenericPrompt<object, GenericPrompt>
    {

        public static bool Prompt(string title, string message, string OkButton = "Ok", string cancelButton = "Cancel", float maxContentHeight = 400)
        {

            content = new GUIContent(message);
            _title = title;
            _okButton = OkButton;
            _cancelButton = cancelButton;
            _maxContentHeight = maxContentHeight;
            var value = Prompt(defaultValue: "_");
            return value.successful;

        }

        static GUIContent content;
        static Vector2 size;
        static string _title;
        static string _okButton;
        static string _cancelButton;
        static float _maxContentHeight;
        public override float extraHeight => size.y;
        public override float width => size.x + 56;

        public override string title => _title;
        public override string cancelButton => _cancelButton;
        public override string okButton => _okButton;

        Vector2 scrollPos;
        public override void OnContentGUI(ref object value)
        {
            size = GUI.skin.label.CalcSize(content);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(_maxContentHeight));
            EditorGUILayout.LabelField(content, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

    }

    /// <summary>Prompts the user for a name.</summary>
    public abstract class GenericPrompt<T, TSelf> : EditorWindow where TSelf : GenericPrompt<T, TSelf>
    {

        (T value, bool successful) result;
        protected readonly List<Func<T, (bool isValid, string message)>> validate = new List<Func<T, (bool isValid, string message)>>();

        static (bool isValid, string message) ValidateDefault(T value) =>
             (!(value is string s && string.IsNullOrWhiteSpace(s)) && value != null, "");

        protected virtual (bool isValid, string message) Validate(T value)
        {
            return (true, "");
        }

        public static (T value, bool successful) Prompt(T defaultValue = default, params Func<T, (bool isValid, string message)>[] validate)
        {

            var window = (GenericPrompt<T, TSelf>)CreateInstance(typeof(TSelf));
            window.titleContent = new GUIContent(window.title);
            window.UpdateSize();

            window.result = (defaultValue, false);
            window.validate.Add(ValidateDefault);
            window.validate.AddRange(validate);
            window.validate.Add((v) => window.Validate(v));

            window.ShowModalUtility();

            return window.result;

        }

        int validateMessages;
        void UpdateSize()
        {

            if (!updateSizeAutomatically)
                return;

            minSize = new Vector2(width, 120 + (12 * validateMessages) + extraHeight);
            maxSize = minSize;

            var x = !hasSetPosition ? (Screen.currentResolution.width - minSize.x) / 2 : pos.x;
            var y = !hasSetPosition ? (Screen.currentResolution.height - minSize.y) / 2 : pos.y;

            position = pos = new Rect(x, y, minSize.x, minSize.y);
            hasSetPosition = true;

        }

        Rect pos;
        bool hasSetPosition;

        public abstract void OnContentGUI(ref T value);
        public new abstract string title { get; }
        public virtual bool updateSizeAutomatically { get; } = true;
        public virtual float extraHeight { get; }
        public virtual float width { get; } = 250;
        public virtual string okButton { get; } = "Done";
        public virtual string cancelButton { get; } = "Cancel";

        void OnGUI()
        {

            if (position != default)
                pos = position;

            var isEnter = Event.current.isKey &&
                (Event.current.keyCode == KeyCode.Return) ||
                (Event.current.keyCode == KeyCode.KeypadEnter);

            EditorGUILayout.BeginVertical(new GUIStyle() { margin = new RectOffset(22, 22, 22, 22) });

            T value = result.value;
            OnContentGUI(ref value);
            result = (value, false);

            var validationErrors = validate.Select(v => v?.Invoke(value) ?? default).Where(v => !v.isValid).ToArray();

            validateMessages = 0;
            foreach (var (isValid, message) in validationErrors)
                if (!string.IsNullOrWhiteSpace(message))
                {
                    var style = new GUIStyle();
                    style.normal.textColor = Color.red;
                    GUILayout.Label(message, style);
                    validateMessages += 1;
                }

            UpdateSize();

            GUILayout.FlexibleSpace();
            EditorGUILayout.Space();

            DrawButtons(!validationErrors.Any(), isEnter);

            GUILayout.EndVertical();

        }

        void DrawButtons(bool doneEnabled, bool isEnter)
        {

            GUILayout.BeginHorizontal();

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                GUI.enabled = doneEnabled;
                DoneButton();
                CancelButton();
            }
            else
            {
                CancelButton();
                GUI.enabled = doneEnabled;
                DoneButton();
            }

            GUILayout.EndHorizontal();

            void CancelButton()
            {
                GUI.enabled = true;
                if (GUILayout.Button(cancelButton))
                {
                    result = (result.value, false);
                    Close();
                }
            }

            void DoneButton()
            {
                if (GUILayout.Button(okButton) || (isEnter && GUI.enabled))
                {
                    result = (result.value, true);
                    Close();
                }
            }

        }

    }

}
#endif
