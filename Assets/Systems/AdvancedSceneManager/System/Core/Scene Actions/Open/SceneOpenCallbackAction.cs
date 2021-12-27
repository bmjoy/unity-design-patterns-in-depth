using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using System;
using System.Collections;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Calls all <see cref="ISceneOpen.OnSceneOpen"/> callbacks in scene.</summary>
    public class SceneOpenCallbackAction : OverridableAction<SceneOpenCallbackAction>
    {

        public SceneOpenCallbackAction(OpenSceneInfo scene, SceneCollection collection = null)
        {
            this.openScene = scene;
            this.collection = collection;
            if (openScene == null)
                Done();
        }

        public SceneOpenCallbackAction(Func<OpenSceneInfo> scene, SceneCollection collection = null)
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
            yield return CallbackUtility.Invoke<ISceneOpen>().On(openScene);
            Done();
        }

    }

}
