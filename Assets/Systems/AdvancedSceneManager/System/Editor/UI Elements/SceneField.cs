#pragma warning disable IDE0051 // Remove unused private members

using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using Lazy.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    /// <summary>Object field with type property that can be set from uxml. Also has <see cref="isReadOnly"/> property to allow selecting or opening value, but not allow changing value.</summary>
    public class ObjectField : UnityEditor.UIElements.ObjectField
    {

        bool m_isReadOnly;
        public bool isReadOnly
        {
            get => m_isReadOnly;
            set { m_isReadOnly = value; ApplyReadOnly(); }
        }

        public ObjectField() : base()
        {
            ApplyReadOnly();
        }

        void ApplyReadOnly()
        {
            Children().Last().SetEnabled(!isReadOnly);
        }

        public new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits>
        { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            readonly UxmlStringAttributeDescription m_label = new UxmlStringAttributeDescription() { name = "label" };
            readonly UxmlStringAttributeDescription m_type = new UxmlStringAttributeDescription() { name = "type" };
            readonly UxmlStringAttributeDescription m_allowSceneObjects = new UxmlStringAttributeDescription() { name = "allowSceneObjects" };
            readonly UxmlStringAttributeDescription m_isReadOnly = new UxmlStringAttributeDescription() { name = "isReadOnly" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {

                base.Init(ve, bag, cc);

                var element = ve as ObjectField;
                element.label = m_label.GetValueFromBag(bag, cc);

                var typeName = m_type.GetValueFromBag(bag, cc);
                if (AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == typeName) is Type type)
                    element.objectType = type;

                bool.TryParse(m_allowSceneObjects.GetValueFromBag(bag, cc), out var b);
                element.allowSceneObjects = b;

                bool.TryParse(m_allowSceneObjects.GetValueFromBag(bag, cc), out var isReadOnly);
                element.isReadOnly = isReadOnly;

            }

        }

    }

    /// <summary>
    /// <para>An <see cref="ObjectField"/> that only accepts <see cref="Scene"/>, with support for <see cref="SceneAsset"/> drag drop.</para>
    /// <para>Has support for <see cref="labelFilter"/>, which filters scenes based on label (i.e. to only show scenes from 'Collection1', for example, use 'ASM:Collection1').</para>
    /// <para><see cref="showOpenButtons"/> can be used to toggle open buttons.</para>
    /// <para>When <see cref="ObjectField.isReadOnly"/> is true, <see cref="showOpenButtons"/> will still be interactable, but value cannot be changed.</para>
    /// </summary>
    public class SceneField : ObjectField
    {

        bool PassesFilter(Scene scene) =>
            PassesFilter(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path));

        bool PassesFilter(SceneAsset asset) =>
            string.IsNullOrWhiteSpace(labelFilter) || AssetDatabase.GetLabels(asset).Contains(labelFilter);

        public SceneField() : base()
        {

            allowSceneObjects = false;
            objectType = typeof(Scene);

            SetupDragDrop();
            SetupMouseEvents();
            SetupOpenButtons();

            //OnValueChanged(value, value);
            CoroutineUtility.Run(after: 0.1f, action: () =>
            {
                OnValueChanged(value, value);
                UpdateEnabled();
            });

        }

        #region Mouse events

        void SetupDragDrop()
        {

            var scenes = new List<Scene>();

            //This fixes a bug where dropping a scene one pixel above this element would result in null being assigned to this field
            var element = this.Q(className: "unity-object-field-display");
            if (element == null)
                return;

            //element.RegisterCallback<DragPerformEvent>(e => e.PreventDefault());

            element.RegisterCallback<DragEnterEvent>(e =>
            {

                e.PreventDefault();

                scenes.Clear();
                scenes.AddRange(DragAndDrop.objectReferences.OfType<Scene>());
                scenes.AddRange(DragAndDrop.objectReferences.OfType<SceneAsset>().Select(s => s.FindASMScene()));
                scenes.AddRange(DragAndDrop.paths.Select(AssetDatabase.LoadAssetAtPath<SceneAsset>).OfType<SceneAsset>().Select(s => s.FindASMScene()));
                scenes = scenes.Where(s => s).GroupBy(s => s.path).Select(g => g.First()).ToList();

                if (scenes.Any())
                {
                    DragAndDrop.AcceptDrag();
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                }

            });

            element.RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.PreventDefault();
                DragAndDrop.AcceptDrag();
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            });

            element.RegisterCallback<DragLeaveEvent>(e => Cancel());
            element.RegisterCallback<DragPerformEvent>(e =>
            {
                e.PreventDefault();
                if (scenes.Any())
                    value = scenes.FirstOrDefault();
                Cancel();
            });

            var down = false;
            element.RegisterCallback<MouseDownEvent>(e => down = true);
            element.RegisterCallback<MouseLeaveEvent>(e => down = false);
            element.RegisterCallback<MouseUpEvent>(e => down = false);

            element.RegisterCallback<MouseMoveEvent>(e =>
            {

                if (!down)
                    return;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { value };
                DragAndDrop.StartDrag("Scene");

            });

            void Cancel() =>
                scenes.Clear();

        }

        void SetupMouseEvents()
        {

            if (disableScene)
                style.opacity = 0.5f;

            var down = false;
            RegisterCallback<MouseDownEvent>(e =>
            {
                down = true;
                e.PreventDefault();

            }, TrickleDown.TrickleDown);

            RegisterCallback<MouseLeaveEvent>(e => down = false);

            RegisterCallback<MouseUpEvent>(e =>
            {

                e.PreventDefault();

                if (!down)
                    return;
                down = false;

                if (!disableScene && e.localMousePosition.x > worldBound.width - 20 && !isReadOnly)
                {

                    var currentPickerWindow = GUIUtility.GetControlID(FocusType.Passive) + 100;
                    var filter = string.IsNullOrWhiteSpace(labelFilter) ? "" : "l:" + labelFilter;

                    var popup = Popup.current;
                    EditorGUIUtility.ShowObjectPicker<Scene>(value, false, filter, currentPickerWindow);

                    Coroutine().StartCoroutine();
                    IEnumerator Coroutine()
                    {

                        while (EditorGUIUtility.GetObjectPickerControlID() == currentPickerWindow)
                        {

                            var sceneAsset = EditorGUIUtility.GetObjectPickerObject() as Scene;
                            value = sceneAsset;
                            yield return null;

                        };

                        popup?.Reopen();

                    }

                }
                else if (e.localMousePosition.x > buttonAdditive.localBound.xMax + 3)
                {

                    if (!value)
                        return;
                    var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(value.assetID));
                    EditorGUIUtilityExt.PingOrOpenAsset(asset, e.clickCount);

                }

            }, TrickleDown.TrickleDown);

        }

        #endregion
        #region Open buttons

        Button buttonSingle;
        Button buttonAdditive;
        public event Action OnSceneOpen;
        public event Action OnSceneOpenAdditive;
        void RefreshSceneOpen()
        {
            if (buttonAdditive == null)
                return;
            buttonAdditive.text = IsSceneOpen() ? "-" : "+";
        }

        void UpdateEnabled()
        {
            buttonSingle.SetEnabled(value);
            buttonAdditive.SetEnabled(value);
        }

        bool IsSceneOpen() =>
            Application.isPlaying
                ? SceneManager.utility.IsOpen(value)
                : SceneManager.editor.IsOpen(value);

        void SetupOpenButtons()
        {

            if (!showOpenButtons)
                return;

            buttonSingle = new Button() { text = "↪", tooltip = "Open scene" };
            buttonSingle.AddToClassList("StandardButton");
            buttonSingle.AddToClassList("OpenScene");

            buttonAdditive = new Button() { text = "+", tooltip = "Open scene additively" };
            buttonAdditive.AddToClassList("StandardButton");
            buttonAdditive.AddToClassList("OpenScene");
            buttonAdditive.AddToClassList("additive");

            buttonSingle.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));

            buttonSingle.style.marginTop = -0.5f;
            buttonAdditive.style.marginTop = -0.5f;

            buttonSingle.clicked += () => OnOpen(false);
            buttonAdditive.clicked += () => OnOpen(true);

            RefreshSceneOpen();
            UpdateEnabled();

            RegisterValueChangedCallback(e => UpdateEnabled());

            void OnOpen(bool additive)
            {

                if (!value)
                    return;

                if (!Application.isPlaying)
                {
                    OpenEditor(value, additive);
                    value.OnPropertyChanged();
                }
                else
                {

                    Open().StartCoroutine();
                    IEnumerator Open() =>
                        SceneField.Open(value, additive);

                }

                if (!additive)
                    OnSceneOpen?.Invoke();
                else
                    OnSceneOpenAdditive?.Invoke();

            }

            Insert(0, buttonAdditive);
            Insert(0, buttonSingle);

        }

        static IEnumerator Open(Scene scene, bool additive)
        {

            if (!Application.isPlaying)
                yield break;

            if (SceneManager.standalone.IsOpen(scene) && additive)
                yield return SceneManager.standalone.Close(scene.GetOpenSceneInfo());
            else if (additive)
                yield return SceneManager.standalone.Open(scene);
            else
                yield return SceneManager.standalone.OpenSingle(scene);

        }

        static void OpenEditor(Scene scene, bool additive)
        {

            if (Application.isPlaying)
                return;

            if (SceneManager.editor.IsOpen(scene) && additive)
                SceneManager.editor.Close(scene);
            else if (additive)
                SceneManager.editor.Open(scene);
            else
                SceneManager.editor.OpenSingle(scene);

        }

        #endregion
        #region Value changed

        public SceneField SetValueWithoutNotify(Scene scene)
        {
            base.SetValueWithoutNotify(scene);
            return this;
        }

        public class SceneChangedEvent : ChangeEvent<Scene>
        {

            public SceneChangedEvent(Scene newValue, Scene oldValue)
            {
                this.newValue = newValue;
                this.previousValue = oldValue;
            }

        }

        public void RegisterValueChangedCallback(EventCallback<ChangeEvent<Scene>> callback)
        {
            INotifyValueChangedExtensions.RegisterValueChangedCallback(this, new EventCallback<ChangeEvent<UnityEngine.Object>>(e => callback.Invoke(new SceneChangedEvent(e.newValue as Scene, e.previousValue as Scene))));
        }

        public new Scene value
        {
            get => (Scene)base.value;
            set { OnValueChanged(this.value, value); base.value = value; }
        }

        void OnValueChanged(Scene oldValue, Scene newValue)
        {

            if (oldValue)
                oldValue.PropertyChanged -= OnValuePropertyChanged;
            if (newValue)
            {
                newValue.PropertyChanged -= OnValuePropertyChanged;
                newValue.PropertyChanged += OnValuePropertyChanged;
                OnValuePropertyChanged(null, null);
            }

            var label = this.Q<Label>(className: "unity-object-field-display__label");
            var display = this.Q(className: "unity-object-field__input");
            label.TrimLabel((newValue ? newValue.name : "None") + " (Scene)", maxWidth: () => display.resolvedStyle.width - 42, enableAuto: true);

        }

        void OnValuePropertyChanged(object sender, EventArgs e)
        {
            RefreshSceneOpen();
        }

        #endregion

        public string labelFilter { get; set; }
        public bool showOpenButtons { get; set; } = true;
        public bool disableScene { get; set; }

        public new class UxmlFactory : UxmlFactory<SceneField, UxmlTraits>
        { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            readonly UxmlStringAttributeDescription m_label = new UxmlStringAttributeDescription() { name = "label" };
            readonly UxmlStringAttributeDescription m_showOpenButtons = new UxmlStringAttributeDescription() { name = "showOpenButtons" };
            readonly UxmlStringAttributeDescription m_labelFilter = new UxmlStringAttributeDescription() { name = "labelFilter" };
            readonly UxmlStringAttributeDescription m_type = new UxmlStringAttributeDescription() { name = "type" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {

                base.Init(ve, bag, cc);

                var element = ve as SceneField;
                element.label = m_label.GetValueFromBag(bag, cc);
                if (Type.GetType(m_type.GetValueFromBag(bag, cc)) is Type type)
                    element.objectType = type;

                if (bool.TryParse(m_showOpenButtons.GetValueFromBag(bag, cc), out var b))
                    element.showOpenButtons = b;

                element.labelFilter = m_labelFilter.GetValueFromBag(bag, cc);

            }

        }

    }

}
