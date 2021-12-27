#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Provides the ability to add settings to advanced scene manager window settings.</summary>
    /// <remarks>Only available in editor.</remarks>
    public class SettingsTab
    {

        public static SettingsTab instance { get; } = new SettingsTab();

        public class _DefaultHeaders
        {
            public string Appearance { get; } = "Appearance";
            public string Log { get; } = "Log";
            public string Options { get; } = "Options";
            public string Options_Project { get; } = "Options_Project";
            public string Options_Profile { get; } = "Options_Profile";
            public string Options_Local { get; } = "Options_Local";
            public string Experimental { get; } = "Experimental";
        }

        public _DefaultHeaders DefaultHeaders { get; } = new _DefaultHeaders();

        internal readonly Dictionary<string, List<OnSettingGUI>> settings = new Dictionary<string, List<OnSettingGUI>>();
        public delegate VisualElement OnSettingGUI();

        internal void Insert(OnSettingGUI callback, string header, int index)
        {

            if (header is null)
                return;

            if (!settings.ContainsKey(header))
                settings.Add(header, new List<OnSettingGUI>());

            if (!settings[header].Contains(callback))
                settings[header].Insert(index, callback);

        }

        /// <summary>Add field to settings tab.</summary>
        /// <param name="callback">The callback that is called when your field is to be constructed. An element of type <see cref="VisualElement"/> is expected to be returned.</param>
        /// <param name="header">The header to place the field under, see also <see cref="DefaultHeaders"/>, for the default headers.</param>
        public void Add(OnSettingGUI callback, string header)
        {

            if (header is null)
                return;

            if (!settings.ContainsKey(header))
                settings.Add(header, new List<OnSettingGUI>());

            if (!settings[header].Contains(callback))
                settings[header].Add(callback);

        }

        internal void Spacer(string header, int index) =>
            Insert(null, header, index);

        public void Spacer(string header) =>
            Add(null, header);

        /// <summary>Removes the setting field.</summary>
        public void Remove(OnSettingGUI callback)
        {
            foreach (var key in settings.Keys)
                settings[key].Remove(callback);
        }

    }

}
#endif
