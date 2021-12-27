using AdvancedSceneManager.Models;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.Utility.PersistentSceneInEditorUtility;

namespace AdvancedSceneManager.Editor
{

    public partial class OpenInEditorPopup : Popup<OpenInEditorPopup>
    {

        public override string path => "AdvancedSceneManager/Popups/OpenInEditor/Popup";

        public static float height { get; private set; }

        Scene scene;

        OpenInEditorSetting setting;
        public OpenInEditorPopup Refresh(Scene scene)
        {

            this.scene = scene;
            setting = GetPersistentOption(scene);
            rootVisualElement.Q<EnumField>("enum").Init(setting.option);
            rootVisualElement.Q<EnumField>("enum").RegisterValueChangedCallback(e => { setting.option = (OpenInEditorOption)e.newValue; OnOptionChanged(); });

            OnOptionChanged();
            void OnOptionChanged()
            {

                var isList = setting.option == OpenInEditorOption.WhenAnySceneOpensExcept || setting.option == OpenInEditorOption.WhenAnyOfTheFollowingScenesOpen;

                var list = rootVisualElement.Q("list");
                list.EnableInClassList("hidden", !isList);

                if (isList)
                {

                    if (setting.list == null)
                        setting.list = Array.Empty<string>();

                    list.Clear();

                    for (int i = 0; i < setting.list.Length; i++)
                        CreateSceneItem(setting.list[i], i, list, OnOptionChanged);

                    var addButton = new Button() { text = "+" };
                    addButton.AddToClassList("Scene-template-header-Remove");
                    addButton.style.alignSelf = Align.FlexEnd;
                    addButton.style.marginRight = 2;
                    addButton.clicked += () => { ArrayUtility.Add(ref setting.list, null); OnOptionChanged(); };
                    list.Add(addButton);

                }
                else
                    list.Clear();

                setting.Save();
                scene.OnPropertyChanged();

            }

            height = rootVisualElement.worldBound.height;
            return this;

        }

        void CreateSceneItem(string path, int index, VisualElement list, Action onChanged)
        {

            var listScene = SceneManager.assetManagement.scenes.Find(path);

            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;

            var sceneField = new SceneField();
            sceneField.SetValueWithoutNotify(listScene);
            sceneField.RegisterValueChangedCallback(e => { setting.list[index] = e.newValue ? e.newValue.path : ""; onChanged?.Invoke(); });

            var removeButton = new Button() { text = "-" };
            removeButton.AddToClassList("Scene-template-header-Remove");
            removeButton.clicked += () => { ArrayUtility.RemoveAt(ref setting.list, index); onChanged?.Invoke(); };

            item.Add(sceneField);
            item.Add(removeButton);

            list.Add(item);

        }

        protected override void OnReopen(OpenInEditorPopup newPopup) =>
            newPopup.Refresh(scene);

    }

}
