using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Core.AsyncOperations;
using AdvancedSceneManager.Exceptions;
using AdvancedSceneManager.Models;
using Lazy.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static AdvancedSceneManager.SceneManager;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Manager for loading screens.</summary>
    public static class LoadingScreenUtility
    {

        #region Methods

        /// <inheritdoc cref="IsLoadingScreenOpen(OpenSceneInfo)">
        public static bool IsLoadingScreenOpen(scene scene) =>
            IsLoadingScreenOpen(scene.Scene());

        /// <inheritdoc cref="IsLoadingScreenOpen(OpenSceneInfo)">
        public static bool IsLoadingScreenOpen(Scene scene) =>
            IsLoadingScreenOpen(scene.GetOpenSceneInfo());

        /// <summary>Gets if this scene is a loading screen.</summary>
        public static bool IsLoadingScreenOpen(OpenSceneInfo scene) =>
            m_loadingScreens.Any(l => l && l.gameObject.scene.name == scene?.unityScene?.name);

        /// <summary>Gets if any loading screens are open.</summary>
        public static bool IsAnyLoadingScreenOpen =>
            loadingScreens.Where(l => l).Any();

        /// <summary>Shows the loading screen associated with this collection.</summary>
        /// <param name="timeout">The time to wait after opening scene before cancelling load screen.</param>
        public static SceneOperation<LoadingScreen> OpenLoadingScreen(SceneCollection collection, float? timeout = null, Action<LoadingScreen> callbackBeforeBegin = null)
        {

            if (!collection)
                return SceneOperation<LoadingScreen>.Done;
            else if (collection.loadingScreenUsage == LoadingScreenUsage.DoNotUse)
                return SceneOperation<LoadingScreen>.Done;
            else if (FindLoadingScreen(collection) is Scene scene)
                return OpenLoadingScreen(scene, timeout, callbackBeforeBegin);

            return SceneOperation<LoadingScreen>.Done;

        }

        /// <summary>Shows a loading screen.</summary>
        /// <param name="timeout">The time to wait after opening scene before cancelling load screen.</param>
        public static SceneOperation<LoadingScreen> OpenLoadingScreen(Scene scene, float? timeout = null, Action<LoadingScreen> callbackBeforeBegin = null) =>
            OpenLoadingScreen<LoadingScreen>(scene, timeout, callbackBeforeBegin);

        /// <summary>Shows a loading screen.</summary>
        /// <param name="timeout">The time to wait after opening scene before cancelling load screen.</param>
        public static SceneOperation<T> OpenLoadingScreen<T>(Scene scene, float? timeout = null, Action<T> callbackBeforeBegin = null) where T : LoadingScreen
        {

            if (!scene)
                return SceneOperation<T>.Done;

            var action = new OpenAndRunCallbackAction<T>(scene, (l) =>
            {
                callbackBeforeBegin?.Invoke(l);
                return l.OnOpen(utility.currentOperation);
            },
            timeout,
            isLoadingScreen: true,
            onMissingCallback: () => Debug.LogError($"No LoadingScreen script could be found in '{scene.name}.'"));

            return SceneOperation<T>.Run(standalone).
                WithAction(action).
                WithCallback(o => Add(action.callback)).
                Return(o => action.callback).
                IgnoreQueue();

        }

        /// <summary>Hide the loading screen.</summary>
        public static SceneOperation CloseLoadingScreen(LoadingScreen loadingScreen)
        {

            if (!loadingScreen)
                return SceneOperation.Done;

            Remove(loadingScreen);
            var action = new RunCallbackAndCloseAction<LoadingScreen>(loadingScreen, (l) => l.OnClose(utility.currentOperation), isLoadingScreen: true);
            return SceneOperation.Run(standalone).
                WithAction(action).
                IgnoreQueue();

        }

        /// <summary>Hide all loading screens.</summary>
        public static SceneOperation CloseAll()
        {

            if (!m_loadingScreens.Any())
                return SceneOperation.Done;

            var actions = m_loadingScreens.Select(loadingScreen => new RunCallbackAndCloseAction<LoadingScreen>(loadingScreen, (l) => l.OnClose(utility.currentOperation), isLoadingScreen: true));

            return SceneOperation.Run(standalone).
                WithAction(actions.ToArray()).
                WithAction(() => m_loadingScreens.Clear()).
                IgnoreQueue();

        }

        /// <summary>Find the loading screen that is associated with this collection.</summary>
        public static Scene FindLoadingScreen(SceneCollection collection)
        {

            if (!collection)
                return null;

            if (collection.loadingScreenUsage == LoadingScreenUsage.Override && collection.loadingScreen)
                return collection.loadingScreen;
            else if (collection.loadingScreenUsage != LoadingScreenUsage.DoNotUse && Profile.current && Profile.current.loadingScreen)
                return Profile.current.loadingScreen;

            return null;

        }

        #endregion
        #region DoAction utility

        #region Fade

        /// <summary>Finds the default fade loading screen.</summary>
        public static Scene fade =>
            assetManagement.scenes.FirstOrDefault(s => s?.path?.EndsWith("AdvancedSceneManager/System/Defaults/Fade Loading Screen.unity") ?? false);

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void OnLoad() =>
            CoroutineUtility.Run(when: () => Profile.current, action: () =>
            {
                if (fade && Profile.current)
                    Profile.current.SetStandalone(fade, true);
            });
#endif

        /// <summary>Fades screen out, performs action and fades in again.</summary>
        public static SceneOperation DoActionWithFade(Func<IEnumerator> action, float duration = 1, Color? color = null) =>
            DoAction(fade, coroutine: action, loadingScreenCallback: l => SetFadeProps(l, duration, color));

        /// <summary>Fades screen out, performs action and fades in again.</summary>
        public static SceneOperation DoActionWithFade(Action action, float duration = 1, Color? color = null) =>
            DoAction(fade, action: () => RunAction(action), loadingScreenCallback: l => SetFadeProps(l, duration, color));

        /// <summary>Fades out the screen.</summary>
        public static SceneOperation<LoadingScreen> FadeOut(float duration = 1, Color? color = null) =>
            OpenLoadingScreen<LoadingScreen>(fade, callbackBeforeBegin: l => SetFadeProps(l, duration, color));

        /// <summary>Fades in the screen.</summary>
        public static SceneOperation FadeIn(LoadingScreen loadingScreen, float duration = 1, Color? color = null)
        {
            SetFadeProps(loadingScreen, duration, color);
            return CloseLoadingScreen(loadingScreen);
        }

        static void SetFadeProps(LoadingScreen loadingScreen, float duration, Color? color)
        {
            if (loadingScreen is DefaultLoadingScreen fade)
            {
                fade.duration = duration;
                fade.color = color ?? Color.black;
            }
        }

        #endregion

        /// <summary>Opens loading screen, performs action and hides loading screen again.</summary>
        public static SceneOperation DoAction(Scene scene, Action action, Action<LoadingScreen> loadingScreenCallback) =>
            DoAction(scene, action: () => RunAction(action), loadingScreenCallback);

        /// <summary>
        /// <para>Opens loading screen, performs action and hides loading screen again.</para>
        /// <para>Throws <see cref="OpenSceneException"/> if <paramref name="scene"/> is null.</para>
        /// </summary>
        public static SceneOperation DoAction(Scene scene, Func<IEnumerator> coroutine, Action<LoadingScreen> loadingScreenCallback)
        {

            if (!scene)
                throw new OpenSceneException(scene, message: "Scene was null");

            var operation = SceneOperation.Run(standalone).
                WithLoadingScreen(scene).
                WithLoadingScreenCallback(loadingScreenCallback).
                WithAction(coroutine).
                IgnoreQueue();

            return operation;

        }

        static Func<IEnumerator> RunAction(Action action)
        {
            return () => Run();
            IEnumerator Run()
            {
                action?.Invoke();
                yield break;
            }
        }

        #endregion
        #region List over open loading screens

        static readonly List<LoadingScreen> m_loadingScreens = new List<LoadingScreen>();

        /// <summary>The currently open loading screens.</summary>
        public static ReadOnlyCollection<LoadingScreen> loadingScreens { get; }

        static LoadingScreenUtility() =>
            loadingScreens = new ReadOnlyCollection<LoadingScreen>(m_loadingScreens);

        static void Add(LoadingScreen loadingScreen)
        {

            if (!loadingScreen || !(loadingScreen.Scene()?.unityScene.HasValue ?? false))
                return;

            PersistentUtility.Set(loadingScreen.Scene().unityScene.Value, SceneCloseBehavior.KeepOpenAlways);
            m_loadingScreens.Add(loadingScreen);
            loadingScreen.canvas.PutOnTop();
            loadingScreen.onDestroy += Remove;

        }

        static void Remove(LoadingScreen loadingScreen)
        {

            var scene = loadingScreen ? loadingScreen.Scene() : null;
            if (scene?.unityScene.HasValue ?? fade)
                return;

            PersistentUtility.Unset(scene.unityScene.Value);
            _ = m_loadingScreens.Remove(loadingScreen);
            _ = m_loadingScreens.RemoveAll(l => !l);
            CanvasSortOrderUtility.Remove(loadingScreen.canvas);

        }

        #endregion

    }

}
