using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.SceneManagerWindow;

namespace AdvancedSceneManager.Editor
{

    public static partial class ScenesTab
    {

        public static (string title, Action action)[] FooterButtons() => new (string title, Action action)[]
        {
            ("New Collection", CreateNewCollection),
        };

        public static void OnEnable(VisualElement element)
        {

            SceneManagerWindow.window.LoadContent(collectionTemplate, element, loadStyle: true);
            SceneManagerWindow.window.LoadContent(sceneTemplate, element, loadStyle: true);

            PopulateList(element.Q("collection-list"));

        }

        #region PropertyChanged

        static Timer timer;
        static void OnPropertyChanged(object sender, EventArgs e)
        {
            if (timer == null)
            {
                timer = new Timer(500);
                timer.Elapsed += (s, e1) =>
                {
                    timer.Stop();
                    PopulateList();
                };
            }
            timer.Stop();
            timer.Start();
        }

        static void AddListener(INotifyPropertyChanged obj)
        {
            if (obj == null) return;
            obj.PropertyChanged -= OnPropertyChanged;
            obj.PropertyChanged += OnPropertyChanged;
            listeners.Add(obj);
        }

        static readonly List<INotifyPropertyChanged> listeners = new List<INotifyPropertyChanged>();
        public static void OnDisable()
        {
            listeners.ForEach(l => l.PropertyChanged -= OnPropertyChanged);
            listeners.Clear();
        }

        #endregion

        const string collectionTemplate = "AdvancedSceneManager/Templates/SceneCollection";
        const string sceneTemplate = "AdvancedSceneManager/Templates/Scene";

        static VisualElement list;

        /// <summary>Populate using last <see cref="VisualElement"/> list.</summary>
        static void PopulateList() => PopulateList(null);

        /// <summary>
        /// <para>Populate the <see cref="VisualElement"/> list.</para>
        /// <para>Passing <paramref name="list"/> as null will use last list.</para>
        /// </summary>
        static void PopulateList(VisualElement list = null)
        {

            if (BuildPipeline.isBuildingPlayer)
                return;

            ScenesTab.list?.Clear();
            if (list == null) list = ScenesTab.list;
            if (list != null)
            {

                ScenesTab.list = list;
                list.Clear();

                if (Profile.current)
                    foreach (var collection in Profile.current.collections.OrderBy(Profile.current.Order).ToArray())
                        CreateItem(collection, list);

                DragAndDropReorder.RegisterList(list,
                    itemRootName: "collection-drag-root", dragButtonName: "collection-drag-button");

            }

        }

        public static void OnReorderStart(DragAndDropReorder.DragElement element)
        {
            var isCollectionsList = element.list == list;
            if (isCollectionsList)
                list.Query<ToolbarToggle>("Collections-template-expander").ForEach(e =>
                {
                    if (e.value)
                        SceneManagerWindow.window.openCollectionExpanders.Set((e.userData as SceneCollection).name, true);
                    e.value = false;
                });
        }

        public static void OnReorderEnd(DragAndDropReorder.DragElement element, int newIndex)
        {

            var isCollectionsList = element.list == list;

            if (isCollectionsList)
            {
                var l = Profile.current.collections.ToList();
                var item = l[element.index];
                Profile.current.Order(item, newIndex);
                Save(Profile.current);
            }
            else //isSceneList
            {

                if (element.item.userData == null)
                    return;

                (SceneCollection collection, Scene _) = ((SceneCollection collection, Scene scene))element.item.userData;

                var l = collection.scenes.ToList();
                var item = l[element.index];
                l.RemoveAt(element.index);
                l.Insert(newIndex, item);
                collection.scenes = l.ToArray();

                Save(collection);

            }

        }

        #region Appearance options

        public const string DisplayCollectionPlayButtonKey = "AdvancedSceneManager.Appearance.CollectionPlayButton";
        public const string DisplayCollectionOpenButtonKey = "AdvancedSceneManager.Appearance.CollectionOpenButton";
        public const string DisplayCollectionAdditiveButtonKey = "AdvancedSceneManager.Appearance.CollectionAdditiveButton";

