using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using scene = UnityEngine.SceneManagement.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Loads a scene, but does not activate it. This is the same as preloading. See <see cref="SceneFinishLoadAction"/>.</summary>
    public class SceneLoadAction : OverridableAction<SceneLoadAction>
    {

        public SceneLoadAction(Models.Scene scene, SceneCollection collection = null)
        {

            this.scene = scene;
            this.collection = collection;

            if (scene == null)
            {
                Done();
                return;
            }

            if (!scene.isIncluded && !overrides.ContainsKey(scene.path))
            {
#if UNITY_EDITOR
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(scene.path);
#else
                UnityEngine.Object asset = null;
#endif
                Debug.LogError("The scene could not be opened since it is not added to build settings.", asset);
                Done();
                return;
            }

        }

        int currentLastIndex;
        protected override void BeforeDoAction(out bool exitOutEarly)
        {
            base.BeforeDoAction(out exitOutEarly);
            currentLastIndex = sceneManager.sceneCount;
        }

        public override IEnumerator DoNonOverridenAction(SceneManagerBase _sceneManager)
        {

            var async = sceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Additive);
            async.allowSceneActivation = false;

            while (async.progress < 0.9f)
            {
                OnProgress(async.progress / 0.9f);
                yield return null;
            }

            var openScene = GetOpenSceneInfo(_sceneManager, async);
            SetPersistentFlag(openScene);
            AddScene(openScene, _sceneManager);
            Done(openScene);

        }

        protected OpenSceneInfo GetOpenSceneInfo(SceneManagerBase _sceneManager, AsyncOperation asyncOperation)
        {

            //Get newly opened scene by path, if we can, since this is the most reliable option, since we don't allow multiple instances of same scene
            //(user could still open manually, but for now we'll consider that an unsupported use-case)

            scene s;
            if (!string.IsNullOrWhiteSpace(scene.path))
                s = sceneManager.GetSceneByPath(scene.path);
            else
            {

                //Fallback to using index, we want to avoid this since it may fail when opening multiple scenes in short succession,
                //but path obviously won't work for scenes that don't have a path,
                //the best would be to get scene directly from AsyncOperation, but it does not provide it, so here we are
                var index = Math.Min(currentLastIndex, sceneManager.sceneCount - 1);
                s = sceneManager.GetSceneAt(index);

            }

            Debug.Assert(scene.path == scene.path && s.name == scene.name, "Could not find unity scene after loading it.");
            return new OpenSceneInfo(scene, s, _sceneManager, asyncOperation);

        }

        public void SetPersistentFlag(OpenSceneInfo scene) =>
            PersistentUtility.Set(scene.unityScene.Value, scene.scene.tag.closeBehavior);

        public void AddScene(OpenSceneInfo scene, SceneManagerBase sceneManager)
        {
            sceneManager.Add(scene);
            sceneManager.RaiseSceneOpened(scene);
        }

    }

}
