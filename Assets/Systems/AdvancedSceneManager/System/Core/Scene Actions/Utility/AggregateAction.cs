using System;
using System.Collections;
using System.Linq;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Runs one ore more actions.</summary>
    public class AggregateAction : SceneAction
    {

        public SceneAction[] actions { get; }

        public AggregateAction(params SceneAction[] actions)
        {

            this.actions = actions.OfType<SceneAction>().ToArray();
            if (actions?.Length == 0)
                Done();

        }

        /// <param name="onDone">Make sure to set properties such as openScene here, if needed.</param>
        public AggregateAction(Action onDone, params SceneAction[] actions) :
            this(actions) =>
                _onDone = onDone;

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            foreach (var action in actions)
            {
                //this.action = action;
                action.OnProgressCallback(OnProgressChanged);
                yield return action.DoAction(_sceneManager);
            }

            _onDone?.Invoke();
            OnDone();

        }

        void OnProgressChanged(float progress) =>
            OnProgress(actions.Sum(a => a.progress) / actions.Length);

        readonly Action _onDone;

        protected virtual void OnDone()
        { }

    }

}
