#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0011 // Add braces

using UnityEngine.UIElements;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using AdvancedSceneManager.Core;
using System.Linq;
using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>Provides an in-game toolbar that makes debugging scene management in build easier.</summary>
    /// <remarks>Only activates in editor and developer builds, and is disabled in non dev build.</remarks>
    public static class InGameToolbarUtility
    {

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            InitializeSettings();
            if (Application.isPlaying)
                Show();
        }
#else
        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            if (isEnabled && Debug.isDebugBuild)
                Show();
        }
#endif

        /// <inheritdoc cref="ASMSettings.inGameToolbarEnabled"/>
        public static bool isEnabled
        {
            get => SceneManager.settings.inGameToolbarEnabled;
            set => SceneManager.settings.inGameToolbarEnabled = value;
        }

#if UNITY_EDITOR

        /// <summary>Enables or disables <see cref="InGameToolbarUtility"/> in editor.</summary>
        public static bool isEnabledInEditor
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.InGameToolbar.IsEnabledInEditor", false);
            set => EditorPrefs.SetBool("AdvancedSceneManager.InGameToolbar.IsEnabledInEditor", value);
        }

        #region ASMSettings

        static void InitializeSettings()
        {
            SettingsTab.instance.Add(CreateElement, header: "Options_In-game Toolbar");
            SettingsTab.instance.Add(CreateElement2, header: "Options_In-game Toolbar");
        }

        static VisualElement CreateElement()
        {
            var element = new UnityEngine.UIElements.Toggle("Display in-game toolbar:") { value = isEnabled };
            _ = element.RegisterValueChangedCallback(e => { isEnabled = e.newValue; ToggleIfInPlayMode(); });
            element.tooltip = "Displays the in-game toolbar, it is collapsed by default (it looks like an arrow then), and can be expanded to more easily debug scene management issues in build.";
            return element;
        }

        static VisualElement CreateElement2()
        {
            var element = new UnityEngine.UIElements.Toggle("Display in editor:") { value = isEnabledInEditor };
            _ = element.RegisterValueChangedCallback(e => { isEnabledInEditor = e.newValue; ToggleIfInPlayMode(); });
            element.tooltip = "Displays the in-game toolbar, it is collapsed by default (it looks like an arrow then), and can be expanded to more easily debug scene management issues in build.";
            return element;
        }

        static void ToggleIfInPlayMode()
        {
            if (Application.isPlaying)
                if (isOpen)
                    Hide();
                else
                    Show();
        }

        #endregion

