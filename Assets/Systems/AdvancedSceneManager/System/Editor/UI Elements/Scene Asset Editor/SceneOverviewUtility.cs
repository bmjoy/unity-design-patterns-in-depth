#pragma warning disable IDE0051 // Remove unused private members

using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.SceneGroupingUtility;

namespace AdvancedSceneManager.Editor
{

    internal static partial class SceneOverviewUtility
    {

        /// <summary>Should return current value for the key, newValue is non-null when a new value is requested to be set, and contains the value to set.</summary>
        public delegate bool IsExpandedDelegate(string key, bool? newValue = null);

        const string collectionTemplate = "AdvancedSceneManager/Templates/SceneCollection";
        const string sceneTemplate = "AdvancedSceneManager/Templates/Scene";

        public static VisualElement CreateSceneOverview(IUIToolkitEditor editor, Scene[] scenes, Profile profile = null, IsExpandedDelegate isExpanded = null, Action<bool> allCheckboxHandler = null, Func<bool> allCheckboxDefaultValue = null) => CreateSceneOverview(editor, profile, Group(scenes, profile), isExpanded, allCheckboxHandler, allCheckboxDefaultValue);
        public static VisualElement CreateSceneOverview(IUIToolkitEditor editor, SceneAsset[] scenes, Profile profile = null, IsExpandedDelegate isExpanded = null, Action<bool> allCheckboxHandler = null, Func<bool> allCheckboxDefaultValue = null) => CreateSceneOverview(editor, profile, Group(scenes, profile), isExpanded, allCheckboxHandler, allCheckboxDefaultValue);

        public static VisualElement CreateSceneOverview(IUIToolkitEditor editor, Profile profile, Dictionary<string, List<(Item item, bool isLoadingScreen)>> items, IsExpandedDelegate isExpanded = null, Action<bool> allCheckboxHandler = null, Func<bool> allCheckboxDefaultValue = null)
        {

            var rootVisualElement = new VisualElement();

            LoadContent(collectionTemplate, rootVisualElement, loadStyle: true);
            LoadContent(sceneTemplate, rootVisualElement, loadStyle: true);

            LoadDefaultStyle(rootVisualElement);
            LoadContent(sceneTemplate, rootVisualElement, loadStyle: true);
            LoadContent("AdvancedSceneManager/Tabs/Scenes/Tab", rootVisualElement, loadStyle: true);
            LoadContent("AdvancedSceneManager/SceneManagerWindow", rootVisualElement, loadStyle: true);

            Reload();

            //Setup listeners for automatic reload when property changes or assets change
            SceneManager.assetManagement.AssetsChanged += Reload;
            AssetsSavedUtility.onAssetsSaved += (s) => Reload();

            var listenTo = items.
                Select(s => s.Value.SelectMany(v => v.item.collections.Select(c => c.collection))).
                Distinct().
                OfType<INotifyPropertyChanged>().
                Concat(items.SelectMany(v => v.Value.Select(s => s.item.scene))).
                ToArray();

            void Reload()
            {
                rootVisualElement.Clear();
                Generate(rootVisualElement);
            }

            void Generate(VisualElement element)
            {

                if (allCheckboxHandler != null && items.Sum(i => i.Value.Count) > 1)
                    ShowScene(null, null, null, element, editor, allCheckboxHandler, allCheckboxDefaultValue);

                var isFirst = true;
                foreach (var group in items)
                {

                    if (string.IsNullOrEmpty(group.Key))
                    {
                        foreach (var item in group.Value.GroupBy(i => i.item.scene).Select(g => g.FirstOrDefault()))
                            ShowScene(item.item, profile, profile ? profile.collections.FirstOrDefault(c => c.name == group.Key) : null, element, editor);
                        continue;
                    }

                    Header(group.Key, profile.name + "." + group.Key, element, isFirstHeader: isFirst, isExpanded: isExpanded);
                    isFirst = false;

                    if (isExpanded?.Invoke(profile.name + "." + group.Key) ?? true)
                        foreach (var item in group.Value.GroupBy(i => i.item.scene).Select(g => g.FirstOrDefault()))
                        {

                            if (item.isLoadingScreen)
                                Header("Loading screen:", profile.name + "." + "LoadingScreenHeader", element, FontStyle.Normal, topMargin: 6);
                            ShowScene(item.item, profile, profile ? profile.collections?.FirstOrDefault(c => c && c.name == group.Key) : null, element, editor);

                        }

                }

            }

            return rootVisualElement;

        }

        static void ShowScene(Item item, Profile profile, SceneCollection collection, VisualElement list, IUIToolkitEditor editor, Action<bool> onChecked = null, Func<bool> allCheckboxDefaultValue = null)
        {
            var template = CreateItem(list, profile, item?.scene, collection, item?.include is null, editor, onChecked, allCheckboxDefaultValue);
            if (!profile)
                template.style.SetMargin(left: -22);
        }

