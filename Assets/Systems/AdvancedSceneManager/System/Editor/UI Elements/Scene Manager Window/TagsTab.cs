using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.SceneManagerWindow;

namespace AdvancedSceneManager.Editor
{

    public static class TagsTab
    {

        const string tagTemplate = "AdvancedSceneManager/Templates/Tag";

        public static (string title, Action action)[] FooterButtons() => new (string title, Action action)[]
        {
            ("New Tag", CreateNewTag),
        };

        public static void OnEnable(VisualElement element)
        {
            SceneManagerWindow.window.LoadContent(tagTemplate, element, loadStyle: true);
            PopulateList(element.Q("tag-list"));
        }

        static VisualElement list;
        static void PopulateList(VisualElement list)
        {

            if (list == null) list = TagsTab.list;
            if (list != null)
            {
                TagsTab.list = list;
                list.Clear();
                if (Profile.current)
                    foreach (var layer in Profile.current.tagDefinitions)
                        if (layer.id != SceneTag.Default.id)
                            CreateItem(layer, list);
            }

        }

        static void CreateItem(SceneTag tag, VisualElement list)
        {

            var element = Resources.Load<VisualTreeAsset>(tagTemplate).CloneTree();
            list.Add(element);

            //Title
            ReloadTitle();
            void ReloadTitle() =>
                element.Q<Label>("Tag-template-header-Label").text = tag.name;

            ReloadColor();
            void ReloadColor() =>
                element.Q(className: "Tag-ColorIndicator").style.backgroundColor = tag.color;

            //Remove collection button
            element.Q<Button>("Tag-template-header-Remove").clicked += () => Remove(tag);

            //Expander
            var expander = element.Q<ToolbarToggle>("Tag-template-expander");
            expander.SetValueWithoutNotify(SceneManagerWindow.window.openTagExpanders.GetValue(tag.id));

            element.Q<Label>("Tag-template-header-Label").RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == 0)
                    expander.value = !expander.value;
            });

            var content = element.Q("Tag-template-content");

            expander.RegisterValueChangedCallback(b => OnChecked());
            OnChecked();

            void OnChecked()
            {
                expander.text = expander.value ? "▼" : "►";
                SceneManagerWindow.window.openTagExpanders.Set(tag.id, expander.value);
                content.EnableInClassList("hidden", !expander.value);
            }

            element.Q<TextField>("Tag-template-Title").Setup(tag, nameof(tag.name), () => { ReloadTitle(); Save(); });
            element.Q<TextField>("Tag-template-Label").Setup(tag, nameof(tag.label), Save);
            element.Q<ColorField>("Tag-template-Color").Setup(tag, nameof(tag.color), () => { ReloadColor(); Save(); });
            element.Q<EnumField>("CloseBehavior").Setup(tag, nameof(tag.closeBehavior), Save);
            element.Q<EnumField>("OpenBehavior").Setup(tag, nameof(tag.openBehavior), Save);

            SceneManagerWindow.DragAndDropReorder.RegisterList(list, dragButtonName: "tag-drag-button", itemRootName: "tag-drag-root");

        }

        static void Save() =>
            SceneManagerWindow.Save(Profile.current);

        static void Remove(SceneTag layer)
        {
            ArrayUtility.Remove(ref Profile.current.tagDefinitions, layer);
            Save();
            SceneManagerWindow.ReopenTab();
        }

        static void CreateNewTag()
        {
            ArrayUtility.Add(ref Profile.current.tagDefinitions, new SceneTag("New Tag"));
            Save();
            SceneManagerWindow.ReopenTab();
        }

        public static void OnReorderEnd(DragAndDropReorder.DragElement element, int newIndex)
        {

            //We're hiding SceneLayer.None, which is always index 0, so we need to add one here since reorder only uses visual chilren
            var oldIndex = element.index;
            oldIndex += 1;
            newIndex += 1;

            var item = Profile.current.tagDefinitions[oldIndex];
            ArrayUtility.RemoveAt(ref Profile.current.tagDefinitions, oldIndex);
            ArrayUtility.Insert(ref Profile.current.tagDefinitions, newIndex, item);

            SceneManagerWindow.Save(Profile.current);

        }

    }

}
