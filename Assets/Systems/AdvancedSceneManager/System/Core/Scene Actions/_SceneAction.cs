using AdvancedSceneManager.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>The base class of all scene actions. The scene actions perform an specific action on a <see cref="Scene"/> when contained within a <see cref="SceneOperation"/>.</summary>
    public abstract class SceneAction
    {

        #region Addressables support

        /// <summary>Called from addressables support package.</summary>
        internal void _Done(OpenSceneInfo openScene = null) =>
            Done(openScene);

        /// <summary>Called from addressables support package.</summary>
        internal void _Done() =>
            Done();

        /// <summary>Called from addressables support package.</summary>
        internal void SetProgress(float progress) =>
            this.progress = progress;

        #endregion

        public abstract IEnumerator DoAction(SceneManagerBase _sceneManager);

        public OpenSceneInfo openScene { get; set; }

        /// <summary>The scene this <see cref="SceneAction"/> is performing its action on.</summary>
        public Scene scene { get; protected set; }

        /// <summary>The collection that is being opened. null if stand-alone.</summary>
        public SceneCollection collection { get; protected set; }

        /// <summary>The progress of this scene action.</summary>
        public float progress { get; private set; }

        /// <summary>Is this scene action done?</summary>
        public bool isDone { get; protected set; }

        readonly List<Action> callbacks = new List<Action>();

        /// <summary>Register a callback when scene action is done.</summary>
        public void RegisterCallback(Action action) => callbacks.Add(action);

        /// <summary>Remove an registered callback when scene action is done.</summary>
        public void UnregisterCallback(Action action) => callbacks.Remove(action);

        readonly List<Action<float>> onProgress = new List<Action<float>>();

        public void OnProgressCallback(Action<float> callback)
        {
            if (!onProgress.Contains(callback))
                onProgress.Add(callback);
        }

        protected void OnProgress(float progress)
        {
            this.progress = progress;
            foreach (var callback in onProgress)
                callback?.Invoke(progress);
        }

        protected virtual void Done()
        {
            isDone = true;
            OnProgress(1);
            callbacks.ForEach(a => a?.Invoke());
        }

        protected virtual void Done(OpenSceneInfo openScene)
        {
            this.openScene = openScene;
            Done();
        }

        public override string ToString() =>
            GetType().Name + ": " +
            (scene ? scene.name : openScene?.unityScene?.name ?? "");

    }

}
