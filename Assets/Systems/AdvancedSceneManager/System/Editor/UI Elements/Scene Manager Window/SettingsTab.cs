using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using static AdvancedSceneManager.Editor.GenericPopup;
using Object = UnityEngine.Object;
using startup = AdvancedSceneManager.Core.AsyncOperations.OpenCollectionsAndScenesFlaggedToOpenAtStartAction;

namespace AdvancedSceneManager.Editor
{

    public static class SettingsTab
    {

        #region Names and tooltips for default settings

        static readonly Dictionary<string, string> names = new Dictionary<string, string>()
        {
            { nameof(Profile.splashScreen), "Splash screen:" },
            { nameof(Profile.startupLoadingScreen), "Startup loading screen:" },
            { nameof(Profile.loadingScreen), "Loading screen:" },

            { nameof(Profile.enableChangingBackgroundLoadingPriority), "Background loading priority:" },
            { nameof(Profile.backgroundLoadingPriority), "" },
            { nameof(Profile.createCameraForSplashScreen), "Create camera for splash screens:" },
            { nameof(Profile.useDefaultPauseScreen), "Use default pause screen:" },
            { nameof(Profile.enableCrossSceneReferences), "Enable cross-scene references:" },

            { ScenesTab.DisplayCollectionPlayButtonKey, "Display collection play button:" },
            { ScenesTab.DisplayCollectionOpenButtonKey, "Display collection open button:" },
            { ScenesTab.DisplayCollectionAdditiveButtonKey, "Display collection open additive button:" },

            { nameof(startup.pointlessCollectionWarning), "Pointless opening of collection during startup:" },
            { nameof(startup.pointlessSceneWarning), "Pointless opening of scene during startup:" },
            { nameof(SceneManagerWindow.deleteTempBuildMessage), "Deleting temp build:" },
            { nameof(CrossSceneReferenceUtility.unableToResolveCrossSceneReferencesWarning), "Unable to resolve cross-scene reference:" },

            { nameof(SceneManager.settings.addLabelsToSceneAssets), "Add labels to SceneAssets:" },
            { nameof(SceneManagerWindow.autoOpenScenesWhenCreated), "Open scenes when created using scene field:" },
            { nameof(BuildSettingsUtility.AllowEditingOfBuildSettings), "Allow manual editing of build settings:" },
        };

        static readonly Dictionary<string, string> tooltips = new Dictionary<string, string>()
        {
            { nameof(Profile.splashScreen), "The splash screen to play during startup." },
            { nameof(Profile.startupLoadingScreen), "The loading screen to use after splash screen (or immediately if no splash screen)." },
            { nameof(Profile.loadingScreen), "The default loading screen to use when opening collections." },

            { nameof(Profile.enableChangingBackgroundLoadingPriority), "Enable or disable ASM automatically changing background loading priority." },
            { nameof(Profile.backgroundLoadingPriority), "Enable or disable ASM automatically changing background loading priority." },

            { nameof(Profile.createCameraForSplashScreen), "Create a camera automatically during splash screen if none is exists." },
            { nameof(Profile.useDefaultPauseScreen), "Use the default pause screen. Which can be opened by pressing escape. If input system is installed and enabled, then the start button on a controller will also work." },
            { nameof(Profile.enableCrossSceneReferences), "Enable or disable cross-scene references. While certainly useful, it can slow down performance of the editor.\n\nCross-scene references is experimental because it has a tendency to break, we're trying our best to fix any bugs that pops up, but the reality is that this is a hack at best and it may never reach a status where it would not be considered experimental, unless Unity adds or changes APIs that makes this easier to implement." },

            { nameof(startup.pointlessCollectionWarning), "Log a warning in console when a collection was closed during startup and it contained no persistent scenes (EditorPrefs)." },
            { nameof(startup.pointlessSceneWarning), "Log a warning to console when a scene was closed during startup and it was not persistent (EditorPrefs)." },
            { nameof(SceneManagerWindow.deleteTempBuildMessage), "Log a message to console when temp build is deleted. This is to make debugging easier, you probably don't need to worry about this (EditorPrefs)." },
            { nameof(CrossSceneReferenceUtility.unableToResolveCrossSceneReferencesWarning), "Log a warning, or error if build, when a cross-scene reference could not be resolved (PlayerPrefs)." },

            { nameof(SceneManager.settings.addLabelsToSceneAssets),
                "Enabling this will result in ASM automatically adding asset labels to SceneAsset to match their associated Scene scriptable object, which may make finding scenes in project easier.\n\n" +
                "But, it also comes with the drawback that unity may randomly ask to reload open scenes due to external changes, this may be ASM changing asset labels during refresh, disabling this would resolve that.\n\nNote that the aforementioned message may still pop up for loading screens or splash screens.\n\n(saved in project wide asm settings)." },

            { nameof(SceneManagerWindow.autoOpenScenesWhenCreated), "When creating a scene using a scene field, open scene automatically." },
            { nameof(BuildSettingsUtility.AllowEditingOfBuildSettings), "ASM manages build settings automatically, and it is not recommended to manually modify them.\n\nFor the event when this is required though, this setting will allow manual edit of build settings." },

            { Settings.DefaultHeaders.Options_Profile, "Settings that are saved in the current profile." },
            { Settings.DefaultHeaders.Options_Project, "Settings that are saved in SceneManager.settings, project wide." },
            { Settings.DefaultHeaders.Options_Local, "Settings that are saved locally, on this PC." },

        };