#endif
        #region Show

        class OnGUIScript : MonoBehaviour
        {
            [HideInInspector] public bool isExpanded;
            [HideInInspector] public bool displayGameObjects;
            [HideInInspector] public bool displayComponents;
            [HideInInspector] public Vector2 scroll;
            [HideInInspector] public float width = 200;
            void OnGUI() => InGameToolbarUtility.OnGUI();
        }

        static OnGUIScript script;
        static bool isOpen => script;
        static void Show()
        {

            if (!isEnabled)
                return;

#if UNITY_EDITOR
            if (!isEnabledInEditor)
                return;
#else
            if (!Debug.isDebugBuild)
                return;
#endif

            script = Object.FindObjectOfType<OnGUIScript>();
            if (script)
                return;

            script = SceneManager.utility.AddToDontDestroyOnLoad<OnGUIScript>();

        }

        static void Hide()
        {
            Object.Destroy(script);
            script = null;
        }

        #endregion
        #region OnGUI

        #region Properties

        static bool isMouseDown;
        static bool isExpanded
        {
            get => script.isExpanded;
            set => script.isExpanded = value;
        }

        static bool displayGameObjects
        {
            get => script.displayGameObjects;
            set => script.displayGameObjects = value;
        }

        static bool displayComponents
        {
            get => script.displayComponents;
            set => script.displayComponents = value;
        }

        static Vector2 scroll
        {
            get => script.scroll;
            set => script.scroll = value;
        }

        static float width
        {
            get => script.width;
            set => script.width = value;
        }

        readonly static SerializableDictionary<int, bool> expanded = new SerializableDictionary<int, bool>();

        #endregion

        static bool hasInitializedStyles;
        static Texture2D texture;
        static void OnGUI()
        {

            if (!hasInitializedStyles)
            {
                hasInitializedStyles = true;
                Styles.Initialize();
                Content.Initialize();
            }

            if (!texture)
            {
                texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.white);
            }

            if (Event.current.rawType == EventType.MouseDown)
                isMouseDown = true;
            else if (Event.current.rawType == EventType.MouseUp)
                isMouseDown = false;

            if (isExpanded)
                Toolbar();
            else
            {
                expanded.Clear();
                objects.Clear();
                properties.Clear();
            }

            Expander();

        }

        #region Expander button

        static readonly Vector2 expanderSize = new Vector2(22, 52);

        static readonly Toggle expander = new Toggle() { onContent = "→", offContent = "←", onClick = ToggleExpanded, normalColor = new Color(0, 0, 0, 0.5f), hoverColor = new Color(0, 0, 0, 0.65f), clickColor = new Color(0, 0, 0, 0.8f) };

        static void Expander()
        {
            expander.style = Styles.expanderStyle;
            var r = new Rect(Screen.width - expanderSize.x, (Screen.height / 2) - (expanderSize.y / 2), expanderSize.x, expanderSize.y);
            expander.OnGUI(r);
        }

        #endregion
        #region Toolbar

        static Color borderColor = new Color32(100, 100, 100, 150);
        static Color backgroundColor = new Color32(0, 0, 0, 200);
        static Color foregroundColor = new Color32(200, 200, 200, 255);

        static readonly Panel panel = new Panel() { normalColor = backgroundColor };

        static bool isDragging;
        static Rect position;
        static void Toolbar()
        {

            var c = GUI.color;
            GUI.color = foregroundColor;

            position = new Rect(Screen.width - width - 22, 22, width, Screen.height - 44);
            panel.OnGUI(position);

            GUILayout.BeginArea(position);

            Header();
            Separator();
            SceneOperations();
            Separator();
            Scenes();

            GUILayout.EndArea();
            GUI.color = c;

            Resize();
            width = Mathf.Clamp(width, 186, Screen.width - 44);

        }

        #region Header

        static readonly Button restartGame = new Button() { content = "↻", onClick = Restart, options = new[] { GUILayout.Width(26), GUILayout.Height(26) } };
        static readonly Button reopenCollection = new Button() { content = "↻ collection", onClick = ReopenCollection, options = new[] { GUILayout.Height(26) } };
        static readonly Button quit = new Button() { content = "×", onClick = Quit, options = new[] { GUILayout.Width(26), GUILayout.Height(26) } };
        static readonly Toggle displayGameObjectsButton = new Toggle() { content = "Display gameobjects:", onToggled = b => displayGameObjects = b, options = new[] { GUILayout.Width(22), GUILayout.Height(22) } };
        static readonly Toggle displayComponentsButton = new Toggle() { content = "Display components:", middleSpacing = 9, onToggled = b => displayComponents = b, options = new[] { GUILayout.Width(22), GUILayout.Height(22) } };

        static void Header()
        {

            //restart / reopen / collapse buttons
            restartGame.style = Styles.button;
            reopenCollection.style = Styles.reopenCollection;
            quit.style = Styles.quit;
            displayGameObjectsButton.style = Styles.button;
            displayGameObjectsButton.isOn = displayGameObjects;
            displayComponentsButton.style = Styles.button;
            displayComponentsButton.isOn = displayComponents;

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            restartGame.OnGUI();
            reopenCollection.OnGUI();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            quit.OnGUI(new Rect(position.width - 26 - 12, 12, 26, 26));
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            //Properties
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            displayGameObjectsButton.OnGUI();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            displayComponentsButton.OnGUI();
            GUILayout.EndHorizontal();

        }

        static void Restart() =>
            SceneManager.runtime.Restart();

        static void ReopenCollection() =>
            SceneManager.collection.Reopen();

        static void Quit() =>
            SceneManager.runtime.Quit();

        static void ToggleExpanded() =>
            isExpanded = !isExpanded;

        #endregion
        #region SceneOperations

        static void SceneOperations()
        {

            GUILayout.Label("Scene Operations:", Styles.h1);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Queued:", Styles.h2);
            GUILayout.Label("Running:", Styles.h2);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(SceneOperation.queue.Count.ToString(), Styles.h2);
            GUILayout.Label(SceneOperation.running.Count.ToString(), Styles.h2);
            GUILayout.EndHorizontal();

        }

        #endregion
        #region Scenes

        struct Obj
        {
            public GameObject obj;
            public Component[] components;
            public Obj[] children;
        }

        static readonly Dictionary<OpenSceneInfo, Obj[]> objects = new Dictionary<OpenSceneInfo, Obj[]>();
        static readonly Dictionary<Component, string[]> properties = new Dictionary<Component, string[]>();

        static bool anyScenes;
        static void Scenes()
        {

            //Updates objects, if enabled
            UpdateObjects();

            anyScenes = false;

            var r = GUILayoutUtility.GetLastRect();
            GUILayout.BeginVertical(Styles.GetMargin(0, 0, 12, 0));

            scroll = GUILayout.BeginScrollView(scroll, alwaysShowHorizontal: false, alwaysShowVertical: false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, Styles.scroll);

            DrawScenes("Persistent:", SceneManager.utility.openScenes.Where(s => s.isPersistent && !s.isSpecial));
            DrawScenes("Collection:", SceneManager.utility.openScenes.Where(s => s.isCollection && !s.isPersistent));
            DrawScenes("Standalone:", SceneManager.utility.openScenes.Where(s => s.isStandalone && !s.isPersistent));
            DrawScenes("Special:", SceneManager.utility.openScenes.Where(s => s.isSpecial).Concat(new[] { SceneManager.utility.dontDestroyOnLoad }));
            DrawScenes("Untracked (any scenes appearing here is a bug, please report):", SceneManager.utility.openScenes.Where(s => s.isUntracked));

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (!anyScenes)
                GUILayout.Label("No open scenes.", Styles.alignCenter);

        }

        static void DrawScenes(string header, IEnumerable<OpenSceneInfo> scenes)
        {

            if (!scenes.Any())
                return;
            anyScenes = true;

            GUILayout.Label(header, Styles.h1);
            GUILayout.BeginHorizontal(Styles.GetMargin(12, 0, 0, 0));
            GUILayout.BeginVertical(Styles.margin_12_12_0_0);

            foreach (var scene in scenes)
                if (CollapsibleHeader(scene.scene.name, hasChildren: objects.GetValue(scene)?.Any() ?? false, key: scene.scene, defaultValue: SceneManager.utility.activeScene == scene))
                    DrawObjects(scene);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

        }

        static void DrawObjects(OpenSceneInfo scene)
        {
            if (expanded[scene.scene.GetInstanceID()] && objects.ContainsKey(scene))
                foreach (var obj in objects[scene])
                    DrawObjects(scene, obj);
        }

        const float objMargin = 16;
        static void DrawObjects(OpenSceneInfo scene, Obj obj, int depth = 1)
        {

            if (!obj.obj)
            {
                UpdateObjects();
                GUIUtility.ExitGUI();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(objMargin * depth);

            var isExpanded = CollapsibleHeader(header: obj.obj.name, hasChildren: obj.children.Any() || (obj.components?.Any() ?? false), key: obj.obj, defaultValue: false);

            GUILayout.EndHorizontal();

            if (isExpanded)
            {

                GUILayout.BeginVertical(Styles.GetMargin(Mathf.RoundToInt(objMargin * depth), 0, 0, 0));
                if (displayComponents && (obj.components?.Any() ?? false))
                {

                    GUILayout.Label("Components:", Styles.inListHeader);
                    GUILayout.BeginVertical(Styles.margin_12_0_0_0);

                    foreach (var c in obj.components)
                        if (CollapsibleHeader(c.GetType().Name, hasChildren: true, key: c, false))
                        {

                            try
                            {
                                if (!properties.ContainsKey(c))
                                    properties.Add(c, c.GetType().GetMembers().
                                        Where(m => m is PropertyInfo || m is FieldInfo).
                                        Where(m => m.GetCustomAttribute<ObsoleteAttribute>() == null).
                                        Select(m => m.Name + ": " + ((m as PropertyInfo)?.GetValue(c) ?? (m as FieldInfo)?.GetValue(c))).
                                        ToArray());
                            }
                            catch (Exception e)
                            {
                                if (!properties.ContainsKey(c))
                                    if (e.InnerException != null)
                                        properties.Add(c, new[] { "Error: " + e.InnerException.Message });
                                    else
                                        properties.Add(c, new[] { "Error: " + e.Message });
                            }

                            foreach (var value in properties[c])
                            {
                                var c1 = GUI.color;
                                if (value.StartsWith("Error:"))
                                    GUI.color = Color.red;
                                GUILayout.Label(value, Styles.propertyItem);
                                GUI.color = c1;
                            }

                        }
                        else
                            properties.Remove(c);

                    GUILayout.EndVertical();

                }
                GUILayout.EndVertical();

                if (obj.children.Any())
                {

                    if (displayComponents)
                    {
                        GUILayout.BeginVertical(Styles.GetMargin(Mathf.RoundToInt(objMargin * (depth)), 0, 0, 0));
                        GUILayout.Label("Children:", Styles.inListHeader);
                        GUILayout.EndVertical();
                    }

                    foreach (var o in obj.children)
                        DrawObjects(scene, o, depth + 1);

                }

            }

        }

        static float lastObjectUpdate;
        static void UpdateObjects()
        {

            if (!displayGameObjects)
            {
                objects.Clear();
                expanded.Clear();
                return;
            }

            if (Time.time - lastObjectUpdate < 1)
                return;
            lastObjectUpdate = Time.time;

            objects.Clear();

            foreach (var scene in SceneManager.utility.openScenes)
                AddSceneObjects(scene);
            AddSceneObjects(SceneManager.utility.dontDestroyOnLoad);

            foreach (var id in expanded.Keys.ToArray())
                if (!FindObjectFromInstanceID(id))
                    _ = expanded.Remove(id);

            Obj GetObj(GameObject o) =>
                new Obj() { obj = o, children = Children(o).Select(GetObj).ToArray(), components = displayComponents ? Components(o).ToArray() : null };

            void AddSceneObjects(OpenSceneInfo scene) =>
                objects.Add(scene, scene.unityScene.Value.GetRootGameObjects().Select(GetObj).GroupBy(o1 => o1.obj.GetInstanceID()).Select(g => g.First()).ToArray());

            IEnumerable<GameObject> Children(GameObject obj)
            {
                for (var i = 0; i < obj.transform.childCount; i++)
                    yield return obj.transform.GetChild(i).gameObject;
            }

            IEnumerable<Component> Components(GameObject obj) =>
                obj.GetComponents<Component>();

        }

        static Func<int, Object> m_FindObjectFromInstanceID = null;
        static Object FindObjectFromInstanceID(int instanceID)
        {

            if (m_FindObjectFromInstanceID == null)
                if (typeof(Object).GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static) is MethodInfo method)
                    m_FindObjectFromInstanceID = (Func<int, Object>)Delegate.CreateDelegate(typeof(Func<int, Object>), method);
                else
                    Debug.LogError("FindObjectFromInstanceID() was not found in UnityEngine.Object");

            return m_FindObjectFromInstanceID?.Invoke(instanceID);

        }

        #endregion

        #endregion
        #region GUI helpers

        static void Separator()
        {
            GUILayout.Space(12);
            var r = GUILayoutUtility.GetRect(position.width, 1);
            GUI.DrawTexture(Rect.MinMaxRect(r.xMin + 12, r.y, r.xMax - 12, r.y + 1), texture, default, default, default, borderColor, 0, 0);
            GUILayout.Space(12);
        }

        static bool CollapsibleHeader(string header, bool hasChildren, Object key, bool defaultValue)
        {

            if (!key)
            {
                Debug.LogWarning("The key cannot be null.");
                return false;
            }

            if (!expanded.ContainsKey(key.GetInstanceID()))
                expanded.Add(key.GetInstanceID(), defaultValue);

            if (!hasChildren)
                GUILayout.Label(header, Styles.noWordWrap);
            else
            {

                if (GUILayout.Button(header, Styles.collapsibleHeader))
                    expanded[key.GetInstanceID()] = !expanded[key.GetInstanceID()];

                var c = expanded[key.GetInstanceID()] ? Content.expanded : Content.collapsed;
                var size = Styles.collapsibleHeader.CalcSize(c);

                var r = GUILayoutUtility.GetLastRect();
                r = new Rect(r.xMin - size.x - 4, r.y - 0, size.x, size.y);
                GUI.Label(r, c, Styles.collapsibleHeader);

            }

            return expanded[key.GetInstanceID()];

        }

        static Color resizeDragColor = new Color32(150, 150, 150, 255);
        static void Resize()
        {

            var r = new Rect(position.xMin, position.yMin, 4, position.height);
            if (r.Contains(Event.current.mousePosition))
            {

                GUI.DrawTexture(r, texture, default, default, default, Color.white, 0, 0);

                if (isMouseDown)
                    isDragging = true;

            }

            if (!isMouseDown)
                isDragging = false;

            if (isDragging)
            {
                GUI.DrawTexture(r, texture, default, default, default, resizeDragColor, 0, 0);
                width = Mathf.Clamp(Screen.width - Event.current.mousePosition.x - 22, 186, Screen.width - 44);
            }

        }

        class Panel : Button
        {
            public override bool isEnabled
            {
                get => false;
                set { }
            }
        }

        class Toggle : Button
        {

            public string offContent = "";
            public string onContent = "✓";
            public Action<bool> onToggled;
            public float middleSpacing = 6;

            string m_content;
            public new string content
            {
                get => m_content;
                set => m_content = value;
            }

            public bool isOn = false;

            public override void OnGUI(Rect? rect = null)
            {
                base.content = isOn ? onContent : offContent;
                GUILayout.BeginHorizontal();
                GUILayout.Label(content, GUILayout.ExpandWidth(false));
                GUILayout.Space(middleSpacing);
                base.OnGUI(rect);
                GUILayout.EndHorizontal();
            }

            protected override void OnClick()
            {
                isOn = !isOn;
                base.OnClick();
                onToggled?.Invoke(isOn);
            }

        }

        class Button
        {

            Color? color;
            public Color normalColor { get; set; } = new Color32(93, 93, 93, 255);
            public Color hoverColor { get; set; } = new Color32(70, 70, 70, 255);
            public Color clickColor { get; set; } = new Color32(50, 50, 50, 255);

            public virtual bool isEnabled { get; set; } = true;

            public virtual string content { get; set; }
            public GUIStyle style { get; set; }

            public GUILayoutOption[] options { get; set; }

            public Action onClick;

            static GUIStyle label;

            Rect r;
            public virtual void OnGUI(Rect? rect = null)
            {

                if (label == null)
                {
                    label = new GUIStyle(GUI.skin.label);
                    label.normal.background = label.hover.background = label.active.background = texture;
                }

                if (style != null)
                    style.normal.background = style.hover.background = style.active.background = texture;

                var size =
                    style != null
                    ? style.CalcSize(new GUIContent(content))
                    : Vector2.zero;

                r = rect ?? GUILayoutUtility.GetRect(size.x + (style ?? label).padding.horizontal, size.y + (style ?? label).padding.vertical, (style ?? label), options);

                color =
                    isEnabled && r.Contains(Event.current.mousePosition)
                    ? isMouseDown
                        ? clickColor
                        : hoverColor
                    : new Color?();

                var c = GUI.backgroundColor;
                GUI.backgroundColor = color ?? normalColor;

                if (isEnabled && (style != null ? GUI.Button(r, content, style) : GUI.Button(r, content)))
                    OnClick();
                else if (!isEnabled)
                    GUI.Label(r, content, label);

                GUI.DrawTexture(r, texture, default, default, default, borderColor, 1, 0);

                GUI.backgroundColor = c;

            }

            protected virtual void OnClick() =>
                onClick?.Invoke();

        }

        static class Styles
        {

            public static GUIStyle expanderStyle { get; private set; }
            public static GUIStyle collapsibleHeader { get; private set; }
            public static GUIStyle noWordWrap { get; private set; }

            public static GUIStyle button { get; private set; }
            public static GUIStyle reopenCollection { get; private set; }
            public static GUIStyle quit { get; private set; }

            public static GUIStyle h1 { get; private set; }
            public static GUIStyle h2 { get; private set; }
            public static GUIStyle margin_12_12_0_0 { get; private set; }
            public static GUIStyle margin_12_0_0_0;
            public static GUIStyle scroll { get; private set; }
            public static GUIStyle alignCenter { get; private set; }
            public static GUIStyle propertyItem { get; private set; }
            public static GUIStyle inListHeader { get; private set; }

            static readonly List<GUIStyle> marginStyles = new List<GUIStyle>();

            public static GUIStyle GetMargin(int left, int right, int top, int bottom)
            {

                if (marginStyles.FirstOrDefault(s => s.margin.left == left && s.margin.right == right && s.margin.top == top && s.margin.bottom == bottom) is GUIStyle style)
                    return style;

                style = new GUIStyle() { margin = new RectOffset(left, right, top, bottom) };
                marginStyles.Add(style);
                return style;

            }

            public static void Initialize()
            {

                expanderStyle = new GUIStyle(GUI.skin.button) { padding = new RectOffset(4, 0, 0, 0) };
                collapsibleHeader = new GUIStyle(GUI.skin.label) { wordWrap = false, hover = new GUIStyleState() { textColor = foregroundColor }, active = new GUIStyleState() { textColor = foregroundColor } };
                noWordWrap = new GUIStyle(GUI.skin.label) { wordWrap = false };

                button = new GUIStyle(GUI.skin.button) { padding = new RectOffset(0, 0, 0, 0), fontSize = 16 };

                button.normal.background = button.hover.background = button.active.background = texture;
                reopenCollection = new GUIStyle(button) { padding = new RectOffset(6, 6, 0, 0), fontSize = 16 };
                quit = new GUIStyle(button) { padding = new RectOffset(2, 0, 0, 0), fontSize = 16 };

                h1 = new GUIStyle(GUI.skin.label) { margin = new RectOffset(0, 0, 6, 6), padding = new RectOffset(16, 0, 0, 0), fontSize = 15 };
                h2 = new GUIStyle(GUI.skin.label) { margin = new RectOffset(0, 0, 6, 6), alignment = TextAnchor.MiddleCenter, };
                scroll = new GUIStyle(GUI.skin.scrollView) { margin = new RectOffset(0, 0, 6, 12) };
                alignCenter = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

                margin_12_12_0_0 = new GUIStyle() { margin = new RectOffset(12, 12, 0, 0) };
                margin_12_0_0_0 = new GUIStyle() { margin = new RectOffset(12, 0, 0, 0) };
                propertyItem = new GUIStyle(GUI.skin.label) { wordWrap = false, normal = new GUIStyleState() { textColor = Color.gray } };
                inListHeader = new GUIStyle(GUI.skin.label) { wordWrap = false, normal = new GUIStyleState() { textColor = Color.gray } };

            }

        }

        static class Content
        {

            public static GUIContent expanded;
            public static GUIContent collapsed;

            public static void Initialize()
            {
                expanded = new GUIContent("▼");
                collapsed = new GUIContent("▶");
            }

        }

        #endregion

        #endregion

    }

}
