using System;
using System.Collections;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Runs a coroutine.</summary>
    public class CallbackAction : SceneAction
    {

        readonly Func<IEnumerator> callback;
        readonly Action action;

        public CallbackAction(Action action)
        {
            this.action = action;
        }

        public CallbackAction(Func<IEnumerator> callback)
        {
            this.callback = callback;
        }

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {
            action?.Invoke();
            yield return callback?.Invoke();
        }

    }

}