        #endregion
        #region Add / Remove callbacks

        public static class HeaderOrder
        {

            static readonly List<string> order = new List<string>()
            {
                Utility.SettingsTab.instance.DefaultHeaders.Options,
                Utility.SettingsTab.instance.DefaultHeaders.Options_Profile,
                Utility.SettingsTab.instance.DefaultHeaders.Options_Project,
                Utility.SettingsTab.instance.DefaultHeaders.Options_Local,
                Utility.SettingsTab.instance.DefaultHeaders.Appearance,
                Utility.SettingsTab.instance.DefaultHeaders.Experimental,
                Utility.SettingsTab.instance.DefaultHeaders.Log,
            };

            public static void Set(string header, int order)
            {
                var i = HeaderOrder.order.IndexOf(header);
                HeaderOrder.order.Insert(order, header);
                if (i != 1)
                    HeaderOrder.order.RemoveAt(i);
            }

            public static int Get(string header)
            {

                if (!order.Contains(header))
                    order.Add(header);

                return order.IndexOf(header);

            }

        }
        public static Utility.SettingsTab Settings => Utility.SettingsTab.instance;

        #endregion
        #region UI

        static (Toggle toggle, EnumField enumField, VisualElement root) EnumToggleField(string toggleProperty, string enumProperty)
        {

            var root = new VisualElement();
            root.AddToClassList("horizontal");

            var toggle = Toggle(toggleProperty);
            var enumField = Enum(enumProperty);
            enumField.style.flexGrow = 1;

            toggle.RegisterValueChangedCallback(value => enumField.SetEnabled(value.newValue));
            enumField.SetEnabled(toggle.value);

            root.Add(toggle);
            root.Add(enumField);

            root.style.marginBottom = -2;

            return (toggle, enumField, root);

        }

        static Toggle Toggle(string property) =>
            new Toggle(names.GetValue(property)).Setup(Profile.current, property, tooltip: tooltips.GetValue(property));

        static Toggle TogglePref(string editorPrefsKey, bool defaultValue = false) =>
            new Toggle(names.GetValue(editorPrefsKey)).Setup(e =>
                PlayerPrefs.SetInt(editorPrefsKey, e.newValue ? 1 : 0),
                PlayerPrefs.GetInt(editorPrefsKey, defaultValue ? 1 : 0) == 1,
                tooltip: tooltips.GetValue(editorPrefsKey));

        static Toggle Toggle(string property, EventCallback<ChangeEvent<bool>> callback, bool defaultValue = false) =>
            new Toggle(names.GetValue(property)).Setup(callback, defaultValue, tooltip: tooltips.GetValue(property));

        static SceneField Scene(string property, string label = "") =>
            (new SceneField() { labelFilter = label }).Setup(label: names.GetValue(property), Profile.current, property, tooltip: tooltips.GetValue(property));

        static EnumField Enum(string property, Action callback = null) =>
            new EnumField(names.GetValue(property)).Setup(Profile.current, property, callback);

        #endregion