        static VisualElement CreateItem(VisualElement list, Profile profile, Scene scene, SceneCollection collection, bool isForceIncluded, IUIToolkitEditor parent, Action<bool> onChecked = null, Func<bool> allCheckboxDefaultValue = null)
        {

            var template = ScenesTab.CreateItem(collection, scene, 0, list, parent);

            template.Q(className: "sceneField").style.opacity = scene ? 1 : 0;
            template.Q("scene-drag-button").EnableInClassList("hidden", true);
            template.Q(className: "NewScene").EnableInClassList("hidden", true);
            template.Q("Scene-template-header-Remove").EnableInClassList("hidden", true);
            template.Q("Extra-Buttons").EnableInClassList("hidden", true);

            var toggle = new Toggle();
            toggle.SetValueWithoutNotify(onChecked is null ? scene.isIncluded : (allCheckboxDefaultValue?.Invoke() ?? false));
            toggle.RegisterValueChangedCallback(e =>
            {
                if (onChecked is null)
                    profile.SetStandalone(scene, e.newValue);
                else
                    onChecked?.Invoke(e.newValue);
            });

            template.Q(className: "Scene-template").Add(toggle);
            toggle.tooltip = "Include in build";

            if (onChecked is null)
                toggle.SetEnabled(!isForceIncluded);
            if (onChecked is null && isForceIncluded)
            {
                toggle.SetValueWithoutNotify(true);
                toggle.tooltip = onChecked is null
                    ? "This scene is part of a collection, used as loading screen, or set as splash screen"
                    : "Check / uncheck all (that are not locked)";
            }

            var button = new Button() { text = "⋮" };
            button.style.fontSize = 20;
            button.style.marginLeft = 6;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.maxWidth = 22;
            button.style.minWidth = 22;
            button.style.paddingLeft = 6;
            button.style.marginRight = 42;
            button.AddToClassList("LayerDropDown");
            button.AddToClassList("StandardButton");
            template.Q(className: "Scene-template").Add(button);

            if (scene && PersistentSceneInEditorUtility.GetPersistentOption(scene).option != PersistentSceneInEditorUtility.OpenInEditorOption.Never)
            {
                var indicator = new VisualElement();
                indicator.style.backgroundColor = Color.red;
                indicator.style.borderTopLeftRadius = 20;
                indicator.style.borderTopRightRadius = 20;
                indicator.style.borderBottomRightRadius = 20;
                indicator.style.borderBottomLeftRadius = 20;
                indicator.style.width = 6;
                indicator.style.height = 6;
                indicator.style.marginLeft = -6;
                button.Add(indicator);
            }

            button.clicked += () => OpenInEditorPopup.Open(button, parent, alignRight: true).Refresh(scene);
            button.style.opacity = scene ? 1 : 0;
            button.SetEnabled(scene);

            return template;

        }

        static VisualElement Horizontal(VisualElement parent = null)
        {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;
            if (parent != null)
                parent.Add(element);
            return element;
        }

        static VisualElement Header(string text, string expandedKey, VisualElement parent = null, FontStyle style = FontStyle.Bold, float topMargin = 22, float bottomMargin = 6, bool isFirstHeader = false, IsExpandedDelegate isExpanded = null)
        {

            var container = new VisualElement();
            container.AddToClassList("horizontal");

            var header = new Label(text);
            header.style.unityFontStyleAndWeight = style;
            header.style.marginTop = isFirstHeader ? 0 : topMargin;
            header.style.marginBottom = bottomMargin;
            container.Add(header);

            if (isExpanded != null)
            {

                var button = new Button();
                container.RegisterCallback<MouseDownEvent>(e =>
                {
                    isExpanded.Invoke(expandedKey, newValue: !isExpanded.Invoke(expandedKey));
                    button.text = isExpanded.Invoke(expandedKey) ? "▼" : "►";
                });

                button.text = isExpanded.Invoke(expandedKey) ? "▼" : "►";
                container.Insert(0, button);
                button.style.SetBorderWidth(all: 0);
                button.style.backgroundColor = Color.clear;
                button.style.alignSelf = Align.FlexEnd;
                button.style.marginBottom = 7;
                button.style.height = 12;

            }

            parent?.Add(container);

            return header;

        }

        static void LoadDefaultStyle(VisualElement element)
        {

            if (element == null)
                return;

            var light = Resources.Load<StyleSheet>("AdvancedSceneManager/Default-Light");
            var dark = Resources.Load<StyleSheet>("AdvancedSceneManager/Default-Dark");

            element.styleSheets.Remove(light);
            element.styleSheets.Remove(dark);

            if (SceneManagerWindow.IsDarkMode)
                element.styleSheets.Add(dark);
            else
                element.styleSheets.Add(light);

        }

        /// <summary>Loads the <see cref="VisualTreeAsset"/> and its associated <see cref="StyleSheet"/> at the same path.</summary>
        static void LoadContent(string path, VisualElement element, bool loadTree = false, bool loadStyle = false, bool clearChildren = false)
        {

            //Load all assets at path, since every VisualTreeAsset has an inline StyleSheet associated, 
            //which means that we can't rely on Resources.Load<StyleSheet>(path) since that
            //might randomly load the inline as the StyleSheet instead, which won't work since all of our 
            //uxml and uss assets that are associated share the same name
            var items = Resources.LoadAll(path);
            var style = items.OfType<StyleSheet>().Where(s => !s.name.Contains("inline")).FirstOrDefault();
            var tree = items.OfType<VisualTreeAsset>().FirstOrDefault();

            if (style && loadStyle && !element.styleSheets.Contains(style))
                element.styleSheets.Add(style);
            if (tree && loadTree)
                element.Add(tree.CloneTree());

        }

    }

}
