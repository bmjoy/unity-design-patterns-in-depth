#if UNITY_EDITOR

using AdvancedSceneManager.Utility;
using Lazy.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>An helper class for creating <see cref="MenuItem"/>.</summary>
    public static class MenuItemHelper
    {

        static readonly Dictionary<MethodBase, (Action onClick, Action onStart)> items = new Dictionary<MethodBase, (Action onClick, Action onStart)>();

        static (Action onClick, Action onStart) GetItem(MethodBase method) =>
            items.GetValue(method);

        static void Invoke(MethodBase method) =>
            GetItem(method).onClick?.Invoke();

        /// <summary>Invokes a menu item, only usable when called from a method decorated with <see cref="MenuItem"/> that has been <see cref="Setup()"/> using <see cref="MenuItemHelper"/>.</summary>
        public static void InvokeThisMenuItem()
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(1).GetMethod();
            if (method.GetCustomAttribute<MenuItem>() != null)
                Invoke(method);
        }

        /// <summary>Sets up an clickable menu item.</summary>
        public static void Setup(MethodBase method, Action onClick = null)
        {

            var attribute = method.GetCustomAttribute<MenuItem>();
            if (attribute == null)
                return;

            items.Set(method, (onClick, onStart: null));

        }

        /// <summary>Sets up an toggleable menu item.</summary>
        public static void Setup(MethodBase method, Func<bool> get, Action<bool> set = null)
        {

            var attribute = method.GetCustomAttribute<MenuItem>();
            if (attribute == null || get == null)
                return;

            items.Set(method, (onClick: OnClick, onStart: OnStart));

            void OnStart() =>
                Menu.SetChecked(attribute.menuItem, get.Invoke());

            void OnClick()
            {

                var newValue = !get.Invoke();
                set?.Invoke(newValue);
                OnStart();

            }

        }

        /// <summary>Sets up an toggleable menu item, using property as backing value.</summary>
        public static void Setup(MethodBase method, PropertyInfo property, Action<bool> onChecked = null, object propertyTarget = null)
        {

            if (property?.PropertyType != typeof(bool))
                return;

            Setup(method,
                get: () => (bool)property.GetValue(propertyTarget),
                set: (value) => { property.SetValue(propertyTarget, value); onChecked?.Invoke(value); });

        }

        [InitializeOnLoadMethod]
        public static void Refresh() =>
            CoroutineUtility.Run(after: 1f, action: () =>
            {
                foreach (var kvp in items)
                    kvp.Value.onStart?.Invoke();
            });

    }

}
#endif