        static bool IsExpanded(string header, bool? value = null)
        {

            if (value.HasValue)
            {
                PlayerPrefs.SetInt("AdvancedSceneManager.SettingsTab." + header, value.Value ? 1 : 0);
                return value.Value;
            }

            return PlayerPrefs.GetInt("AdvancedSceneManager.SettingsTab." + header, 0) == 1;

        }

        public static void OnEnable(VisualElement element)
        {
            AddDefaultSettings();
            InitializeProfile(element);
            InitializeSettings(element);
        }

        static void InitializeProfile(VisualElement element)
        {

            var profile = element.Q<ObjectField>("Settings-Profile");
            profile.SetValueWithoutNotify(Profile.current);

            profile.RegisterValueChangedCallback(ValueChanged);

            void ValueChanged(ChangeEvent<Object> e)
            {
                SceneManagerWindow.IgnoreProfileChanged();
                Profile.current = (Profile)e.newValue;
                profile.UnregisterValueChangedCallback(ValueChanged);
                SceneManagerWindow.OnProfileChanged(Profile.current);
                OnEnable(element);
            }

            var newButton = element.Q<ToolbarToggle>("Settings-New-Profile-Button");
            newButton.RegisterValueChangedCallback(e =>
            {

                GenericPopup.Open(newButton, SceneManagerWindow.window, alignRight: true, offset: new Vector2(0, -3)).Refresh(
                    Item.Create("New profile", () => { SceneManager.assetManagement.CreateProfileAndAssign(); SceneManagerWindow.Reload(); }),
                    Item.Create("Duplicate profile", () => { SceneManager.assetManagement.DuplicateProfileAndAssign(); SceneManagerWindow.Reload(); }).WithEnabledState(HasProfile()),
                    Item.Separator,
                    Item.Create("Delete profile", DeleteProfile).WithEnabledState(HasProfile()));

                bool HasProfile() =>
                    Profile.current;

            });

        }

        static void AddDefaultSettings()
        {

            //Options
            Settings.Add(header: Settings.DefaultHeaders.Options_Profile, callback: () =>
            {

                var root = new VisualElement();
                root.style.marginBottom = 12;

                root.Add(Scene(nameof(Profile.splashScreen), "ASM: SplashScreen"));
                root.Add(Scene(nameof(Profile.startupLoadingScreen), label: "LoadingScreen"));
                root.Add(Scene(nameof(Profile.loadingScreen), label: "LoadingScreen"));

                return root;

            });

            //Options
            //Profile
            Settings.Add(() => EnumToggleField(toggleProperty: nameof(Profile.enableChangingBackgroundLoadingPriority), enumProperty: nameof(Profile.backgroundLoadingPriority)).root, header: Settings.DefaultHeaders.Options_Profile);
            Settings.Add(() => Toggle(nameof(Profile.createCameraForSplashScreen)), header: Settings.DefaultHeaders.Options_Profile);
            Settings.Add(() => Toggle(nameof(Profile.useDefaultPauseScreen)), header: Settings.DefaultHeaders.Options_Profile);

            //Project
            Settings.Add(() => Toggle(nameof(ASMSettings.addLabelsToSceneAssets), e => { SceneManager.settings.addLabelsToSceneAssets = e.newValue; SceneManager.settings.MarkAsDirty(); AssetRefreshUtility.Refresh(); }, SceneManager.settings.addLabelsToSceneAssets), header: Settings.DefaultHeaders.Options_Project);

            //Local
            Settings.Add(() => Toggle(nameof(SceneManagerWindow.autoOpenScenesWhenCreated), e => { SceneManagerWindow.window.autoOpenScenesWhenCreated = e.newValue; SceneManagerWindow.window.Save(); }, SceneManagerWindow.window.autoOpenScenesWhenCreated), header: Settings.DefaultHeaders.Options_Local);
            Settings.Add(() => Toggle(nameof(BuildSettingsUtility.AllowEditingOfBuildSettings), e => { BuildSettingsUtility.AllowEditingOfBuildSettings = e.newValue; }, BuildSettingsUtility.AllowEditingOfBuildSettings), header: Settings.DefaultHeaders.Options_Local);

            //Appearance
            Settings.Insert(() => TogglePref(ScenesTab.DisplayCollectionPlayButtonKey, defaultValue: true), header: Settings.DefaultHeaders.Appearance, index: 0);
            Settings.Insert(() => TogglePref(ScenesTab.DisplayCollectionOpenButtonKey, defaultValue: true), header: Settings.DefaultHeaders.Appearance, index: 1);
            Settings.Insert(() => TogglePref(ScenesTab.DisplayCollectionAdditiveButtonKey, defaultValue: true), header: Settings.DefaultHeaders.Appearance, index: 2);
            Settings.Spacer(header: Settings.DefaultHeaders.Appearance, 3);

            //Log
            Settings.Add(() => Toggle(nameof(startup.pointlessCollectionWarning), e => startup.pointlessCollectionWarning = e.newValue, startup.pointlessCollectionWarning), header: Settings.DefaultHeaders.Log);
            Settings.Add(() => Toggle(nameof(startup.pointlessSceneWarning), e => startup.pointlessSceneWarning = e.newValue, startup.pointlessSceneWarning), header: Settings.DefaultHeaders.Log);

            Settings.Add(() => Toggle(nameof(SceneManagerWindow.deleteTempBuildMessage), e => SceneManagerWindow.deleteTempBuildMessage = e.newValue, SceneManagerWindow.deleteTempBuildMessage), header: Settings.DefaultHeaders.Log);
            Settings.Add(() => Toggle(nameof(CrossSceneReferenceUtility.unableToResolveCrossSceneReferencesWarning), e => CrossSceneReferenceUtility.unableToResolveCrossSceneReferencesWarning = e.newValue, CrossSceneReferenceUtility.unableToResolveCrossSceneReferencesWarning), header: Settings.DefaultHeaders.Log);

            //Experimental
            Settings.Add(() => Toggle(nameof(Profile.enableCrossSceneReferences)), header: Settings.DefaultHeaders.Experimental);

        }

