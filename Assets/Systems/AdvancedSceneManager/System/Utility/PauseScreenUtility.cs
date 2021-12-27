#pragma warning disable IDE0051 // Remove unused private members

using AdvancedSceneManager.Models;
using Lazy.Utility;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Contains functions for interacting with the default pause screen.</summary>
    [AddComponentMenu("")]
    public class PauseScreenUtility : MonoBehaviour
    {

        #region Static

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad() =>
            CoroutineUtility.Run(when: () => SceneManager.runtime.isInitialized, action: () =>
            {

                coroutine?.Stop();
                Hide();

                if (Profile.current && Profile.current.useDefaultPauseScreen)
                    ListenForKey();

                if (Profile.current)
                {
                    Profile.current.PropertyChanged -= Profile_PropertyChanged;
                    Profile.current.PropertyChanged += Profile_PropertyChanged;
                }

            });

        private static void Profile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Profile.useDefaultPauseScreen))
                OnLoad();
        }

        /// <summary>Gets if the pause screen is currently open.</summary>
        public static bool isOpen =>
            current != null;

        static GlobalCoroutine coroutine;

        /// <summary>Starts listening keys and opens pause screen when keys pressed.</summary>
        public static void ListenForKey()
        {
            StopListening();
            coroutine = Listen().StartCoroutine(description: "Default Pause Screen");
        }

        /// <summary>Stops listening for keys, this will disable pause screen. (Manually calling <see cref="Show"/> will still work though)</summary>
        public static void StopListening() =>
            coroutine?.Stop();

        static IEnumerator Listen()
        {
            while (true)
            {

                yield return null;

                if (!LoadingScreenUtility.IsAnyLoadingScreenOpen)
                {

#if ENABLE_INPUT_SYSTEM
                    if ((UnityEngine.InputSystem.Keyboard.current?.escapeKey?.wasPressedThisFrame ?? false) ||
                        (UnityEngine.InputSystem.Gamepad.current?.startButton?.wasPressedThisFrame ?? false))
                        Toggle();
                    if (UnityEngine.InputSystem.Gamepad.current?.bButton?.wasPressedThisFrame ?? false)
                        Hide();
#else
                    if (Input.GetKeyDown(KeyCode.Escape))
                        Toggle();
#endif
                }

                if (PauseScreenInput.Current)
                    PauseScreenInput.Current.DoUpdate();

            }
        }

        internal static PauseScreenUtility current;
        static UnityEngine.SceneManagement.Scene scene;
        static bool IsOpeningOrClosing;
        static CursorLockMode cursorLockState;
        static bool cursorVisible;

        /// <summary>Shows the pause screen.</summary>
        public static void Show()
        {

            if (IsOpeningOrClosing || current)
                return;

            IsOpeningOrClosing = true;

            scene = UnityEngine.SceneManagement.SceneManager.CreateScene("Pause screen");

            var o = Instantiate(Resources.Load<GameObject>("AdvancedSceneManager/DefaultPauseScreen"));

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(o, scene);
            var components = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<Behaviour>()).ToArray();

            if (components.OfType<PauseScreenUtility>().FirstOrDefault() is PauseScreenUtility callback)
                current = callback;
            else
            {
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                return;
            }

            if (components.OfType<Canvas>().FirstOrDefault() is Canvas canvas)
            {
                canvas.PutOnTop();
                canvas.gameObject.AddComponent<PauseScreenInput>().ActivateModule();
            }

            current.Begin()?.StartCoroutine(() =>
                IsOpeningOrClosing = false);

            cursorLockState = Cursor.lockState;
            cursorVisible = Cursor.visible;

            if (!Camera.main)
            {
                var camera = o.AddComponent<Camera>();
                camera.backgroundColor = Color.black;
            }

        }

        private void LateUpdate()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Hides the pause screen.</summary>
        public static void Hide(bool ignoreAnimations = false)
        {

            if (IsOpeningOrClosing)
                return;

            if (!current)
                return;

            IsOpeningOrClosing = true;

            DoHide().StartCoroutine();
            IEnumerator DoHide()
            {

                if (!ignoreAnimations)
                    yield return current.End();

                if (scene.isLoaded)
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);

                current = null;
                scene = default;
                IsOpeningOrClosing = false;

            }

            Cursor.lockState = cursorLockState;
            Cursor.visible = cursorVisible;

        }

        /// <summary>Toggles the pause screen on / off.</summary>
        public static void Toggle()
        {
            if (!current)
                Show();
            else
                Hide();
        }

        #endregion

        public Button resume;
        public Button restartCollection;
        public Button restartGame;
        public Button quit;

        public CanvasGroup canvasGroup;

        public IEnumerator Begin()
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = 0;
                yield return canvasGroup.Fade(1, 0.25f);
            }
        }

        public IEnumerator End()
        {
            if (canvasGroup)
                yield return canvasGroup.Fade(0, 0.25f);
        }

        #region Buttons

        public void RestartCollection()
        {

            Wait().StartCoroutine();
            IEnumerator Wait()
            {

                canvasGroup.interactable = false;

                yield return SceneManager.collection.Reopen();

                if (canvasGroup)
                    canvasGroup.interactable = true;
                Resume();

            }

        }

        public void RestartGame()
        {
            canvasGroup.interactable = false;
            SceneManager.runtime.Restart();
        }

        public void Resume() =>
            Hide();

        public void Quit()
        {
            canvasGroup.interactable = false;
            SceneManager.runtime.Quit();
        }

        #endregion

    }

    class PauseScreenInput :
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.UI.InputSystemUIInputModule
#else
        UnityEngine.EventSystems.StandaloneInputModule
