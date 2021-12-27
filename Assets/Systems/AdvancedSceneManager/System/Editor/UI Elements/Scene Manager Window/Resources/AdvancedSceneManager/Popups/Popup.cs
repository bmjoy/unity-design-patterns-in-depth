using Lazy.Utility;
using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    /// <summary>An interface used by <see cref="Popup{T}"/> to allow it to get <see cref="rootVisualElement"/> and <see cref="position"/> of the editor it is attached to.</summary>
    public interface IUIToolkitEditor
    {
        VisualElement rootVisualElement { get; }
        Rect position { get; set; }
    }

    public abstract class Popup
    {

        public static Popup current { get; protected set; }
        public abstract void Reopen();

    }

    public abstract class Popup<T> : Popup where T : Popup<T>, new()
    {

        public override void Reopen() =>
            OnReopen(Open(placementTarget, parent, alignRight, offset, stretchToPlacementTargetWidth, ignoreAnimation: true));

        protected abstract void OnReopen(T newPopup);

        static bool isHover;
        static bool stretchToPlacementTargetWidth;

        public new static T current { get; protected set; }

        public static bool IsOpen() => current != null;
        public static bool IsOpen(IUIToolkitEditor editor) => current?.parent == editor;

        public static T Open(VisualElement placementTarget, IUIToolkitEditor parent, bool alignRight = false, Vector2 offset = default, bool stretchToPlacementTargetWidth = false, bool ignoreAnimation = false)
        {

            if (current != null)
            {
                ClosePopup(onAnimationComplete: () => Open(placementTarget, parent, alignRight, offset, stretchToPlacementTargetWidth, ignoreAnimation));
                return current;
            }

            var popup = new T();

            current = popup;

            //Create wrapper elements
            //Overlay covers entire window, and closes popup on click
            //Popup content is the popup 'window' itself

            popup.overlay?.RemoveFromHierarchy();

            var overlay = new VisualElement() { name = "overlay" };
            var popupContent = new ScrollView() { name = "popup" };

            popup.overlay = overlay;
            popup.rootVisualElement = popupContent;
            parent.rootVisualElement.Add(overlay);
            popup.overlay.Add(popupContent);

            //Set background color for popup since there seems to be no way to get default background color in uss (and no public API either for some reason...)
            popup.rootVisualElement.style.backgroundColor = VisualElementExtensions.DefaultBackgroundColor;

            //Add handler to close popup when overlay clicked
            popupContent.RegisterCallback<MouseEnterEvent>(e => isHover = true);
            popupContent.RegisterCallback<MouseLeaveEvent>(e => isHover = false);
            overlay.RegisterCallback<MouseDownEvent>(e => { if (!isHover) ClosePopup(); });

            //Add popup window style
            overlay.styleSheets.Add(Resources.Load<StyleSheet>("AdvancedSceneManager/Popups/PopupWindow"));

            //Set positioning properties so that SetPosition() can access them later since content is not loaded yet
            popup.offset = offset;
            popup.placementTarget = placementTarget;
            popup.parent = parent;
            popup.alignRight = alignRight;

            //Make sure that the popup content is loaded
            popup.rootVisualElement.style.opacity = 0;
            popup.ReloadContent();

            //We aren't an editor window anymore, but we still need Update() or OnGUI()
            EditorApplication.update -= Update;
            EditorApplication.update += Update;

            Popup<T>.stretchToPlacementTargetWidth = stretchToPlacementTargetWidth;

            popup.HasBorder = popup.EnableBorder;
            current.AnimateOpacity(0, 1, ignoreAnimation ? 0 : 1.5f).StartCoroutine();
            Update();

            if (placementTarget is ToolbarToggle toggle)
                toggle.SetValueWithoutNotify(true);

            return popup;

        }

        static void Update()
        {
            current?.SetPosition();
            current?.overlay?.BringToFront();
        }

        IEnumerator AnimateOpacity(float from, float to, float duration, Action onComplete = null)
        {

            if (duration <= 0)
            {
                rootVisualElement.style.opacity = to;
                yield break;
            }

            yield return new WaitForSeconds(0.1f);

            var time = 0f;
            while (time < duration)
            {
                rootVisualElement.style.opacity = Mathf.Lerp(from, to, time / duration);
                time += 0.33f;
                yield return new WaitForSeconds(0.033f);
            }

            rootVisualElement.style.opacity = to;
            onComplete?.Invoke();

        }

        public void Close() =>
            Close(onAnimationComplete: null);

        public void Close(Action onAnimationComplete = null)
        {

            AnimateOpacity(1, 0, 1.5f, () =>
            {

                overlay?.RemoveFromHierarchy();

                OnClose();
                current = null;
                EditorApplication.update -= Update;

                if (placementTarget is ToolbarToggle toggle)
                    toggle.SetValueWithoutNotify(false);

                onAnimationComplete?.Invoke();

            }).StartCoroutine();

        }

        public static void ClosePopup(Action onAnimationComplete = null) =>
            current?.Close(onAnimationComplete);

        protected virtual void OnClose()
        { }

        #region Load content

        public abstract string path { get; }

        public VisualElement overlay;
        public VisualElement rootVisualElement;

        public bool isMainContentLoaded => rootVisualElement.childCount > 0;

        /// <summary>Loads the <see cref="VisualTreeAsset"/> and its associated <see cref="StyleSheet"/> at the same path.</summary>
        public void LoadContent(string path, VisualElement element) =>
            LoadContent(path, element, true, true, true);

        /// <summary>Loads the <see cref="VisualTreeAsset"/> and its associated <see cref="StyleSheet"/> at the same path.</summary>
        public void LoadContent(string path, VisualElement element, bool loadTree = false, bool loadStyle = false, bool clearChildren = false)
        {

            if (clearChildren)
                element.Clear();

            //Load all assets at path, since every VisualTreeAsset has an inline StyleSheet associated, 
            //which means that we can't rely on Resources.Load<StyleSheet>(path) since that
            //might randomly load the inline as the StyleSheet instead, which won't work since all of our 
            //uxml and uss assets that are associated share the same name
            var items = Resources.LoadAll(path);
            var style = items.OfType<StyleSheet>().Where(s => !s.name.Contains("inline")).FirstOrDefault();
            var tree = items.OfType<VisualTreeAsset>().FirstOrDefault();

            if (style && loadStyle) element.styleSheets.Add(style);
            if (tree && loadTree)
                element.Add(tree.CloneTree());

        }

        public void ReloadContent()
        {
            LoadContent(path, rootVisualElement);
        }

        #endregion

        protected virtual bool EnableBorder { get; } = true;

        public bool HasBorder
        {
            get => rootVisualElement.ClassListContains("border");
            set => rootVisualElement.EnableInClassList("border", value);
        }

        public Vector2 offset { get; private set; }
        public bool alignRight { get; private set; }
        public VisualElement placementTarget { get; private set; }
        public IUIToolkitEditor parent { get; private set; }

        public static void SetOffset(Vector2 offset)
        {
            if (current != null)
                current.offset = offset;
        }

        (float x, float y) GetPosition()
        {
            var pos = placementTarget.worldBound.position;
            return (pos.x - offset.x, pos.y - offset.y);
        }

        void SetSize()
        {
            if (stretchToPlacementTargetWidth)
                rootVisualElement.style.width = placementTarget.resolvedStyle.width;
        }

        protected void SetPosition()
        {

            if (parent == null)
                return;

            SetSize();
            if (float.IsNaN(rootVisualElement.resolvedStyle.width) || float.IsNaN(placementTarget.resolvedStyle.width))
                return;

            (float x, float y) = GetPosition();

            if (alignRight && x != 0)
                x -= rootVisualElement.resolvedStyle.width - placementTarget.resolvedStyle.width;

            rootVisualElement.style.maxHeight = parent.position.height - y;

            if (y + rootVisualElement.resolvedStyle.height >= parent.rootVisualElement.resolvedStyle.height - 10)
            {
                y = parent.rootVisualElement.resolvedStyle.height - rootVisualElement.Children().ElementAt(0).resolvedStyle.height - 12;
                rootVisualElement.style.minHeight = rootVisualElement.Children().ElementAt(0).resolvedStyle.height + 10;
            }

            rootVisualElement.style.left = x;
            rootVisualElement.style.top = y;

        }

    }

}