        public static bool DisplayCollectionPlayButton
        {
            get => PlayerPrefs.GetInt(DisplayCollectionPlayButtonKey, 1) == 1;
            set => PlayerPrefs.SetInt(DisplayCollectionPlayButtonKey, value ? 1 : 0);
        }

        public static bool DisplayCollectionOpenButton
        {
            get => PlayerPrefs.GetInt(DisplayCollectionOpenButtonKey, 1) == 1;
            set => PlayerPrefs.SetInt(DisplayCollectionOpenButtonKey, value ? 1 : 0);
        }

        public static bool DisplayCollectionAdditiveButton
        {
            get => PlayerPrefs.GetInt(DisplayCollectionAdditiveButtonKey, 1) == 1;
            set => PlayerPrefs.SetInt(DisplayCollectionAdditiveButtonKey, value ? 1 : 0);
        }

        #endregion
        #region Create list items

        class HeaderClickManipulator : MouseManipulator
        {

            readonly ToolbarToggle toggle;

            public HeaderClickManipulator(ToolbarToggle toggle)
            {
                this.toggle = toggle;
            }

            static IEventHandler downHeader = null;
            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(MouseDown);
                target.RegisterCallback<MouseLeaveEvent>(MouseLeave);
                target.RegisterCallback<MouseUpEvent>(MouseUp);
                target.RegisterCallback<MouseMoveEvent>(MouseMove);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(MouseDown);
                target.UnregisterCallback<MouseLeaveEvent>(MouseLeave);
                target.UnregisterCallback<MouseUpEvent>(MouseUp);
                target.UnregisterCallback<MouseMoveEvent>(MouseMove);
            }

            bool down;
            void MouseDown(MouseDownEvent e)
            {
                if (e.button == 0 && e.modifiers == EventModifiers.None)
                {
                    down = true;
                    downHeader = e.target;
                }
            }

            void MouseLeave(MouseLeaveEvent e)
            {
                downHeader = null;
                down = false;
            }

            void MouseUp(MouseUpEvent e)
            {
                if (downHeader == e.target && e.button == 0 && e.modifiers == EventModifiers.None)
                {
                    toggle.value = !toggle.value;
                    down = false;
                }
            }

            void MouseMove(MouseMoveEvent e)
            {

                if (!down)
                    return;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { toggle.userData as SceneCollection };
                DragAndDrop.StartDrag("SceneCollection");

            }

        }