#endif
    {

        public static PauseScreenInput Current { get; private set; }

        int index = 0;

        List<Button> buttons;

        protected override void Start()
        {
            Current = this;
            base.Start();
            buttons = new List<Button>()
            {
                PauseScreenUtility.current.resume,
                PauseScreenUtility.current.restartCollection,
                PauseScreenUtility.current.restartGame,
                PauseScreenUtility.current.quit,
            };
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Current == this)
                Current = null;
        }

        void MoveUp() =>
            MoveTo(index - 1);

        void MoveDown() =>
            MoveTo(index + 1);

        void MoveTo(int index)
        {

            if (index < 0) index = 0;
            if (index > 3) index = 3;

            this.index = index;
            eventSystem.SetSelectedGameObject(buttons[index].gameObject);

        }

        void Activate()
        {
            if (buttons.ElementAtOrDefault(index) is Button button)
                ExecuteEvents.Execute(button.gameObject, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
        }

        void Deselect()
        {
            index = -1;
            eventSystem.SetSelectedGameObject(null);
        }

        public bool isUsingPointer;

        public override bool ShouldActivateModule() =>
            true;

#if !ENABLE_INPUT_SYSTEM
        Vector3 mousePos;
#endif

        //Update is not called for base class if we define it here, so instead we have to create our own update function
        //UpdateModule() does not work since there is some issue with input module activation
        public void DoUpdate()
        {

#if ENABLE_INPUT_SYSTEM

            if (UnityEngine.InputSystem.Pointer.current?.delta?.EvaluateMagnitude() > 1)
                isUsingPointer = true;
            else if (UnityEngine.InputSystem.InputSystem.devices.Where(d => !typeof(UnityEngine.InputSystem.Pointer).IsAssignableFrom(d.GetType())).Any(d => d.wasUpdatedThisFrame))
                isUsingPointer = false;

            if (!isUsingPointer)
            {

                if ((UnityEngine.InputSystem.Keyboard.current?.upArrowKey?.wasPressedThisFrame ?? false) ||
                    (UnityEngine.InputSystem.Gamepad.current?.dpad.up?.wasPressedThisFrame ?? false))
                    MoveUp();

                if ((UnityEngine.InputSystem.Keyboard.current?.downArrowKey?.wasPressedThisFrame ?? false) ||
                    (UnityEngine.InputSystem.Gamepad.current?.dpad.down?.wasPressedThisFrame ?? false))
                    MoveDown();

                if ((UnityEngine.InputSystem.Keyboard.current?.enterKey?.wasPressedThisFrame ?? false) ||
                    (UnityEngine.InputSystem.Keyboard.current?.numpadEnterKey?.wasPressedThisFrame ?? false) ||
                    (UnityEngine.InputSystem.Gamepad.current?.aButton?.wasPressedThisFrame ?? false))
                    Activate();

            }

#else

            if (Input.mousePresent && (mousePos - Input.mousePosition).magnitude > 1)
                isUsingPointer = true;
            else if (Input.anyKey)
                isUsingPointer = false;

            mousePos = Input.mousePosition;

            if (!isUsingPointer)
            {

                if (Input.GetKeyDown(KeyCode.UpArrow))
                    MoveUp();
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    MoveDown();
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    Activate();

            }

#endif

            if (isUsingPointer)
                Deselect();

        }

    }

}
