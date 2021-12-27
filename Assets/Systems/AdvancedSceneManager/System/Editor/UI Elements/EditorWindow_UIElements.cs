using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    public abstract class EditorWindow_UIElements<T> : EditorWindow where T : EditorWindow_UIElements<T>
    {

        public virtual bool autoReloadOnWindowFocus => true;
        public abstract string path { get; }

        public new GUIContent title
        {
            get => window.titleContent;
            set => window.titleContent = value;
        }

        #region Singleton

        public static T window =>
            Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();

        public static void Open()
        {
            var w = window ? window : GetWindow<T>();
            w.DoShow();
            w.OnShow();
        }

        public static new void Close()
        {
            if (window is EditorWindow w && w)
            {
                window.OnClose();
                w.Close();
            }
        }

        public virtual void OnClose()
        { }

        /// <summary>Override this to change the way this window should be opened, by default <see cref="EditorWindow.Show()"/> is used.</summary>
        protected virtual void DoShow()
        {
            Show();
        }

        protected virtual void OnShow()
        {
            title = new GUIContent(ObjectNames.NicifyVariableName(GetType().Name.Replace("Window", "")));
        }

        public static void Reopen()
        {
            Close();
            Open();
        }

        #endregion
        #region Load content

        public bool isMainContentLoaded => rootVisualElement.childCount > (suspendMessage != null ? 1 : 0);

        VisualElement suspendMessage;
        protected void ShowSuspendMessage()
        {
            suspendMessage?.RemoveFromHierarchy();
            suspendMessage = new Label("Scene Manager window is suspended, press anywhere to load.");
            suspendMessage.style.alignSelf = new StyleEnum<Align>(Align.Center);
            suspendMessage.style.marginTop = 12f;
            rootVisualElement.Add(suspendMessage);
        }

        /// <summary>Loads the <see cref="VisualTreeAsset"/> and its associated <see cref="StyleSheet"/> at the same path.</summary>
        public void LoadContent(string path, VisualElement element) =>
            LoadContent(path, element, true, true, true);

        /// <summary>Loads the <see cref="VisualTreeAsset"/> and its associated <see cref="StyleSheet"/> at the same path.</summary>
        public void LoadContent(string path, VisualElement element, bool loadTree = false, bool loadStyle = false, bool clearChildren = false)
        {

            if (clearChildren)
            {
                if (element == rootVisualElement)
                    suspendMessage?.RemoveFromHierarchy();
                element.Clear();
            }

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

        public void ReloadContent()
        {
            LoadContent(path, rootVisualElement);
        }

        #endregion

        public virtual void OnFocus()
        {
            if (focusedWindow == this && autoReloadOnWindowFocus)
                ReloadContent();
        }

        public virtual void OnEnable()
        {
            if (focusedWindow == this)
                ReloadContent();
        }

    }

}