        static void InitializeSettings(VisualElement element)
        {

            var extraSettings = element.Q<VisualElement>("extraSettings");
            extraSettings.Clear();

            extraSettings.style.SetMargin(top: 6);

            var headers = Settings.settings.Keys.Select(k => !k.Contains("_") ? k : k.Remove(k.IndexOf("_"))).Distinct().OrderBy(HeaderOrder.Get).ToArray();

            foreach (var header in headers)
            {

                var box = new Box();
                var container = new VisualElement();
                var button = new Button();
                extraSettings.Add(box);
                box.Add(button);
                box.Add(container);
                box.style.SetMargin(top: 6, horizontal: 0, bottom: 0);
                container.style.SetMargin(all: 12, top: 0);

                var settings = Settings.settings.Where(s => s.Key.StartsWith(header)).OrderBy(v => HeaderOrder.Get(v.Key)).ToArray();

                foreach (var setting in settings)
                {

                    if (setting.Key.Contains("_"))
                    {
                        var text = new TextElement() { text = setting.Key.Substring(setting.Key.IndexOf("_") + 1) };
                        text.style.unityFontStyleAndWeight = FontStyle.Bold;
                        text.style.marginTop = 6;
                        text.style.marginBottom = 4;
                        container.Add(text);
                        text.tooltip = tooltips.GetValue(setting.Key);
                    }

                    foreach (var callback in setting.Value)
                    {

                        if (callback is null)
                        {
                            //Spacer
                            var el = new VisualElement();
                            el.style.height = 12;
                            container.Add(el);
                        }
                        else if (callback.Invoke() is VisualElement el && el != null)
                        {
                            el.SetEnabled(setting.Key != Settings.DefaultHeaders.Options_Profile || Profile.current);
                            container.Add(el);
                        }

                    }

                }

                button.AddToClassList("header");

                ReloadExpander();
                button.clicked += () =>
                {
                    IsExpanded(header, !IsExpanded(header));
                    ReloadExpander();
                };

                void ReloadExpander()
                {

                    var isExpanded = IsExpanded(header);

                    button.style.marginBottom = isExpanded ? 12 : 0;
                    button.style.marginTop = isExpanded ? 2 : 0;
                    box.style.SetPadding(4);
                    button.text = (isExpanded ? "▼  " : "►  ") + header;

                    container.EnableInClassList("hidden", !isExpanded);

                }

            }

        }

        static void DeleteProfile()
        {
            if (Profile.current)
                Profile.current.Delete();
        }

    }

}
