using AdvancedSceneManager.Core.AsyncOperations;
using AdvancedSceneManager.Utility;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

using static AdvancedSceneManager.SceneManager;

using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;
using AdvancedSceneManager.Core;
using System.Collections;
using Lazy.Utility;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace AdvancedSceneManager
{

    namespace Utility
    {

        //This is defined here, instead of its own file, because unity will display it in the object picker when searching for scene helper

        /// <summary>An helper class to make working with preloaded scenes easier, contains method for finish loading and discarding preloaded scene.</summary>
        public class PreloadedSceneHelper
        {

            public PreloadedSceneHelper(OpenSceneInfo scene, bool hasRunCallbacks)
            {
                this.scene = scene;
                this.hasRunCallbacks = hasRunCallbacks;
            }

            /// <summary>The scene that was preloaded.</summary>
            public OpenSceneInfo scene { get; private set; }

            /// <summary>Gets whatever the scene is still preloaded.</summary>
            public bool isStillPreloaded => scene?.isPreloaded ?? false;

            /// <summary>Gets whatever <see cref="Callbacks.ISceneOpen.OnSceneOpen"/> callbacks has been called.</summary>
            public bool hasRunCallbacks { get; private set; }

            /// <summary>Finishes loading scene.</summary>
            public SceneOperation<OpenSceneInfo> FinishLoading()
            {

                if (!isStillPreloaded || scene.sceneManager == null)
                    return SceneOperation<OpenSceneInfo>.Done;

                var operation = SceneOperation<OpenSceneInfo>.Run(standalone);

                if (!hasRunCallbacks)
                    operation.WithAction(new SceneOpenCallbackAction(scene));
                hasRunCallbacks = true;
                operation.WithAction(new SceneFinishLoadAction(scene));

                return operation;

            }

            /// <summary>Closes the scene.</summary>
            public SceneOperation Discard()
            {

                var operation = SceneOperation.Run(standalone);
                if (hasRunCallbacks)
                    operation.WithAction(new SceneCloseCallbackAction(scene));
                operation.WithAction(new SceneUnloadAction(scene));

                return operation;

            }

        }

    }

    namespace Core
    {

        /// <summary>
        /// <para>The manager for stand-alone scenes.</para>
        /// <para>Usage: <see cref="standalone"/>.</para>
        /// </summary>
        public class StandaloneManager : SceneManagerBase
        {

            internal void OnLoad() =>
                RegisterUnityCallback();

            #region Unity scene event hooks

            List<string> multipleInstanceWarnings = new List<string>();
            bool hasRegisteredCallbacks;
            void RegisterUnityCallback()
            {

                if (hasRegisteredCallbacks)
                    return;
                hasRegisteredCallbacks = true;

                sceneManager.sceneLoaded += OnSceneLoaded;
                sceneManager.sceneUnloaded += OnSceneUnloaded;

#if UNITY_EDITOR

                if (Application.isPlaying)
                    return;

                EditorSceneManager.sceneOpened += OnSceneOpened;
                EditorSceneManager.sceneClosed += OnSceneClosed;

                void OnSceneOpened(scene scene, OpenSceneMode openMode) =>
                    OnSceneLoaded(scene, openMode == OpenSceneMode.Single ? LoadSceneMode.Single : LoadSceneMode.Additive);

                void OnSceneClosed(scene scene) =>
                    OnSceneUnloaded(scene);

#endif

                void OnSceneLoaded(scene scene, LoadSceneMode mode)
                {

                    if (Utility.SceneUtility.GetAllOpenUnityScenes().GroupBy(s => s.path).Any(g => g.Count() > 1))
                    {
                        if (!multipleInstanceWarnings.Contains(scene.path))
                            Debug.LogWarning("Scene is opened more than once, this is not supported and may result in first instance being tracked twice and second one not tracked at all.");
                        multipleInstanceWarnings.Add(scene.path);
                        return;
                    }

                    Coroutine().StartCoroutine();
                    IEnumerator Coroutine()
                    {

                        yield return new WaitForSeconds(1);

                        while (SceneOperation.queue.Any())
                            yield return null;

                        if (!(utility.FindOpenScene(scene) is OpenSceneInfo))
                        {

                            if (string.IsNullOrEmpty(scene.path))
                                yield break;

                            var Scene = assetManagement.scenes.Find(scene.path);
                            if (!Scene)
                            {
                                Debug.LogError("A scene was opened from outside Advanced Scene Manager, but no associated Scene asset could be found.");
                                yield break;
                            }

                            if (mode == LoadSceneMode.Single)
                            {
                                collection.SetNull();
                                collection.Clear();
                                Clear();
                            }

                            Add(new OpenSceneInfo(Scene, scene, standalone));

                        }

                    }

                }

                void OnSceneUnloaded(scene scene)
                {

                    multipleInstanceWarnings.Remove(scene.path);

                    Coroutine().StartCoroutine();
                    IEnumerator Coroutine()
                    {

                        while (SceneOperation.queue.Any())
                            yield return null;

                        if (utility.FindOpenScene(scene) is OpenSceneInfo openScene)
                        {

                            if (standalone.IsOpen(openScene.scene))
                                standalone.Remove(openScene);

                            if (collection.IsOpen(openScene.scene))
                                collection.Remove(openScene);

                        }

                    }

                }

            }

            #endregion

            /// <summary>
            /// <para>Close existing scenes and open the specified one.</para>
            /// <para>This will close the current collection.</para>
            /// </summary>
            public SceneOperation<OpenSceneInfo> OpenSingle(Scene scene) =>
                SceneOperation<OpenSceneInfo>.Run(this).
                    Close(utility.openScenes).
                    Open(scene).
                    WithCallback(collection.SetNull).
                    Return(o => o.FindLastAction<SceneOpenAction>()?.openScene);

            /// <summary>
            /// <para>Preloads the scene.</para>
            /// <para>Use <see cref="PreloadedSceneHelper.FinishLoading"/> or <see cref="Open(OpenSceneInfo)"/> to finish loading scene.</para> 
            /// </summary>
            public SceneOperation<PreloadedSceneHelper> Preload(Scene scene, bool doCallbacks = false)
            {

                var operation = SceneOperation<PreloadedSceneHelper>.Run(this);

                var loadAction = new SceneLoadAction(scene);
                operation.WithAction(loadAction);

                if (doCallbacks)
                    operation.WithAction(new SceneOpenCallbackAction(() => loadAction.openScene));

                operation.Return(o => new PreloadedSceneHelper(loadAction.openScene, doCallbacks));

                return operation;

            }

        }

    }

}
