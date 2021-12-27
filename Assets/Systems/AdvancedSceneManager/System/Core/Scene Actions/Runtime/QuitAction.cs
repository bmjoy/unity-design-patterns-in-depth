using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    public class QuitAction : SceneAction
    {

        /// <summary>
        /// <para>Cancels a quit in progress.</para>
        /// <para>Only usable during a RegisterQuitCallback() or while isQuitting is true.</para>
        /// </summary>
        public static void CancelQuit()
        {
            if (isQuitting)
                cancelQuit = true;
        }

        /// <summary>Gets whatever ASM is currently in the process of quitting.</summary>
        public static bool isQuitting { get; internal set; }

        static bool cancelQuit;

        public QuitAction(bool fade, Color? fadeColor = null, float fadeDuration = 1, bool callSceneCloseCallbacks = true, bool callCollectionCloseCallbacks = true)
        {
            this.fade = fade;
            color = fadeColor ?? Color.black;
            duration = fadeDuration;
            this.callSceneCloseCallbacks = callSceneCloseCallbacks;
            this.callCollectionCloseCallbacks = callCollectionCloseCallbacks;
            if (isQuitting)
                Done();
        }

        readonly bool fade;
        readonly Color color;
        readonly float duration;
        bool callSceneCloseCallbacks;
        bool callCollectionCloseCallbacks;

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            isQuitting = true;

            var wait = new List<IEnumerator>();

            SceneOperation<LoadingScreen> fadeScreen = null;
            if (fade)
            {
                yield return fadeScreen = LoadingScreenUtility.FadeOut(duration, color);
                wait.Add(new WaitForSecondsRealtime(0.5f));
            }

            wait.AddRange(SceneManager.runtime.quitCallbacks);

            if (callCollectionCloseCallbacks)
                wait.Add(CallCollectionCloseCallbacks());

            if (callSceneCloseCallbacks)
                wait.Add(CallSceneCloseCallbacks());

            yield return wait.WaitAll(isCancelled: () => cancelQuit);

            if (cancelQuit)
            {
                cancelQuit = false;
                isQuitting = false;
                if (fadeScreen?.value)
                    yield return LoadingScreenUtility.CloseLoadingScreen(fadeScreen.value);
                yield break;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif

        }

        IEnumerator CallSceneCloseCallbacks()
        {
            yield return CallbackUtility.Invoke<ISceneClose>().OnAllOpenScenes();
        }

        IEnumerator CallCollectionCloseCallbacks()
        {
            if (SceneManager.collection)
                yield return CallbackUtility.Invoke<ICollectionClose>().WithParam(SceneManager.collection).OnAllOpenScenes();
        }

    }

}
