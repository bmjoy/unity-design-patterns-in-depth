using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Linq;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Closes all scenes, except <see cref="DefaultSceneUtility"/>, regardless of whatever they are tracked or not. This is used in <see cref="StartupAction"/>, where we cannot be sure scenes are tracked yet.</summary>
    public class CloseAllUnityScenesAction : SceneAction
    {

        public CloseAllUnityScenesAction()
        { }

        public CloseAllUnityScenesAction(Func<UnityEngine.SceneManagement.Scene> ignore) =>
            this.ignore = ignore;

        readonly Func<UnityEngine.SceneManagement.Scene> ignore;

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            var ignore = this.ignore?.Invoke();

            var scenes = SceneUtility.GetAllOpenUnityScenes().
                Where(s => !ignore.HasValue || scene.path != ignore.Value.path).
                Where(s => !DefaultSceneUtility.IsDefaultScene(s)).
                ToArray();

            foreach (var scene in scenes)
            {
                DefaultSceneUtility.EnsureOpen();
                var so = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                if (so != null)
                    while (!so.isDone)
                    {
                        OnProgress(so.progress);
                        yield return null;
                    }
            }

            SceneManager.collection.SetNull();
            SceneManager.collection.Clear();
            SceneManager.standalone.Clear();

        }

    }

}
