#pragma warning disable IDE0029 // Use coalesce expression
#pragma warning disable IDE0051 // Remove unused private members

using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    public static class StyleExtensions
    {

        public static void SetBorderWidth(this IStyle style, float? all = null, float? vertical = null, float? horizontal = null, float? left = null, float? right = null, float? top = null, float? bottom = null)
        {

            if (!left.HasValue) left = horizontal ?? all;
            if (!right.HasValue) right = horizontal ?? all;
            if (!top.HasValue) top = vertical ?? all;
            if (!bottom.HasValue) bottom = vertical ?? all;

            if (right.HasValue) style.borderRightWidth = right.Value;
            if (bottom.HasValue) style.borderBottomWidth = bottom.Value;
            if (left.HasValue) style.borderLeftWidth = left.Value;
            if (top.HasValue) style.borderTopWidth = top.Value;

        }

        public static void SetBorderColor(this IStyle style, Color? all = null, Color? vertical = null, Color? horizontal = null, Color? left = null, Color? right = null, Color? top = null, Color? bottom = null)
        {

            if (!left.HasValue) left = horizontal ?? all;
            if (!right.HasValue) right = horizontal ?? all;
            if (!top.HasValue) top = vertical ?? all;
            if (!bottom.HasValue) bottom = vertical ?? all;

            if (right.HasValue) style.borderRightColor = right.Value;
            if (bottom.HasValue) style.borderBottomColor = bottom.Value;
            if (left.HasValue) style.borderLeftColor = left.Value;
            if (top.HasValue) style.borderTopColor = top.Value;

        }

        public static void SetMargin(this IStyle style, float? all = null, float? vertical = null, float? horizontal = null, float? left = null, float? right = null, float? top = null, float? bottom = null)
        {

            if (!left.HasValue) left = horizontal ?? all;
            if (!right.HasValue) right = horizontal ?? all;
            if (!top.HasValue) top = vertical ?? all;
            if (!bottom.HasValue) bottom = vertical ?? all;

            if (right.HasValue) style.marginRight = right.Value;
            if (bottom.HasValue) style.marginBottom = bottom.Value;
            if (left.HasValue) style.marginLeft = left.Value;
            if (top.HasValue) style.marginTop = top.Value;

        }

        public static void SetPadding(this IStyle style, float? all = null, float? vertical = null, float? horizontal = null, float? left = null, float? right = null, float? top = null, float? bottom = null)
        {

            if (!left.HasValue) left = horizontal ?? all;
            if (!right.HasValue) right = horizontal ?? all;
            if (!top.HasValue) top = vertical ?? all;
            if (!bottom.HasValue) bottom = vertical ?? all;

            if (right.HasValue) style.paddingRight = right.Value;
            if (bottom.HasValue) style.paddingBottom = bottom.Value;
            if (left.HasValue) style.paddingLeft = left.Value;
            if (top.HasValue) style.paddingTop = top.Value;

        }

    }

}
