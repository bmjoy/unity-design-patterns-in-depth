using AdvancedSceneManager.Callbacks;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Opens a scene and finds a script of the specified type, and runs a callback, scene is closed if not found.</summary>
    public class OpenAndRunCallbackAction<T> : SceneAction
    {

        public const float defaultTimeout = 1f;

        public OpenAndRunCallbackAction(Scene scene, Func<T, IEnumerator> runCallback, float? timeout = null, bool isLoadingScreen = false, Action onMissingCallback = null)
        {

            this.scene = scene;
            this.timeout = timeout ?? defaultTimeout;
            this.runCallback = runCallback;
            this.isLoadingScreen = isLoadingScreen;
            this.onMissingCallback = onMissingCallback;

            if (!scene)
                Done();

        }

        internal OpenAndRunCallbackAction(T callback)
        {

            this.callback = callback;
            if (callback == null)
                Done();

        }

        public Action onMissingCallback { get; private set; }
        public T callback { get; private set; }
        public Func<T, IEnumerator> runCallback { get; private set; }
        public bool isLoadingScreen { get; private set; }

        readonly float timeout;

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            if (!scene)
                yield break;

            var openAction = new SceneOpenAction(scene);
            yield return openAction.DoAction(_sceneManager);

            if (!(openAction.openScene?.unityScene?.IsValid() ?? false))
                yield break;

            var uScene = openAction.openScene?.unityScene.Value;

            var time = 0f;
            while (!uScene.Value.GetRootGameObjects().Any() && time < timeout)
            {
                yield return null;
                time += Time.deltaTime;
            }

            callback = uScene.Value.GetRootGameObjects().SelectMany(s => s.GetComponentsInChildren<MonoBehaviour>(true)).OfType<T>().FirstOrDefault();
            if (callback != null)
            {

                if (isLoadingScreen && callback is LoadingScreen l)
                    SceneManager.utility.RaiseLoadingScreenOpening(l);

                yield return runCallback?.Invoke(callback);

                if (isLoadingScreen && callback is LoadingScreen l1)
                    SceneManager.utility.RaiseLoadingScreenOpened(l1);

            }
            else
            {
                onMissingCallback?.Invoke();
                yield return new SceneCloseAction(openAction.openScene).DoAction(SceneManager.standalone);
            }

            Done();

        }

    }

}
