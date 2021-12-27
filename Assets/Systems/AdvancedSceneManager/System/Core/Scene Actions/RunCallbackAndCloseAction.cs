using AdvancedSceneManager.Callbacks;
using System;
using System.Collections;
using UnityEngine;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Runs a callback and closes the scene.</summary>
    public class RunCallbackAndCloseAction<T> : SceneAction where T : MonoBehaviour
    {

        public RunCallbackAndCloseAction(T callback, Func<T, IEnumerator> runCallback, bool closeScene = true, bool isLoadingScreen = false)
        {
            this.closeScene = closeScene;
            this.callback = callback;
            this.runCallback = runCallback;
            this.isLoadingScreen = isLoadingScreen;
            if (callback == null)
                Done();
        }

        public bool closeScene { get; }
        public T callback { get; }
        public Func<T, IEnumerator> runCallback { get; }
        public bool isLoadingScreen { get; private set; }

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            if (callback == null)
            {
                Done();
                yield break;
            }

            if (isLoadingScreen && callback is LoadingScreen l)
                SceneManager.utility.RaiseLoadingScreenClosing(l);

            yield return runCallback?.Invoke(callback);

            if (isLoadingScreen && callback is LoadingScreen l1)
                SceneManager.utility.RaiseLoadingScreenClosed(l1);

            if (callback is MonoBehaviour mono && mono.gameObject.scene.IsValid())
                yield return new SceneCloseAction(mono.gameObject.Scene()).DoAction(_sceneManager);

            Done();

        }

    }

}
