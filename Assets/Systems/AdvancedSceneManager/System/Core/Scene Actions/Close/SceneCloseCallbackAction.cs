using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using System;
using System.Collections;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Calls all <see cref="ISceneClose.OnSceneClose"/> callbacks in scene.</summary>
    public class SceneCloseCallbackAction : OverridableAction<SceneCloseCallbackAction>
    {

        public SceneCloseCallbackAction(OpenSceneInfo scene, SceneCollection collection = null)
        {
            this.openScene = scene;
            this.collection = collection;
            if (openScene == null)
                Done();
        }

        public SceneCloseCallbackAction(Func<OpenSceneInfo> scene, SceneCollection collection = null)
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
            yield return CallbackUtility.Invoke<ISceneClose>().On(openScene);
            Done();
        }

    }

}
