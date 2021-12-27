using AdvancedSceneManager.Editor.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    public static class VisualElementExtensions
    {

        private static Color? defaultBackgroundColor;
        public static Color DefaultBackgroundColor =>
            defaultBackgroundColor ?? (defaultBackgroundColor = EditorGUIUtilityExt.GetDefaultBackgroundColor()) ?? Color.clear;

        static readonly List<string> lockedObjects = new List<string>();

        internal static void SetLocked(string path, bool locked)
        {
            if (locked && !lockedObjects.Contains(path))
                lockedObjects.Add(path);
            else
                lockedObjects.Remove(path);
        }

        internal static void SetLocked(this VisualElement element, string path)
        {
            element.Query().Class("lockable").ForEach(e =>
            {
                if (e is ObjectField of)
                    of.isReadOnly = lockedObjects.Contains(path);
                else
                    e.SetEnabled(!lockedObjects.Contains(path));
            });
        }

        internal static TElement SetValueWithoutNotifyExt<TElement, TValue>(this TElement element, TValue value) where TElement : INotifyValueChanged<TValue>
        {
            element.SetValueWithoutNotify(value);
            return element;
        }

        public static TElement SetEnabledExt<TElement>(this TElement element, bool enabled) where TElement : VisualElement
        {
            element.SetEnabled(enabled);
            return element;
        }

        public static EnumField SetValueWithoutNotifyExt(this EnumField element, Enum value)
        {
            element.Init(value);
            return element;
        }

        public static SceneField Setup<T>(this SceneField element, string label, T target, string field, Action onChanged = null, bool saveInSceneManagerWindow = true, string tooltip = null)
        {

            if (element == null)
                return element;

            element.label = label;
            return Setup(element, target, field, onChanged, saveInSceneManagerWindow, tooltip);

        }

        /// <summary>Sets up an ui toolkit element, by setting current value and registers value changed callback and automatically sets the new value to target.</summary>
        public static TElement Setup<TElement, T>(this TElement element, T target, string field, Action onChanged = null, bool saveInSceneManagerWindow = true, string tooltip = null) where TElement : VisualElement
        {
            var t = typeof(TElement).GetInterfaces().FirstOrDefault(t1 => t1.Name.StartsWith("INotifyValueChanged"))?.GenericTypeArguments?.FirstOrDefault();
            if (t == null)
                return element;

            if (!string.IsNullOrWhiteSpace(tooltip))
                element.tooltip = tooltip;

            var method = typeof(VisualElementExtensions).GetMethod(nameof(SetupInternal), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(typeof(TElement), t, typeof(T));
            method?.Invoke(null, new object[] { element, target, field, onChanged, saveInSceneManagerWindow });
            return element;
        }

        public static Toggle Setup(this Toggle element, EventCallback<ChangeEvent<bool>> valueChanged, bool defaultValue = false, string tooltip = null)
        {

            if (element == null)
                return null;

            if (!string.IsNullOrWhiteSpace(tooltip))
                element.tooltip = tooltip;

            element.RegisterValueChangedCallback(valueChanged);
            element.SetValueWithoutNotify(defaultValue);
            return element;

        }

        public static Button Setup(this Button element, Action onClick)
        {

            if (element == null)
                return null;

            element.clicked += onClick;
            return element;

        }

        static void SetupInternal<TElement, TValue, T>(this TElement element, T target, string field, Action onChanged = null, bool saveInSceneManagerWindow = true) where TElement : INotifyValueChanged<TValue>
        {

            void Set(MemberInfo member, object v)
            {
                if (member is FieldInfo f && !f.IsInitOnly) f.SetValue(target, v);
                if (member is PropertyInfo p && p.SetMethod != null) p.SetValue(target, v);
            }

            object Get(MemberInfo member)
            {
                if (member is FieldInfo f)
                    return f.GetValue(target);
                if (member is PropertyInfo p)
                    return p.GetValue(target);
                return default;
            }

            var fieldInfo = (MemberInfo)target?.GetType()?.GetField(field) ?? target?.GetType()?.GetProperty(field);
            if (fieldInfo == null)
                return;

            var value = (TValue)Get(fieldInfo);
            if (element is EnumField enumField && value is Enum enumValue)
                enumField.Init(enumValue);
            else
                element?.SetValueWithoutNotify(value);

            element?.RegisterValueChangedCallback(e =>
            {
                Set(fieldInfo, e.newValue);
                if (saveInSceneManagerWindow && target is ScriptableObject so)
                    SceneManagerWindow.Save(so);
                onChanged?.Invoke();
            });

        }

        public static VisualElement FindAncestor(this VisualElement element, string name = "", string className = "", Action<VisualElement> actionToPerformOnEachParent = null)
        {

            if (element == null)
                return null;

            actionToPerformOnEachParent?.Invoke(element);

            if (!string.IsNullOrWhiteSpace(name) && element.name == name)
                return element;

            if (!string.IsNullOrWhiteSpace(className) && element.ClassListContains(className))
                return element;

            return FindAncestor(element.parent, name, className, actionToPerformOnEachParent);

        }

        public static VisualElement GetRoot(this VisualElement element)
        {

            if (element.parent == null)
                return element;

            return GetRoot(element.parent);

        }

        internal static StyleSheet[] GetStyles(this VisualElement element)
        {

            var l = new List<StyleSheet>();

            element.FindAncestor(actionToPerformOnEachParent: e =>
            {
                for (int i = 0; i < e.styleSheets.count; i++)
                    l.Add(e.styleSheets[i]);
            });

            return l.ToArray();

        }

    }

}
