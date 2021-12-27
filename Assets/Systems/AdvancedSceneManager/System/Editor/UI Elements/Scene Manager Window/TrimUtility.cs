#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

using static UnityEngine.UIElements.VisualElement;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Applies text-overflow uss property in 2020+, but applies manual trim in earlier versions.</summary>
    public static class TrimUtility
    {

        /// <summary>Trim the label and show ellipsis if too long.</summary>
        public static void TrimLabel(this TextElement label, string text, Func<float> maxWidth, bool enableAuto)
        {

            if (label == null)
                return;

            //Check and set text-overflow property (Unity 2020+ only) if available
            if (SetTextOverflowIfAvailable(label, enableAuto))
                return;

            TrimLabel(label, text, maxWidth.Invoke());
            label.parent.UnregisterCallback<GeometryChangedEvent>(DoTrim);

            if (enableAuto)
                label.parent.RegisterCallback<GeometryChangedEvent>(DoTrim);

            void DoTrim(GeometryChangedEvent e)
            {
                if (maxWidth?.Invoke() is float f && !float.IsNaN(f))
                    TrimLabel(label, text, f);
            }

        }

        #region Text Overflow property (Unity 2020+)

        /// <summary>
        /// Sets text overflow, if available.
        /// <para>Returns true if property was successfully applied.</para>
        /// </summary>
        static bool SetTextOverflowIfAvailable(TextElement label, bool enable)
        {

            try
            {

                var type = Type.GetType("UnityEngine.UIElements.TextOverflow, UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

                var value = Enum.Parse(type, "Ellipsis");
                var styleEnum = Activator.CreateInstance(typeof(StyleEnum<>).MakeGenericType(type), value);

                typeof(IStyle).GetProperty("textOverflow").SetValue(label.style, enable ? styleEnum : default);

                label.style.width = new StyleLength(new Length(92f, LengthUnit.Percent));

                return true;

            }
            catch (Exception)
            {
                return false;
            }

        }

        #endregion
        #region Manual text trim

        static readonly Dictionary<TextElement, Dictionary<char, float>> lengths = new Dictionary<TextElement, Dictionary<char, float>>();

        public static float GetLength(string str, TextElement label) =>
            str.ToCharArray().Sum(c => GetLength(c, label));

        public static float GetLength(char c, TextElement label)
        {

            if (!lengths.ContainsKey(label))
                lengths.Add(label, new Dictionary<char, float>());

            if (lengths[label].TryGetValue(c, out var value))
                return value;
            else
            {
                var length = label.MeasureTextSize(c.ToString(), 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
                if (!float.IsNaN(length.x))
                    lengths[label].Add(c, length.x);
                return length.x;
            }

        }

        const string Ellipsis = "...";

        static void TrimLabel(this TextElement label, string text, float maxWidth)
        {

            var fullText = text;
            while (GetLength(text, label) > maxWidth)
            {

                if (text == Ellipsis)
                {
                    text = fullText;
                    break;
                }

                if (text.EndsWith(Ellipsis))
                    text = text.Remove(text.Length - Ellipsis.Length - 1).TrimEnd(' ') + Ellipsis;
                else
                    text = text.Remove(text.Length - 1).TrimEnd(' ') + Ellipsis;

            }

            label.style.alignSelf = GetLength(text, label) > maxWidth ? Align.FlexStart : Align.Center;
            label.text = text;

        }

        #endregion

    }

}
