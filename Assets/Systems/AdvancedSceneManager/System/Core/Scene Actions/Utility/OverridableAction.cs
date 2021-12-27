using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Core.AsyncOperations
{
    /// <summary>An action that can have its functionality overriden, this is needed to provide addressables support. When overriding default ASM actions, use caution since this will obviously lead to undocumented behavior.</summary>
    public abstract class OverridableAction<T> : SceneAction where T : OverridableAction<T>
    {

        static OverridableAction() =>
            overrides = new ReadOnlyDictionary<string, Func<SceneManagerBase, T, IEnumerator>>(overridesList);

        static readonly Dictionary<string, Func<SceneManagerBase, T, IEnumerator>> overridesList = new Dictionary<string, Func<SceneManagerBase, T, IEnumerator>>();

        /// <summary>All overriden scenes.</summary>
        public static ReadOnlyDictionary<string, Func<SceneManagerBase, T, IEnumerator>> overrides { get; }

        /// <summary>Overrides behavior for the specified scene.</summary>
        public static void Override(string scene, Func<SceneManagerBase, T, IEnumerator> coroutine) =>
            overridesList.Set(scene, coroutine);

        /// <summary>Clears overrides for the specified scene.</summary>
        public static void ClearOverride(string scene) =>
            Override(scene, null);

        /// <summary>Clears all overrides.</summary>
        public static void ClearOverrides() =>
            overridesList.Clear();

        /// <summary>
        /// <para>This must not be overriden since <see cref="OverridableAction{T}"/> does not work otherwise</para> 
        /// <para>Override <see cref="DoNonOverridenAction(SceneManagerBase)"/> instead.</para>
        /// </summary>
        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            BeforeDoAction(out var exitOutEarly);
            if (exitOutEarly)
            {
                Done();
                yield break;
            }

            var path = "";
            if (scene)
                path = scene.path;
            else if (openScene != null)
                path = openScene.scene.path;

            if (overridesList.TryGetValue(path, out var coroutine))
                yield return coroutine.Invoke(_sceneManager, (T)this);
            else
                yield return DoNonOverridenAction(_sceneManager);

        }

        public abstract IEnumerator DoNonOverridenAction(SceneManagerBase _sceneManager);

        protected virtual void BeforeDoAction(out bool exitOutEarly)
        { exitOutEarly = false; }

        protected Func<OpenSceneInfo> lazyScene { get; set; }

        public override string ToString() =>
            GetType().Name + ": " +
            (scene ? scene.name : openScene?.unityScene?.name
            ?? (lazyScene != null ? "(to be evaluated)" : ""));

    }

}