        static bool IsExpanded(SceneCollection collection, bool? expanded = null)
        {

            var id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collection));
            if (expanded.HasValue)
                SceneManagerWindow.window.openCollectionExpanders.Set(id, expanded.Value);
            return SceneManagerWindow.window.openCollectionExpanders.GetValue(id);

        }

        static void CreateItem(SceneCollection collection, VisualElement list)
        {

            if (BuildPipeline.isBuildingPlayer || !collection)
                return;

            AddListener(collection);

            var res = Resources.Load<VisualTreeAsset>(collectionTemplate);
            if (!res)
                return;
            var element = res.CloneTree();

            list.Add(element);

            var label = element.Q<Label>("Collection-template-header-Label");
            //Title
            ReloadTitle();
            void ReloadTitle()
            {
                label.text = collection ? collection.title : "";
                TrimUtility.TrimLabel(label, label.text, MaxLabelWidth, enableAuto: true);
            }

            float MaxLabelWidth() =>
                element.Q(name: "Extra-Buttons").localBound.x - label.localBound.x;

            //Play button
            var button = element.Q<Button>("openPlay");
            button.EnableInClassList("hidden", !DisplayCollectionPlayButton);
            if (DisplayCollectionPlayButton)
            {
                button.clickable.activators.Add(new ManipulatorActivationFilter() { modifiers = EventModifiers.Shift });
                button.clicked += () => Play(collection);
            }

            //Open button
            button = element.Q<Button>("open");
            button.EnableInClassList("hidden", !DisplayCollectionOpenButton);
            if (DisplayCollectionOpenButton)
            {

                button.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));

                button.clickable.activators.Add(new ManipulatorActivationFilter() { modifiers = EventModifiers.Shift });
                button.clicked += () => Open(collection);

            }

            //Open additive button
            button = element.Q<Button>("openAdditive");
            button.EnableInClassList("hidden", !DisplayCollectionAdditiveButton);
            if (DisplayCollectionAdditiveButton)
            {
                button.clickable.activators.Add(new ManipulatorActivationFilter() { modifiers = EventModifiers.Shift });
                button.clickable.clickedWithEventInfo += (e) => Open(collection, additive: true);
            }

            SceneManager.editor.scenesUpdated += UpdateAdditiveButton;
            EditorApplication.playModeStateChanged += (s) => UpdateAdditiveButton();
            UpdateAdditiveButton();
            void UpdateAdditiveButton()
            {
                button.SetEnabledExt(!Application.isPlaying);
                button.text = IsOpen(collection) ? "-" : "+";
            }

            //Add scene button
            element.Q<Button>("Collection-template-header-Add").clicked += () => CreateNewScene(collection);

            //Remove collection button
            element.Q<Button>("Collection-template-header-Remove").clicked += () => RemoveCollection(collection);

            //Edit collection popup
            var menuButton = element.Q<ToolbarToggle>(name: "settingsButton");
            menuButton.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));
            menuButton.RegisterValueChangedCallback(e =>
            EditCollectionPopup.Open(
                placementTarget: menuButton,
                parent: SceneManagerWindow.window,
                alignRight: true,
                offset: new Vector2(0, -3)
                ).Refresh(collection, ReloadTitle, ReloadStar));

            //Expander
            var expander = element.Q<ToolbarToggle>("Collections-template-expander");
            expander.SetValueWithoutNotify(IsExpanded(collection));
            element.Q<Label>("Collection-template-header-Label").AddManipulator(new HeaderClickManipulator(expander));
            expander.userData = collection;

            //Used to restore expanded state after reorder
            expander.userData = collection;
            SceneManagerWindow.Selection.Register(element.Q(className: "Collections-template-header"), collection, selected => SceneManagerWindow.Selection.SetBackgroundColor(element.Q(className: "Collections-template-header"), selected));

            ReloadStar();
            void ReloadStar() =>
                element.Q("Collection-template-header-Star").EnableInClassList("hidden", collection.startupOption == CollectionStartupOption.DoNotOpen);

            var content = element.Q("Collections-template-content");

            expander.RegisterValueChangedCallback(b => OnChecked());
            OnChecked();

            ExtraButtons(collection, element.Q(name: "Extra-Buttons"));
            element.SetLocked(AssetDatabase.GetAssetPath(collection));

            SetupDragDropAddItem(element, content, collection);

            void OnChecked()
            {

                expander.text = expander.value ? "▼" : "►";
                IsExpanded(collection, expander.value);
                content.EnableInClassList("hidden", !expander.value);

                if (expander.value)
                {
                    CreateSceneItems(collection, content);
                    DragAndDropReorder.RegisterList(content, dragButtonName: "scene-drag-button", itemRootName: "scene-drag-root");
                }
                else
                {
                    ClearSceneItems(content);
                    DragAndDropReorder.UnregisterList(content);
                }

            }

        }

        static void SetupDragDropAddItem(VisualElement element, VisualElement parent, SceneCollection collection)
        {

            var dropElement = new VisualElement();
            dropElement.style.backgroundColor = new StyleColor(new Color32(0, 122, 163, 255));
            dropElement.style.height = 22;
            dropElement.style.marginBottom = 10;
            dropElement.EnableInClassList("hidden", true);
            dropElement.style.paddingTop = 3;
            dropElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            dropElement.style.alignContent = Align.Center;

            var l = new Label("+");

            dropElement.Add(l);

            element.Add(dropElement);

            var scenes = new List<Scene>();

            element.RegisterCallback<DragEnterEvent>(e =>
            {

                dropElement.EnableInClassList("hidden", false);
                parent.style.marginBottom = 0;

                scenes.Clear();
                scenes.AddRange(DragAndDrop.objectReferences.OfType<Scene>());
                scenes.AddRange(DragAndDrop.objectReferences.OfType<SceneAsset>().Select(s => s.FindASMScene()));
                scenes.AddRange(DragAndDrop.paths.Select(AssetDatabase.LoadAssetAtPath<SceneAsset>).OfType<SceneAsset>().Select(s => s.FindASMScene()));

                scenes = scenes.GroupBy(s => s.path).Select(g => g.First()).ToList();

                if (scenes.Any())
                {
                    DragAndDrop.AcceptDrag();
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                }

            });

            SceneManagerWindow.window.rootVisualElement.RegisterCallback<DragPerformEvent>(e =>
            {
                Cancel();
            });

            element.RegisterCallback<DragLeaveEvent>(e =>
            {
                Cancel();
            });

            dropElement.RegisterCallback<DragUpdatedEvent>(e =>
            {
                DragAndDrop.AcceptDrag();
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            });

            dropElement.RegisterCallback<DragPerformEvent>(e =>
            {
                foreach (var scene in scenes)
                    CreateNewScene(collection, scene);
                Cancel();
            });

            void Cancel()
            {
                scenes.Clear();
                parent.style.marginBottom = 10;
                dropElement.EnableInClassList("hidden", true);
            }

            bool hadDragAndDrop = false;
            OnGUIEvent -= ScenesTab_OnGUIEvent;
            OnGUIEvent += ScenesTab_OnGUIEvent;

            void ScenesTab_OnGUIEvent()
            {
                var isDragAndDrop = DragAndDrop.objectReferences.Any();
                if (hadDragAndDrop && !isDragAndDrop)
                    Cancel();
                hadDragAndDrop = isDragAndDrop;
            }

        }

        static void CreateSceneItems(SceneCollection collection, VisualElement scenesList)
        {
            var scenes = collection.scenes;
            for (int i = 0; i < scenes.Length; i++)
                CreateItem(collection, scenes[i], i, scenesList, SceneManagerWindow.window);
        }

        static void ClearSceneItems(VisualElement scenesList) =>
            scenesList?.Clear();

        internal static VisualElement CreateItem(SceneCollection collection, Scene scene, int index, VisualElement scenesList, IUIToolkitEditor parent, string label = "")
        {

            if (scenesList == null)
                return null;

            AddListener(scene);

            var resource = Resources.Load<VisualTreeAsset>(sceneTemplate);
            if (!resource)
                return null;
            var element = resource.CloneTree();
            scenesList.Add(element);

            //Used on reorder to get assets
            element.Q(name: "scene-drag-root").userData = (collection, scene);

            var sceneField = element.Q<SceneField>("sceneField");

            sceneField.EnableInClassList("hidden", false);

            sceneField.label = label;
            sceneField.disableScene = !collection;
            sceneField.SetValueWithoutNotify(scene).RegisterValueChangedCallback(e =>
            {
                var scenes = collection.scenes;
                scenes[index] = e.newValue;
                collection.scenes = scenes;
                Save(collection);
                SetTag(element, parent, e.newValue, collection);
                ReloadNewButton(e.newValue);
            });

            SetTag(element, parent, scene, collection);

            element.Q<Button>("Scene-template-header-Remove").clicked += () => RemoveScene(collection, index);
            element.Q<Button>(className: "NewScene").clicked += () => CreateNewScene(collection, index);

            ExtraButtons(scene, element.Q(name: "Extra-Buttons"));

            SceneManagerWindow.Selection.Register(element, scene);

            ReloadNewButton(scene);
            void ReloadNewButton(Scene s) =>
                element.Q<Button>(className: "NewScene").EnableInClassList("hidden", s);

            element.SetLocked(AssetDatabase.GetAssetPath(collection));

            return element;

        }

        static void SetTag(VisualElement element, IUIToolkitEditor parent, Scene scene, SceneCollection collection)
        {

            if (element.Q<Button>(className: "LayerDropDown") is Button dropdown)
            {

                dropdown.EnableInClassList("hidden", !scene);
                if (!scene)
                    return;

                void UpdateMenu()
                {

                    var current = scene.tag;
                    element.Q<VisualElement>(className: "Scene_ColorIndicator").style.backgroundColor = current.color;
                    element.Q<Label>(className: "Scene-letter").text = current.label;
                    dropdown.text = current.name;

                }

                dropdown.TrimLabel(scene.tag.name, () => dropdown.resolvedStyle.width - 12, enableAuto: true);

                dropdown.visible = scene;
                dropdown.clicked += () =>
                PickTagPopup.Open(dropdown, parent, alignRight: true).
                Refresh(scene.tag, onSelected: layer =>
                {

                    if (Profile.current)
                        Profile.current.Tag(scene, setTo: layer);

                    UpdateMenu();

                });

                UpdateMenu();

            }

        }

        #region Extra buttons

        internal delegate VisualElement ExtraCollectionButton(SceneCollection collection);
        internal delegate VisualElement ExtraSceneButton(Scene scene);

        static readonly Dictionary<ExtraCollectionButton, (int? position, bool isLockable)> extraCollectionButtons = new Dictionary<ExtraCollectionButton, (int? position, bool isLockable)>();
        static readonly Dictionary<ExtraSceneButton, (int? position, bool isLockable)> extraSceneButtons = new Dictionary<ExtraSceneButton, (int? position, bool isLockable)>();
        internal static void AddExtraButton(ExtraCollectionButton callback, int? position = null, bool isLockable = true)
        {
            if (!extraCollectionButtons.ContainsKey(callback))
                extraCollectionButtons.Add(callback, (position, isLockable));
        }

        internal static void RemoveExtraButton(ExtraCollectionButton callback) =>
            extraCollectionButtons.Remove(callback);

        internal static void AddExtraButton(ExtraSceneButton callback, int? position = null, bool isLockable = true)
        {
            if (!extraSceneButtons.ContainsKey(callback))
                extraSceneButtons.Add(callback, (position, isLockable));
        }

        internal static void RemoveExtraButton(ExtraSceneButton callback) =>
            extraSceneButtons.Remove(callback);

        static void ExtraButtons(SceneCollection collection, VisualElement panel)
        {
            foreach (var callback in extraCollectionButtons.OrderBy(e => e.Value.position).ToArray())
                if (callback.Key?.Invoke(collection) is VisualElement element)
                {
                    panel.Add(element);
                    element.EnableInClassList("lockable", callback.Value.isLockable);
                }
        }

        static void ExtraButtons(Scene scene, VisualElement panel)
        {
            foreach (var callback in extraSceneButtons.OrderBy(e => e.Value.position).ToArray())
                if (callback.Key?.Invoke(scene) is VisualElement element)
                {
                    panel.Add(element);
                    element.EnableInClassList("lockable", callback.Value.isLockable);
                }
        }

        #endregion

        #endregion
        #region Asset management

        static void CreateNewCollection() =>
            SceneCollectionUtility.Create("New Collection");

        static void CreateNewScene(SceneCollection collection, int index)
        {

            SceneUtility.Create(onCreated: null, collection, index, replaceIndex: true, save: false);

            Save(collection);
            PopulateList();

            //CoroutineUtility.Run(SceneManager.assetManagement.ReloadAssets, after: 0.1f);

        }

        static void RemoveCollection(SceneCollection collection) =>
            SceneCollectionUtility.Remove(collection);

        static void CreateNewScene(SceneCollection collection, Scene scene = null)
        {
            var scenes = collection.scenes;
            ArrayUtility.Add(ref scenes, scene);
            collection.scenes = scenes;
            Save(collection);
            IsExpanded(collection, true);
            PopulateList();
        }

        static void RemoveScene(SceneCollection collection, int index)
        {
            var scenes = collection.scenes;
            ArrayUtility.RemoveAt(ref scenes, index);
            collection.scenes = scenes;
            Save(collection);
            PopulateList();
        }

        #endregion
        #region Open

        static void Play(SceneCollection collection) =>
            SceneManager.runtime.Start(collection, ignoreDoNotOpen: Event.current?.shift ?? false, playSplashScreen: false);

        static void Open(SceneCollection collection, bool additive = false)
        {
            if (Application.isPlaying)
            {
                if (collection.IsOpen())
                    collection.Close();
                else
                    collection.Open();
            }
            else if (!additive)
                SceneManager.editor.Open(collection, ignoreTags: Event.current?.shift ?? false);
            else if (SceneManager.editor.IsOpen(collection))
            {
                if (SceneManager.editor.CanClose(collection))
                    SceneManager.editor.Close(collection);
            }
            else
                SceneManager.editor.Open(collection, additive: true, ignoreTags: Event.current?.shift ?? false);

        }

        static bool IsOpen(SceneCollection collection) =>
            !Application.isPlaying && SceneManager.editor.IsOpen(collection);

        #endregion

    }

}
