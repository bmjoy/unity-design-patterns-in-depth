#pragma warning disable IDE0051 // Remove unused private members

using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using AdvancedSceneManager.Core.AsyncOperations;

using static AdvancedSceneManager.SceneManager;
using System.Linq;
using AdvancedSceneManager.Utility;
using System;
using AdvancedSceneManager.Models;
using Lazy.Utility;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace AdvancedSceneManager.Core
{

    /// <summary>Manages the start and quit processes of the game.</summary>
    public class Runtime
    {

        /// <summary>Gets whatever ASM is done with startup process.</summary>
        public bool isInitialized { get; internal set; }

        /// <summary>Occurs before startup process is started, or when <see cref="Restart"/> is called.</summary>
        public event Action beforeStart;

        /// <summary>Occurs after startup process is done, or when <see cref="Restart"/> is called.</summary>
        public event Action afterStart;

        #region InitializeOnLoad

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {

            //This is to prevent asm from restarting when scripts are recompiled, when user has set playmode to continue
            if (Application.isPlaying)
            {
                Application.quitting += () => PlayerPrefs.SetInt("AdvancedSceneManager.IsPlaying", 0);
                if (PlayerPrefs.GetInt("AdvancedSceneManager.IsPlaying", 0) == 1)
                    return;
                PlayerPrefs.SetInt("AdvancedSceneManager.IsPlaying", 1);
            }
            else
                PlayerPrefs.SetInt("AdvancedSceneManager.IsPlaying", 0);

            SetProfile();

            if (profile && profile.enableChangingBackgroundLoadingPriority)
                Application.backgroundLoadingPriority = profile.backgroundLoadingPriority;

#if UNITY_EDITOR

            if (!EditorApplication.isPlaying)
                RestoreSceneSetup();

            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;

#endif

            standalone.OnLoad();
            if (!Application.isPlaying)
                return;

            DefaultSceneUtility.EnsureOpen();

            if (runtime.isBuildMode)
                runtime.DoStart(props);
            else
                runtime.isInitialized = true;

        }

#if UNITY_EDITOR
        static void EditorApplication_playModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                runtime.isBuildMode = false;
        }
#endif

        #endregion
        #region Start

        [Serializable]
        public struct StartProps
        {

            /// <summary>
            /// <para>Gets the default <see cref="StartProps"/>.</para>
            /// <para>Cannot be called during <see cref="UnityEngine.Object"/> constructor.</para>
            /// </summary>
            public static StartProps GetDefault() =>
                new StartProps()
                {
                    skipSplashScreen = false,
                    ignoreDoNotOpen = false,
                    fadeColor = SceneManager.settings.buildUnitySplashScreenColor,
                    initialFadeDuration = 0,
                    beforeSplashScreenFadeDuration = 1f,
                    m_overrideOpenCollection = ""
                };

            public bool skipSplashScreen;
            public bool ignoreDoNotOpen;
            public Color fadeColor;
            public float initialFadeDuration;
            public float beforeSplashScreenFadeDuration;

#pragma warning disable CS0414
            [SerializeField] private string m_overrideOpenCollection;
#pragma warning restore CS0414

            public SceneCollection overrideOpenCollection
            {
                get
                {
#if UNITY_EDITOR
                    return AssetDatabase.LoadAssetAtPath<SceneCollection>(m_overrideOpenCollection);
#else
                    return null;
#endif
                }
                set
                {
#if UNITY_EDITOR
                    m_overrideOpenCollection = AssetDatabase.GetAssetPath(value);
#else
                    return;
#endif
                }
            }

        }

        static StartProps props
        {
            get
            {
                var json = PlayerPrefs.GetString("AdvancedSceneManager.StartProps", null);
                if (!string.IsNullOrWhiteSpace(json) && JsonUtility.FromJson<StartProps>(json) is StartProps props)
                    return props;
                else
                    return StartProps.GetDefault();
            }

            set => PlayerPrefs.SetString("AdvancedSceneManager.StartProps", JsonUtility.ToJson(value));
        }

        /// <summary>
        /// <para>Gets whatever we're currently in build mode.</para>
        /// <para>This is true when in build or when play button in scene manager window is pressed.</para>
        /// </summary>
        public bool isBuildMode
        {
#if UNITY_EDITOR
            private set => PlayerPrefs.SetInt("AdvancedSceneManager.ForceBuildMode", value ? 1 : 0);
            get => PlayerPrefs.GetInt("AdvancedSceneManager.ForceBuildMode", 0) == 1;
#else
            get => true;
#endif
        }

        /// <summary>Gets if game was started as a build.</summary>
        public bool wasStartedAsBuild { get; private set; }

        /// <summary>
        /// <para>Starts startup sequence.</para>
        /// <para>Enters playmode if in editor.</para>
        /// </summary>
        /// <param name="quickStart">Skips splash screen if <see langword="true"/>.</param>
        /// <param name="collection">Opens the collection after all other collections and scenes flagged to open has.</param>
        public void Start(SceneCollection collection = null, bool ignoreDoNotOpen = false, bool playSplashScreen = true) =>
            Start(skipSplashScreen: !playSplashScreen, SceneManager.settings.buildUnitySplashScreenColor, 0, 1f, overrideOpenCollection: collection, ignoreDoNotOpen);

        /// <summary>
        /// <para>Restarts game and plays startup sequence again.</para>
        /// <para>Enters playmode if in editor.</para>
        /// </summary>
        public void Restart(bool playSplashScreen = false)
        {
            var props = Runtime.props;
            Start(skipSplashScreen: !playSplashScreen, props.fadeColor, props.initialFadeDuration, props.beforeSplashScreenFadeDuration, overrideOpenCollection: props.overrideOpenCollection, props.ignoreDoNotOpen);
        }

        void Start(bool skipSplashScreen, Color fadeColor, float initialFadeDuration, float beforeSplashScreenFadeDuration, SceneCollection overrideOpenCollection, bool ignoreDoNotOpen)
        {

            props = new StartProps()
            {
                skipSplashScreen = skipSplashScreen,
                fadeColor = fadeColor,
                initialFadeDuration = initialFadeDuration,
                beforeSplashScreenFadeDuration = beforeSplashScreenFadeDuration,
                overrideOpenCollection = overrideOpenCollection,
                ignoreDoNotOpen = ignoreDoNotOpen
            };

            if (Application.isPlaying)
                DoStart(props);
            else
            {
#if UNITY_EDITOR

                Coroutine().StartCoroutine();
                return;

                IEnumerator Coroutine()
                {

                    //Prevents a long delay in-between edit and play mode, that is otherwise avoided by just not pressing play button during, or directly after recompile
                    while (EditorApplication.isCompiling)
                        yield return null;

                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        yield break;

                    isBuildMode = true;

                    yield return SaveSceneSetup();

                    EditorApplication.EnterPlaymode();
                    wasStartedAsBuild = true;

                    //DoStart() is called from OnLoad()

                }

#endif
            }


        }

        void DoStart(StartProps props)
        {

            wasStartedAsBuild = true;

            var skipSplashScreen = props.skipSplashScreen || !Profile.current || !Profile.current.splashScreen;

            ActionUtility.Try(() => beforeStart?.Invoke());
            SceneOperation.Run(null).
            WithAction(new StartupAction(skipSplashScreen: skipSplashScreen, props.fadeColor, props.initialFadeDuration, props.beforeSplashScreenFadeDuration, props.overrideOpenCollection, props.ignoreDoNotOpen)).
            WithAction(() =>
            {
                runtime.isInitialized = true;
                ActionUtility.Try(() => afterStart?.Invoke());
            });

        }

        static void SetProfile()
        {

#if !UNITY_EDITOR
            Profile.SetProfile(SceneManager.settings.buildProfile);
#endif

            if (!Profile.current)
            {
                Debug.LogError("No build profile set!");
                NoProfileWarning.Show();
            }

        }

        #region No profile warning

        class NoProfileWarning : MonoBehaviour
        {

            public static void Show()
            {
                if (Profile.current)
                    return;
                var o = new GameObject("NoProfileWarning");
                o.AddComponent<NoProfileWarning>();
            }

            void Start()
            {

                DontDestroyOnLoad(gameObject);

                if (!Camera.main)
                {
                    var c = gameObject.AddComponent<Camera>();
                    c.clearFlags = CameraClearFlags.Color;
                    c.backgroundColor = Color.black;
                }

                Update();

            }

            void Update()
            {
                if (Profile.current)
                    Destroy(gameObject);
            }

            GUIContent content;
            GUIStyle style;
            void OnGUI()
            {

                if (content == null)
                    content = new GUIContent("No active profile");

                if (style == null)
                    style = new GUIStyle(GUI.skin.label) { fontSize = 22 };

                var size = style.CalcSize(content);
                GUI.Label(new Rect((Screen.width / 2) - (size.x / 2), (Screen.height / 2) - (size.y / 2), size.x, size.y), content, style);

            }

        }

        #endregion

        #endregion
        #region Scene setup

#if UNITY_EDITOR

        [Serializable]
        class Setup
        {

            public Setup(params SceneSetup[] scenes) =>
                this.scenes = scenes;

            public SceneSetup[] scenes;

        }

        static IEnumerator SaveSceneSetup()
        {

            if (EditorSceneManager.GetSceneManagerSetup().Any())
            {
                var json = JsonUtility.ToJson(new Setup(EditorSceneManager.GetSceneManagerSetup()));
                EditorPrefs.SetString("AdvancedSceneManager.BeforePlaymodeSceneSetup", json);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "AdvancedSceneManager";

            yield return new WaitForSeconds(0.1f);

        }

        static void RestoreSceneSetup()
        {

            EditorApplication.playModeStateChanged += mode =>
            {
                if (mode == PlayModeStateChange.EnteredEditMode)
                {

                    var json = EditorPrefs.GetString("AdvancedSceneManager.BeforePlaymodeSceneSetup");
                    var setup = JsonUtility.FromJson<Setup>(json);

                    if (setup?.scenes != null)
                        setup.scenes = setup.scenes.Where(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)).ToArray();

                    if ((setup?.scenes?.Any() ?? false))
                        EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);

                    EditorPrefs.SetString("AdvancedSceneManager.BeforePlaymodeSceneSetup", "");

                }
            };
        }

#endif

        #endregion
        #region Quit

        internal readonly List<IEnumerator> quitCallbacks = new List<IEnumerator>();

        /// <summary>Register a callback to be called before quit.</summary>
        public void RegisterQuitCallback(IEnumerator courutine) =>
            quitCallbacks.Add(courutine);

        /// <summary>Unregister a callback that was to be called before quit.</summary>
        public void UnregisterQuitCallback(IEnumerator courutine) =>
            quitCallbacks.Remove(courutine);

        /// <inheritdoc cref="QuitAction.CancelQuit"/>
        public void CancelQuit() =>
            QuitAction.CancelQuit();

        /// <inheritdoc cref="QuitAction.isQuitting"/>
        public bool isQuitting =>
            QuitAction.isQuitting;

        /// <summary>Quits the game, and calls quitCallbacks, optionally with a fade animation.</summary>
        public void Quit(bool fade = true, Color? fadeColor = null, float fadeDuration = 1) =>
            SceneOperation.Run(null).
                WithAction(new QuitAction(fade, fadeColor, fadeDuration));

        #endregion

    }

}
