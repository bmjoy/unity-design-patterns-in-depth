#pragma warning disable IDE0083 // Use pattern matching

using AdvancedSceneManager.Models;
using System;
using System.Collections;
using UnityEngine;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Finish load of a scene that was loaded, but not activated. See <see cref="SceneLoadAction"/>.</summary>
    public class SceneFinishLoadAction : OverridableAction<SceneFinishLoadAction>
    {

        public SceneFinishLoadAction(OpenSceneInfo scene, SceneCollection collection = null)
        {
            this.openScene = scene;
            this.collection = collection;
            if (openScene == null)
                Done();
        }

        public SceneFinishLoadAction(Func<OpenSceneInfo> scene, SceneCollection collection = null)
        {
            lazyScene = scene;
            this.collection = collection;
            if (lazyScene == null)
                Done();
        }

        protected override void BeforeDoAction(out bool exitOutEarly)
        {
            if (lazyScene != null)
                openScene = lazyScene.Invoke();
            exitOutEarly = openScene == null;
        }

        public override IEnumerator DoNonOverridenAction(SceneManagerBase _sceneManager)
        {

            if (!(openScene?.isOpen ?? false))
            {
                Done();
                yield break;
            }

            if (!(openScene.asyncOperation is AsyncOperation async))
                yield break;

            async.allowSceneActivation = true;
            while (!async.isDone)
            {
                OnProgress(async.progress - 0.9f);
                yield return null;
            }

            yield return null;

            openScene.asyncOperation = null;
            Done(openScene);

        }

    }

}
