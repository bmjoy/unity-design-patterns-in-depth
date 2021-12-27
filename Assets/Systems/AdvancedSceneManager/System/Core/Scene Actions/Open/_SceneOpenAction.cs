using System.Linq;
using AdvancedSceneManager.Models;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>
    /// <para>Opens a scene. This is a proxy for running the following actions in order:</para>
    /// <para><see cref="SceneLoadAction"/>, preload scene.</para>
    /// <para><see cref="SceneFinishLoadAction"/>, activate scene.</para>
    /// <para><see cref="SceneOpenCallbackAction"/>, call callbacks.</para>
    /// </summary>
    public class SceneOpenAction : AggregateAction
    {

        public SceneOpenAction(Models.Scene scene, SceneCollection collection = null) :
            base(
                new SceneLoadAction(scene, collection),
                new SceneFinishLoadAction(() => scene.GetOpenSceneInfo(), collection),
                new SceneOpenCallbackAction(() => scene.GetOpenSceneInfo(), collection))
        {
            this.scene = scene;
            this.collection = collection;
        }

        protected override void OnDone()
        {
            openScene = actions.FirstOrDefault().openScene;
        }

    }

}
