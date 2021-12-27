using System.Linq;
using AdvancedSceneManager.Models;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>
    /// <para>Closes a scene. This is a proxy for running the following actions in order:</para>
    /// <para><see cref="SceneCloseCallbackAction"/>, call callbacks.</para>
    /// <para><see cref="SceneUnloadAction"/>, unload scenes.</para>
    /// </summary>
    public class SceneCloseAction : AggregateAction
    {

        public SceneCloseAction(OpenSceneInfo scene, SceneCollection collection = null) :
            base(
                new SceneCloseCallbackAction(scene, collection),
                new SceneUnloadAction(scene, collection))
        {
            this.openScene = scene;
            this.collection = collection;
        }

        protected override void OnDone()
        {
            openScene = actions.FirstOrDefault().openScene;
        }

    }

}
