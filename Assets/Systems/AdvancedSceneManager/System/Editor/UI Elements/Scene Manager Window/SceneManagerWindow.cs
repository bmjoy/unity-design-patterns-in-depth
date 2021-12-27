#pragma warning disable IDE0051 // Remove unused private members

using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.GenericPopup;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AdvancedSceneManager.Editor
{

    [InitializeOnLoad]
    public partial class SceneManagerWindow : EditorWindow_UIElements<SceneManagerWindow>, IUIToolkitEditor
    {

        public enum Tab
        {
            Scenes, Tags, Settings
        }

        readonly Dictionary<Tab, (Type type, string path)> tabs = new Dictionary<Tab, (Type type, string path)>()
        {
            {Tab.Scenes,   (typeof(ScenesTab),   $"AdvancedSceneManager/Tabs/{Tab.Scenes}/Tab") },
            {Tab.Tags,     (typeof(TagsTab),     $"AdvancedSceneManager/Tabs/{Tab.Tags}/Tab") },
            {Tab.Settings, (typeof(SettingsTab), $"AdvancedSceneManager/Tabs/{Tab.Settings}/Tab") },
        };

        [SerializeField] private Tab tab;
        [SerializeField] internal SerializableStringBoolDict openCollectionExpanders = new SerializableStringBoolDict();
        [SerializeField] internal SerializableStringBoolDict openTagExpanders = new SerializableStringBoolDict();
        public bool autoOpenScenesWhenCreated = true;

        public static event Action OnGUIEvent;
        public static event Action MouseUp;

        public static new Rect position;

        static void OnMouseUp(MouseUpEvent e) =>
            MouseUp?.Invoke();

        public static bool IsDarkMode =>
            EditorGUIUtility.isProSkin;

        static bool ignoreProfileChanged;
        public static void IgnoreProfileChanged() =>
            ignoreProfileChanged = true;

        private void OnGUI()
        {

            wantsMouseMove = true;

            if (!isMainContentLoaded)
                return;

            //Tab logic is contained within static classes, some of them needs access to OnGUI, to check input, for example
            OnGUIEvent?.Invoke();

            //Register handler for upper right menu button
            var menu = rootVisualElement.Q<ToolbarToggle>("button-menu");
            menu.UnregisterValueChangedCallback(ShowMenu);
            menu.RegisterValueChangedCallback(ShowMenu);

            void ShowMenu(ChangeEvent<bool> e) =>
                this.ShowMenu();

            if (!InternalEditorUtility.isApplicationActive)
                return;

            position = ((EditorWindow)window).position;

            SetContentSize();

            //Invoke OnGUI() on tab
            InvokeTab();
            SetContentSize();

            if (window && window.rootVisualElement?.Q("footer-right") is VisualElement footer)
                footer.SetEnabled(Profile.current);

            //Repaint constantly if drag and drop operation is ongoing
            if (DragAndDropReorder.rootVisualElement != null)
                Repaint();

        }

        #region Constructor and overrides

        const string windowPath = "AdvancedSceneManager/SceneManagerWindow";

        public override string path => windowPath;
        public override bool autoReloadOnWindowFocus => false;

        [MenuItem("File/Scene Manager... %#m", priority = 205)]
        static void MenuItem()
        {
            Open();
            //window.title = new GUIContent("Scene Manager");
        }

        static SceneManagerWindow()
        {

            DragAndDropReorder.OnDragStarted += OnDragStarted;
            DragAndDropReorder.OnDragEnded += OnDragEnded;
            DragAndDropReorder.OnDragCancel += OnDragCancel;
            Selection.OnSelectionChanged += OnSelectionChanged;

            EditorApplication.projectChanged += Reload;
            Profile.onProfileChanged += () =>
            {
                if (ignoreProfileChanged)
                    return;
                ignoreProfileChanged = false;
                Reload();
            };

            AssetsSavedUtility.onAssetsSaved += OnAssetsSave;
            SceneManager.assetManagement.AssetsChanged += () =>
            {
                if (!Application.isPlaying)
                    Reload();
            };

        }

        #endregion
        #region Reload / OnEnable, OnDisable

        public override void OnEnable()
        {

            base.OnEnable();

            //Load variables from editor prefs
            var json = EditorPrefs.GetString("AdvancedSceneManager.SceneManagerWindow", JsonUtility.ToJson(this));
            JsonUtility.FromJsonOverwrite(json, this);

            LoadDefaultStyle();

            DragAndDropReorder.rootVisualElement = rootVisualElement;

            Reload();

            //Invoke OnEnable on current tab
            InvokeTab();

            minSize = new Vector2(460, 250);

            rootVisualElement?.Q(className: "review")?.Q<Label>(className: "Link-To-Settings")?.
                RegisterCallback<MouseDownEvent>(e =>
                Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/advanced-scene-manager-174152#reviews"));

            RefreshReviewPrompt();

        }

        private void OnDisable()
        {

            //Invoke OnDisable() on current tab
            InvokeTab();

            //Save variables to editor prefs
            var json = JsonUtility.ToJson(this);
            EditorPrefs.SetString("AdvancedSceneManager.SceneManagerWindow", json);

        }

        void LoadDefaultStyle()
        {

            var light = Resources.Load<StyleSheet>("AdvancedSceneManager/Default-Light");
            var dark = Resources.Load<StyleSheet>("AdvancedSceneManager/Default-Dark");

            rootVisualElement.styleSheets.Remove(light);
            rootVisualElement.styleSheets.Remove(dark);

            if (IsDarkMode)
                rootVisualElement.styleSheets.Add(dark);
            else
                rootVisualElement.styleSheets.Add(light);

        }

        [DidReloadScripts]
        internal static void Reload()
        {

            if (window is SceneManagerWindow w && w && !BuildPipeline.isBuildingPlayer)
            {

                w.rootVisualElement?.UnregisterCallback<MouseUpEvent>(OnMouseUp);

                if (w.rootVisualElement == null)
                    return;

                w.ReloadContent();
                ReopenTab();
                w.SetContentSize();

                if (w.rootVisualElement.Q<Button>("PlayButton") is Button playButton)
                {
                    playButton.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));
                    playButton.clicked -= OnPlayButton;
                    playButton.clicked += OnPlayButton;
                }

                if (w.rootVisualElement.Q<Button>("button-reload") is Button reloadButton)
                {
                    reloadButton.clicked -= w.Refresh;
                    reloadButton.clicked += w.Refresh;
                    reloadButton.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));
                }

                w.rootVisualElement.RegisterCallback<MouseUpEvent>(OnMouseUp);

                if (w.rootVisualElement.Q("Link-To-Settings-Button") is VisualElement link)
                {
                    link.UnregisterCallback<MouseDownEvent>(OnClick, TrickleDown.TrickleDown);
                    link.RegisterCallback<MouseDownEvent>(OnClick, TrickleDown.TrickleDown);
                }

                if (w.rootVisualElement.Q<ToolbarToggle>("button-menu") is ToolbarToggle menuButton)
                    menuButton.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));

                void OnClick(object e) =>
                    w.SetTab(Tab.Settings);

                OnProfileChanged(Profile.current);

                w.RefreshReviewPrompt();

            }

            void OnPlayButton()
            {
                Coroutine().StartCoroutine();
                IEnumerator Coroutine()
                {
                    SceneManager.runtime.Start();
                    yield return null;
                    Reload();
                }
            }

        }

        [PostProcessBuild]
        static void PostBuild(BuildTarget _, string _1)
        {
            if (window is SceneManagerWindow w && w)
                w.Refresh();
        }

        public class PostProcess : AssetPostprocessor
        {

            static void OnPostprocessAllAssets(string[] _, string[] _1, string[] _2, string[] _3)
            {
                Reload();
            }

        }

        #endregion
        #region Warning / Review prompt

        private static readonly Color NoProfileColor = new Color32(255, 173, 51, 255);
        private static readonly Color TutorialColor = new Color32(135, 206, 250, 255);
        private static readonly string NoProfileText = "No profile, changes will not be saved.";

        void HideWarning()
        {
            SetWarning(Color.clear, "");
            rootVisualElement?.Q("No-Profile-Warning")?.EnableInClassList("hidden", true);
        }

        void SetWarning(Color color, string text)
        {

            if (rootVisualElement == null)
                return;

            if (rootVisualElement.Q("content") is VisualElement content)
            {
                content.style.borderLeftWidth = content.style.borderRightWidth = content.style.borderBottomWidth = 1;
                content.style.borderLeftColor = content.style.borderRightColor = content.style.borderBottomColor = color;
                if (FooterButtons().Any())
                    content.style.borderBottomWidth = 0;
                else
                {
                    content.style.borderBottomWidth = 1;
                    content.style.borderBottomColor = color;
                }
            }

            if (rootVisualElement.Q("header") is VisualElement header)
            {
                header.style.borderLeftWidth = header.style.borderRightWidth = 1;
                header.style.borderLeftColor = header.style.borderRightColor = color;
            }

            rootVisualElement.Query("No-Profile-Warning").ForEach(warning =>
            {
                warning.style.backgroundColor = color;
                warning.EnableInClassList("hidden", false);
            });

            if (rootVisualElement.Q<Label>("No-Profile-Warning-Label") is Label label)
                label.text = text;

            if (rootVisualElement.Q("footer") is VisualElement footer)
            {

                footer.style.borderLeftWidth = footer.style.borderRightWidth = footer.style.borderBottomWidth = 1;

                if (FooterButtons().Any())
                    footer.style.borderLeftColor = footer.style.borderRightColor = footer.style.borderBottomColor = color;

            }
        }

        public static void OnProfileChanged(Profile profile)
        {

            if (window == null)
                return;

            if (!profile)
                window.SetWarning(NoProfileColor, NoProfileText);
            else
                window.HideWarning();

        }

        void OnReviewClose()
        {
            PlayerPrefs.SetInt("AdvancedSceneManager.HideReviewPrompt", 1);
            RefreshReviewPrompt();
        }

        void RefreshReviewPrompt()
        {
            if (rootVisualElement.Q<Button>("closeReviewPrompt") is Button button)
            {
                button.clicked -= OnReviewClose;
                button.clicked += OnReviewClose;
            }
            rootVisualElement?.Q(className: "review")?.EnableInClassList("hidden", PlayerPrefs.GetInt("AdvancedSceneManager.HideReviewPrompt") == 1);
        }

        #endregion
        #region Upper right menu

        void ShowMenu()
        {

            if (rootVisualElement == null)
                return;

            GenericPopup.Open(rootVisualElement.Q("button-menu"), this, alignRight: true, offset: new Vector2(0, -6)).
                Refresh(
                    Item.Create("Scene Overview...", SceneOverviewWindow.Open),
                    Item.Create("Plugins and samples...", () => { UnityEditor.PackageManager.UI.Window.Open("plugin.asm.package-manager"); UnityEditor.PackageManager.UI.Window.Open("plugin.asm.package-manager"); }),
                    Item.Separator,
                    Item.Create("Look at documentation...", OpenDocumentation),
                    Item.Create("Look at lazy.solutions...", OpenLazy),
                    Item.Separator,
                    Item.Create("Build temp (profiler)", () => Build(profiler: true)),
                    Item.Create("Build temp", () => Build(profiler: false)),
                    Item.Separator,
                    Item.Create("Reset", ResetSceneManager)
                );

        }

        void OpenDocumentation() => Process.Start("https://github.com/Zumwani/advanced-scene-manager/wiki");
        void OpenLazy() => Process.Start("http://lazy.solutions/");
        void ResetSceneManager()
        {
            if (EditorUtility.DisplayDialog("Resetting Advanced Scene Manager...", "This will reset all changes made in Advanced Scene Manager and cannot be undone. Are you sure you wish to continue?'", "Reset", "Cancel"))
                SceneManager.assetManagement.Clear();
        }

        void BakeLightmaps()
        {

            var scenes = Selection.scenes(includeCollections: true).Select(s => s.path).ToArray();

            if (scenes.Count() < 2)
                return;

            var displayNames = scenes.Select(s => s.Replace("Assets/", "").Replace(".unity", "")).ToArray();
            if (GenericPrompt.Prompt(title: "Baking lightmap for multiple scenes...", message: "Are you sure you wish to multi-bake lightmap for the following scenes?" + Environment.NewLine +
                string.Join(Environment.NewLine, displayNames) + Environment.NewLine + Environment.NewLine))
                Lightmapping.BakeMultipleScenes(scenes);

            Reload();

        }

        void CombineScenes()
        {
            SceneUtility.MergeScenes(Selection.scenes(includeCollections: false).Select(s => s.path).ToArray());
        }

        void Refresh()
        {

            AssetRefreshUtility.Refresh();
            BuildSettingsUtility.UpdateBuildSettings();

            Reload();

        }

        /// <summary>Enables message when temp build is deleted.</summary>
        public static bool deleteTempBuildMessage
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.Warnings.deleteTempBuild");
            set => EditorPrefs.SetBool("AdvancedSceneManager.Warnings.deleteTempBuild", value);
        }

        void Build(bool profiler)
        {

            if (BuildPipeline.isBuildingPlayer)
                return;

            var options = BuildOptions.AutoRunPlayer | BuildOptions.Development;
            if (profiler) options |= BuildOptions.ConnectWithProfiler;

            var path = Path.Combine(Path.GetTempPath(), "Advanced Scene Manager", Application.companyName + "+" + Application.productName, "build.exe");
            var folder = Directory.GetParent(path).FullName;
            if (Directory.Exists(folder))
            {
                try
                {
                    Directory.Delete(folder, recursive: true);
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.LogError("Game is still running.");
                    return;
                }
            }

#if UNITY_2019
            //Display progress bar early since it takes a while for unity to show theirs...
            EditorUtility.DisplayProgressBar("Building Player", "Running prebuild events", 0);

#elif UNITY_2020 || UNITY_2020_OR_NEWER

            var progress = Progress.Start("Waiting for temp build to exit", options: Progress.Options.Indefinite);
            EditorApplication.update += Update;

            //Stop unity from displaying '(not responding)' message next to progress bar
            void Update() =>
                Progress.Report(progress, 0, 1);

#endif

            BuildSettingsUtility.UpdateBuildSettings();
            BuildPipeline.BuildPlayer(new BuildPlayerOptions()
            {
                target = EditorUserBuildSettings.activeBuildTarget,
                options = options,
                scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
                locationPathName = path,
            });

            var showWarning = deleteTempBuildMessage;
            DeleteFolderOnProcessClose();
            void DeleteFolderOnProcessClose()
            {

                Coroutine().StartCoroutine(OnComplete);
                void OnComplete()
                {
#if UNITY_2019
                    EditorUtility.ClearProgressBar();
#elif UNITY_2020 || UNITY_2020_OR_NEWER
                    EditorApplication.update -= Update;
                    Progress.Remove(progress);
#endif
                }

                IEnumerator Coroutine()
                {
                    var dir = Directory.GetParent(path);
                    while (dir.Exists)
                    {

                        dir.Refresh();

                        var sdo = Path.GetFileNameWithoutExtension(path);
                        var processes = Process.GetProcessesByName(sdo);

                        if (processes.FirstOrDefault(p => p.MainModule.FileName == path) is Process process)
                        {

                            while (!process.HasExited)
                                yield return null;

                            try
                            {
                                if (dir.Exists)
                                {
                                    dir.Delete(true);
                                    if (showWarning)
                                        Debug.Log("Temp build deleted");
                                }
                            }
                            catch (Exception)
                            { }

                            yield break;

                        }

                        yield return new WaitForSeconds(0.5f);

                    }

                }

            }

        }

        #endregion
        #region Drag and drop reorder

        /// <summary>An class that manages drag and drop reorder.</summary>
        public static class DragAndDropReorder
        {

            static DragAndDropReorder()
            {
                OnGUIEvent += OnGUI;
                MouseUp += StopDrag;
            }

            public static VisualElement rootVisualElement { get; set; }

            public static event Action<DragElement> OnDragStarted;
            public static event Action<DragElement, int> OnDragEnded;
            public static event Action<DragElement> OnDragCancel;

            public static DragElement currentDragElement { get; private set; }
            public static int newIndex { get; private set; } = -1;
            public static float offset { get; private set; }

            public class DragElement
            {
                public VisualElement list;
                public VisualElement item;
                public VisualElement button;
                public EventCallback<MouseDownEvent> mouseDown;
                public EventCallback<MouseUpEvent> mouseUp;
                public EventCallback<MouseMoveEvent> mouseMove;
                public int index;
                public string itemRootName;
                public string itemRootClass;
            }

            /// <summary>
            /// <para>The lists that has drag elements.</para>
            /// <para>Key1: A list that can be reordered.</para>
            /// <para>Key2: The child element of list (Key1) that can be dragged.</para>
            /// <para>Value: <see cref="DragElement"/>, the logical object that represents an element that can be dragged.</para>
            /// </summary>
            static readonly
                Dictionary<VisualElement, Dictionary<VisualElement, DragElement>> lists = new
                Dictionary<VisualElement, Dictionary<VisualElement, DragElement>>();

            #region Registration

            public static void RegisterList(VisualElement list, string dragButtonName = null, string dragButtonClass = null, string itemRootName = null, string itemRootClass = null)
            {

                if (string.IsNullOrWhiteSpace(dragButtonName) && string.IsNullOrWhiteSpace(dragButtonClass))
                    throw new ArgumentException($"Either {nameof(dragButtonName)} or {nameof(dragButtonClass)} must be set!");

                if (string.IsNullOrWhiteSpace(itemRootName) && string.IsNullOrWhiteSpace(itemRootClass))
                    throw new ArgumentException($"Either {nameof(itemRootName)} or {nameof(itemRootClass)} must be set!");

                var dragElements = lists.Set(list, lists.GetValue(list) ?? new Dictionary<VisualElement, DragElement>());

                var i = -1;
                list.Query(className: itemRootClass, name: itemRootName).ForEach(item =>
                {

                    //Get or create logical drag element object
                    var dragElement = dragElements.Set(item, dragElements.GetValue(item) ?? new DragElement());

                    //Find drag button
                    var button = item.Q(name: dragButtonName, className: dragButtonClass);

                    var mouseDown = new EventCallback<MouseDownEvent>((MouseDownEvent e) =>
                    {
                        if (e.button == 0 && e.modifiers == EventModifiers.None)
                            StartDrag(dragElement, e);
                    });

                    //Unregister old callback, if we already registered this element before
                    button.UnregisterCallback(dragElement.mouseDown);
                    button.RegisterCallback(mouseDown);


                    dragElement.button = button;
                    dragElement.mouseDown = mouseDown;
                    dragElement.list = list;
                    dragElement.item = item;
                    dragElement.index = i += 1;
                    dragElement.itemRootClass = itemRootClass;
                    dragElement.itemRootName = itemRootName;

                });

            }

            public static void UnregisterList(VisualElement list)
            {

                if (!lists.ContainsKey(list))
                    return;

                var items = lists.GetValue(list).Values;

                foreach (var item in lists.GetValue(list).Values)
                    item.button.UnregisterCallback(item.mouseDown);

                lists.Remove(list);

            }

            public static void UnregisterListAll()
            {
                foreach (var list in lists.ToArray())
                    UnregisterList(list.Key);
            }

            #endregion

            static void StartDrag(DragElement element, MouseDownEvent e)
            {
                if (currentDragElement == null && CanDrag(element))
                {
                    OnDragStarted?.Invoke(element);
                    offset = e.localMousePosition.y;
                    currentDragElement = element;
                }
            }

            static void StopDrag()
            {

                if (currentDragElement != null)
                {

                    if (_isOutsideOfDeadzone)
                    {

                        CleanUp(currentDragElement);

                        if (currentDragElement.index == newIndex)
                            OnDragCancel?.Invoke(currentDragElement);
                        else
                            OnDragEnded?.Invoke(currentDragElement, newIndex);

                    }

                    isUp = false;
                    offset = 0;
                    currentDragElement = null;
                    newIndex = -1;
                    mouseDownPos = null;
                    _isOutsideOfDeadzone = false;

                }
            }

            static VisualElement currentDropZone;
            static VisualElement CreateDropZone(float height)
            {

                currentDropZone?.RemoveFromHierarchy();

                currentDropZone = new VisualElement();
                currentDropZone.style.height = height;
                currentDropZone.style.backgroundColor = Color.gray;

                return currentDropZone;

            }

            static bool CanDrag(DragElement element) =>
                element.list.childCount > 1;

            static bool isUp;
            static VisualElement overlay;
            static void Setup(DragElement element)
            {

                element.list.Children().ElementAt(element.index).RemoveFromHierarchy();
                rootVisualElement.Add(element.item);

                element.item.style.position = Position.Absolute;
                element.item.style.top = Event.current.mousePosition.y - offset - 3;

                foreach (var style in element.list.GetStyles())
                    element.item.styleSheets.Add(style);

                element.item.style.backgroundColor = VisualElementExtensions.DefaultBackgroundColor;
                element.item.style.width = element.list.resolvedStyle.width;
                element.item.style.marginLeft = element.item.style.marginRight = element.list.worldBound.xMin;
                element.item.style.height = element.list.Children().Min(e => e.resolvedStyle.height);

                element.item.Query("draggable-hidden").ForEach(e => e.EnableInClassList("hidden", true));

                overlay?.RemoveFromHierarchy();
                overlay = new VisualElement() { name = "block-input-overlay" };
                rootVisualElement.Add(overlay);

            }

            static void CleanUp(DragElement element)
            {
                overlay?.RemoveFromHierarchy();
                foreach (var item in rootVisualElement.Children().ToArray())
                {
                    if (item.name == element.itemRootName || item.ClassListContains(element.itemRootClass))
                        item.RemoveFromHierarchy();
                }
            }

            static Vector2 prevMousePos;
            static Vector2? mouseDownPos;
            static bool _isOutsideOfDeadzone;
            static void OnGUI()
            {

                if (currentDragElement == null)
                    return;

                if (!mouseDownPos.HasValue)
                    mouseDownPos = Event.current.mousePosition;

                var isOutsideOfDeadZone = (Event.current.mousePosition - mouseDownPos.Value).magnitude > 2;
                if (isOutsideOfDeadZone && !_isOutsideOfDeadzone)
                {
                    Setup(currentDragElement);
                    _isOutsideOfDeadzone = isOutsideOfDeadZone;
                    return;
                }
                else if (!isOutsideOfDeadZone)
                    return;

                //Check if mouse is moving up or down, since we want different behavior for drop zone depending on this
                var delta = Event.current.mousePosition - prevMousePos;
                if (delta.y != 0)
                    isUp = delta.y < 0;

                //Move element
                var element = currentDragElement.item;
                element.style.position = Position.Absolute;
                element.style.top = Event.current.mousePosition.y - offset - 3;


                //Ensure that we stay inside bounds of list
                var yMin = currentDragElement.list.worldBound.yMin;
                var yMax = currentDragElement.list.worldBound.yMax;

                if (element.style.top.value.value < yMin - element.resolvedStyle.height) element.style.top = yMin - element.resolvedStyle.height;
                if (element.style.top.value.value > yMax) element.style.top = yMax;


                //Get index under mouse position
                var elements = currentDragElement.list.Query(className: currentDragElement.itemRootClass, name: currentDragElement.itemRootName).ToList();
                var index = isUp
                    ? elements.FindIndex(e => element.resolvedStyle.top + element.resolvedStyle.height < e.worldBound.center.y)
                    : elements.FindIndex(e => element.resolvedStyle.top + (element.resolvedStyle.height * 2) < e.worldBound.center.y);

                if (index == -1)
                    index = elements.Count;

                if (index != newIndex)
                {

                    //Put element where item would go, as a preview
                    var dropZone = CreateDropZone(currentDragElement.item.style.height.value.value);
                    if (index < elements.Count && index >= 0)
                        currentDragElement.list.Insert(index, dropZone);
                    else
                        currentDragElement.list.Add(dropZone);

                }

                newIndex = index;
                prevMousePos = Event.current.mousePosition;

            }

        }

        static void OnDragStarted(DragAndDropReorder.DragElement element)
        {
            OnReorderStart(element);
        }

        static void OnDragEnded(DragAndDropReorder.DragElement element, int newIndex)
        {
            OnReorderEnd(element, newIndex);
            OnDragCancel(element);
        }

        static void OnDragCancel(DragAndDropReorder.DragElement element)
        {
            Reload();
        }

        #endregion
        #region Selection

        /// <summary>A class that manages selection.</summary>
        public static class Selection
        {

            /// <summary>Occurs when the selection changes.</summary>
            public static event Action OnSelectionChanged;

            /// <summary>The objects that are selected.</summary>
            public static Object[] objects = Array.Empty<Object>();

            /// <summary>Gets all selected objects of type <see cref="Scene"/>.</summary>
            /// <param name="includeCollections">If true, then scenes in collections will be returned as well, as a flattened list.</param>
            public static IEnumerable<Scene> scenes(bool includeCollections) =>
                !includeCollections
                    ? objects.OfType<Scene>().Where(s => s).Distinct()
                    : objects.SelectMany(o =>
                    {

                        if (o is SceneCollection collection)
                            return collection.scenes;
                        else if (o is Scene scene)
                            return new[] { scene };

                        return Array.Empty<Scene>();

                    }).
                    Distinct();

            static readonly StyleColor unselectedColor = new StyleColor(StyleKeyword.Null);
            static readonly StyleColor selectedColor = new StyleColor(new Color(0.3686f, 0.5058f, 0.6274f));

            static bool isSelected(Object o) =>
                objects.Contains(o);

            static readonly Dictionary<VisualElement, (Object obj, Action<bool> setVisualIndicator)> selectables = new Dictionary<VisualElement, (Object obj, Action<bool> setVisualIndicator)>();

            /// <summary>Registers an element for selection.</summary>
            /// <param name="setVisualIndicator">By default, <paramref name="element"/> will have its background-color set. Override this if that does not work for the current element.</param>
            public static void Register(VisualElement element, Object obj, Action<bool> setVisualIndicator = null)
            {

                if (element == null)
                    return;

                selectables.Set(element, (obj, setVisualIndicator ?? (selected => SetBackgroundColor(element, selected))));
                selectables.GetValue(element).setVisualIndicator.Invoke(isSelected(obj));
                element.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                element.RegisterCallback<MouseDownEvent>(OnMouseDown);

            }

            public static void Unregister(VisualElement element)
            {

                var (obj, setVisualIndicator) = selectables.GetValue(element);
                setVisualIndicator?.Invoke(false);
                ArrayUtility.Remove(ref objects, obj);

                selectables.Remove(element);
                element.UnregisterCallback<MouseDownEvent>(OnMouseDown);

            }

            static void OnMouseDown(MouseDownEvent e)
            {
                if (e.button == 0 && e.modifiers == EventModifiers.Control)
                    if (e.currentTarget is VisualElement element)
                    {

                        var (obj, setVisualIndicator) = selectables.GetValue(element);
                        var shouldSelect = !objects.Contains(obj);

                        if (shouldSelect)
                            ArrayUtility.Add(ref objects, obj);
                        else
                            ArrayUtility.Remove(ref objects, obj);

                        setVisualIndicator?.Invoke(shouldSelect);
                        OnSelectionChanged?.Invoke();

                        e.StopImmediatePropagation();

                    }
            }

            public static void SetBackgroundColor(VisualElement element, bool selected)
            {
                if (element == null)
                    return;
                element.style.backgroundColor = selected ? selectedColor : unselectedColor;
            }

            public static void Reset()
            {
                foreach (var selectable in selectables.Keys.ToArray())
                    Unregister(selectable);
            }

        }

        static void OnSelectionChanged()
        {

            var w = window;

            if (!w)
                return;

            if (w.rootVisualElement.Q("bakeLightMapsButton") is Button bakeButton)
            {
                bakeButton.clicked -= w.BakeLightmaps;
                bakeButton.clicked += w.BakeLightmaps;
                bakeButton.EnableInClassList("hidden", Selection.scenes(includeCollections: true).Count() < 2);
            }

            if (w.rootVisualElement.Q("combineScenesButton") is Button combineButton)
            {
                combineButton.clicked -= w.CombineScenes;
                combineButton.clicked += w.CombineScenes;
                combineButton.EnableInClassList("hidden", Selection.scenes(includeCollections: false).Count() < 2);
            }

            if (w.rootVisualElement.Q("selectionText") is Label text)
            {
                text.text = Selection.objects.Any() ? ("(" + Selection.objects.Length + " items selected.)") : "(CTRL + Click, to select items)";
                text.EnableInClassList("hidden", w.tab != Tab.Scenes);
            }

        }

        #endregion
        #region Tabs

        private void OnLostFocus()
        {
            //Invoke OnLostFocus() on current tab
            InvokeTab();
        }

        private (string title, Action action)[] FooterButtons() =>
            ((string title, Action action)[])InvokeTab() ?? Array.Empty<(string title, Action action)>();

        static void OnReorderStart(DragAndDropReorder.DragElement element) => InvokeTab(nameof(OnReorderStart), element);
        static void OnReorderEnd(DragAndDropReorder.DragElement element, int newIndex) => InvokeTab(nameof(OnReorderEnd), element, newIndex);

        public static void ReopenTab()
        {
            if (window is SceneManagerWindow w && w)
                w.SetTab(w.tab);
        }

        /// <summary>Set tab as active.</summary>
        void SetTab(Tab tab)
        {

            if (BuildPipeline.isBuildingPlayer)
                return;

            Selection.Reset();
            DragAndDropReorder.UnregisterListAll();
            var tabHeader = rootVisualElement.Q<VisualElement>("tabs");
            if (tabHeader == null)
                return;

            tabHeader.Clear();

            //We need to manually reset the other tabs, so let's just loop through them all
            foreach (var t in Enum.GetValues(typeof(Tab)))
            {

                var enabled = tab == (Tab)t;
                var tabButton = new ToolbarToggle();
                tabHeader.Add(tabButton);

                tabButton.AddToClassList("tab-button");
                tabButton.text = ObjectNames.NicifyVariableName(t.ToString());
                tabButton.SetValueWithoutNotify(enabled);

                if (enabled)
                    tabButton.AddToClassList("selected");

                tabButton.RegisterValueChangedCallback(e =>
                {
                    if (e.newValue)
                        SetTab((Tab)t);
                    else
                        tabButton.SetValueWithoutNotify(true);
                });

                rootVisualElement.Q(className: "review").EnableInClassList("hidden", tab != Tab.Settings);

            }

            //Set content to tab
            var content = rootVisualElement.Q<VisualElement>("tab-content");
            LoadContent(tabs[tab].path, content);

            //Disable existing tab and enable new
            InvokeTab(nameof(OnDisable));
            this.tab = tab;
            InvokeTab(nameof(OnEnable));

            var footer = rootVisualElement.Q("footer-right");
            footer.Clear();
            foreach (var button in FooterButtons())
            {
                var b = new Button() { text = button.title };
                b.AddToClassList("newButton");
                b.clicked += button.action;
                footer.Add(b);
            }

            footer = rootVisualElement.Q("footer");
            footer.EnableInClassList("hidden", !FooterButtons().Any());

            OnSelectionChanged();
            OnProfileChanged(Profile.current);
            RefreshReviewPrompt();

        }

        /// <summary>Set height of content area to fill available space.</summary>
        void SetContentSize()
        {

            if (rootVisualElement.Q<VisualElement>("content") is VisualElement content)
                content.style.height = new StyleLength(Screen.height);

        }

        /// <summary>
        /// <para>Invokes the static method on the current <see cref="Tab"/>.</para>
        /// <para>For example, when called from <see cref="OnFocus"/>, OnFocus() will be called on the current tab.</para>
        /// </summary>
        static object InvokeTab([CallerMemberName] string caller = "")
        {
            return window is SceneManagerWindow w && w
                ? InvokeTab(caller, w.rootVisualElement.Q<VisualElement>("tab-content"))
                : null;
        }

        static object InvokeTab(string name, params object[] param)
        {

            var w = window;
            if (!w)
                return null;

            var method = w.tabs[w.tab].type?.GetMethod(name);
            if (method == null)
                return null;

            if (method?.GetParameters()?.Select(p => p?.ParameterType)?.SequenceEqual((param ?? Array.Empty<object>())?.Select(p => p?.GetType())) ?? false)
                return method?.Invoke(null, param);
            else if (!method?.GetParameters()?.Any() ?? false)
                return method?.Invoke(null, null);

            return null;

        }

        #endregion
        #region Save

        public static void Save(ScriptableObject so = null)
        {

            if (!Profile.current)
                return;

            if (so == null)
                so = Profile.current;

            EditorUtility.SetDirty(so);

            if (focusedWindow == SceneOverviewWindow.window && SceneOverviewWindow.window is SceneOverviewWindow sceneOverview && sceneOverview)
                sceneOverview.titleContent = new GUIContent("Scene Overview*");
            else if (window is SceneManagerWindow w && w)
                w.titleContent = new GUIContent("Scene Manager*");

            BuildSettingsUtility.UpdateBuildSettings();

        }

        static void OnAssetsSave(string[] paths)
        {

            if (window is SceneManagerWindow w && w)
                w.titleContent = new GUIContent("Scene Manager");

            if (SceneOverviewWindow.window is SceneOverviewWindow w1 && w1)
                w1.titleContent = new GUIContent("Scene Overview");

            Reload();

        }

        #endregion

    }

}
