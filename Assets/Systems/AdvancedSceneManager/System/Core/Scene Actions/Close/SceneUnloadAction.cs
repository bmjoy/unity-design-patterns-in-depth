using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections;

using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Finish load of a scene that was loaded, but not activated. See <see cref="SceneLoadAction"/>.</summary>
    public class SceneUnloadAction : OverridableAction<SceneUnloadAction>
    {

        public SceneUnloadAction(OpenSceneInfo scene, SceneCollection collection = null)
        {
            this.openScene = scene;
            this.collection = collection;
            if (scene == null)
                Done();
        }

        public SceneUnloadAction(Func<OpenSceneInfo> scene, SceneCollection collection = null)
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

            if (!(openScene?.unityScene?.IsValid() ?? false))
                yield break;

            var async = sceneManager.UnloadSceneAsync(openScene.unityScene.Value);

            while (!(async?.isDone ?? true))
            {
                OnProgress(async.progress);
                yield return null;
            }

            UnsetPersistentFlag(openScene);
            Remove(openScene, SceneManager.standalone);
            Remove(openScene, SceneManager.collection);
            Done(openScene);

        }

        public void UnsetPersistentFlag(OpenSceneInfo scene) =>
            PersistentUtility.Unset(scene.unityScene.Value);

        public void Remove(OpenSceneInfo scene, SceneManagerBase sceneManager)
        {
            sceneManager.Remove(scene);
            sceneManager.RaiseSceneClosed(scene);
            scene.OnSceneClosed();
        }

    }

}
