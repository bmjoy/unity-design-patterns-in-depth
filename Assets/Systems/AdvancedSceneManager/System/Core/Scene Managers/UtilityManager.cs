using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static AdvancedSceneManager.SceneManager;
using Object = UnityEngine.Object;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Core
{

    /// <summary>
    /// <para>An utility scene manager that helps with actions that might relate to either <see cref="collection"/> or <see cref="standalone"/> managers.</para>
    /// <para>Usage: <see cref="utility"/>.</para>
    /// </summary>
    public class UtilitySceneManager
    {

        /// <summary>Gets all currently open scenes.</summary>
        public IEnumerable<OpenSceneInfo> openScenes =>
            collection.openScenes.Concat(standalone.openScenes);

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {


            RegisterCallbackHandlers();
            SetupQueue();

            //This code is redundant when first starting, but when scripts recompile in playmode and execution continues,
            //they disappear, this fixes that (and there seem to be no issues first time either)
            if (Application.isPlaying && !runtime.wasStartedAsBuild)
                foreach (var scene in SceneUtility.GetAllOpenUnityScenes())
                    if (scene.IsValid() && !DefaultSceneUtility.IsDefaultScene(scene) && assetManagement.FindSceneByPath(scene.path) is Scene s && s && !utility.openScenes.Any(s1 => s1.unityScene == scene))
                        standalone.Add(new OpenSceneInfo(s, scene, standalone));

        }

        /// <summary>Performs a callback on the scripts on all open scenes.</summary>
        public IEnumerator DoSceneCallback<T>(Func<T, IEnumerator> action) =>
            CallbackUtility.Invoke<T>().WithCallback(action).OnAllOpenScenes();

        #region Queue

        /// <inheritdoc cref="SceneOperationBase{TSelf}.queueEmpty"/>
        public event Action queueEmpty;

        /// <inheritdoc cref="SceneOperationBase{TSelf}.isBusy"/>
        public bool isBusy => SceneOperation.isBusy;

        /// <summary>The currently running scene operations.</summary>
        public IEnumerable<ISceneOperation> runningOperations =>
            SceneOperation.running;

        /// <summary>Gets the current scene operation queue.</summary>
        public IEnumerable<ISceneOperation> queuedOperations =>
            SceneOperation.queue;

        /// <summary>Gets the current active operation in the queue.</summary>
        public ISceneOperation currentOperation =>
            SceneOperation.queue.FirstOrDefault();

        static void SetupQueue()
        {

            SceneOperation.queueEmpty += () =>
            {
                //Move scenes that remained open when the parent collection closed to standalone
                CarryOverPersistentCollectionScenes();
                utility.queueEmpty?.Invoke();
            };

        }

        /// <summary>Move persistent scenes that remained in <see cref="collection"/> to <see cref="standalone"/>.</summary>
        static void CarryOverPersistentCollectionScenes()
        {
            var scenes = collection.openScenes.Where(s => !collection || !collection.current.scenes.Contains(s.scene)).ToArray();
            foreach (var scene in scenes)
            {
                collection.Remove(scene);
                standalone.Add(scene);
            }
        }

        #endregion
        #region Scene Open / Close callbacks

        public delegate void ActiveSceneChangedHandler(OpenSceneInfo previousScene, OpenSceneInfo activeScene);

        /// <summary>Occurs when the active scene changes.</summary>
        public event ActiveSceneChangedHandler ActiveSceneChanged;

        /// <summary>Occurs when a scene is opened.</summary>
        public event Action<OpenSceneInfo, SceneManagerBase> SceneOpened;

        /// <summary>Occurs when a scene is closed.</summary>
        public event Action<OpenSceneInfo, SceneManagerBase> SceneClosed;

        /// <summary>Occurs when a loading screen is about to be opened.</summary>
        public event Action<LoadingScreen> LoadingScreenOpening;

        /// <summary>Occurs when a loading screen has opened.</summary>
        public event Action<LoadingScreen> LoadingScreenOpened;

        /// <summary>Occurs when a loading screen is about to close.</summary>
        public event Action<LoadingScreen> LoadingScreenClosing;

        /// <summary>Occurs when a loading screen has closed.</summary>
        public event Action<LoadingScreen> LoadingScreenClosed;

        internal void RaiseLoadingScreenOpening(LoadingScreen loadingScreen) =>
            LoadingScreenOpening?.Invoke(loadingScreen);

        internal void RaiseLoadingScreenOpened(LoadingScreen loadingScreen) =>
            LoadingScreenOpened?.Invoke(loadingScreen);

        internal void RaiseLoadingScreenClosing(LoadingScreen loadingScreen) =>
            LoadingScreenClosing?.Invoke(loadingScreen);

        internal void RaiseLoadingScreenClosed(LoadingScreen loadingScreen) =>
            LoadingScreenClosed?.Invoke(loadingScreen);

        static readonly Dictionary<ISceneObject, List<(Action action, bool persistent)>> sceneOpenCallbacks = new Dictionary<ISceneObject, List<(Action action, bool persistent)>>();
        static readonly Dictionary<ISceneObject, List<(Action action, bool persistent)>> sceneCloseCallbacks = new Dictionary<ISceneObject, List<(Action action, bool persistent)>>();

        /// <summary>Registers a callback for when a scene or collection has opened, or closed, the callback is removed once it has been called, unless persistent is true.</summary>
        public void RegisterOpenCallback<T>(T scene, Action onOpen = null, Action onClose = null, bool persistent = false) where T : Object, ISceneObject
        {
            if (scene)
            {
                if (onOpen != null)
                {
                    UnregisterCallback(scene, onOpen: onOpen);
                    sceneOpenCallbacks.Add(scene, (onOpen, persistent));
                }
                if (onClose != null)
                {
                    UnregisterCallback(scene, onClose: onClose);
                    sceneCloseCallbacks.Add(scene, (onClose, persistent));
                }
            }
        }

        /// <summary>Unregisters a callback.</summary>
        public void UnregisterCallback<T>(T scene, Action onOpen = null, Action onClose = null) where T : Object, ISceneObject
        {
            if (scene)
            {
                sceneOpenCallbacks.GetValue(scene)?.RemoveAll(o => o.action == onOpen);
                sceneCloseCallbacks.GetValue(scene)?.RemoveAll(o => o.action == onClose);
            }
        }

        /// <summary>Register handlers for scene open / close callbacks.</summary>
        static void RegisterCallbackHandlers()
        {

            standalone.sceneOpened += s => OnSceneOpened(s, standalone);
            collection.sceneOpened += s => OnSceneOpened(s, collection);

            standalone.sceneClosed += s => OnSceneClosed(s, standalone);
            collection.sceneClosed += s => OnSceneClosed(s, collection);

            collection.opened += c => OnSceneOpened(c, collection);
            collection.closed += c => OnSceneOpened(c, collection);

            sceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            void OnSceneOpened(object scene, SceneManagerBase sceneManager)
            {

                ISceneObject obj = null;
                if (scene is OpenSceneInfo info)
                    obj = info.scene;
                else if (scene is SceneCollection collection)
                    obj = collection;
                else
                    return;

                if (sceneOpenCallbacks.TryGetValue(obj, out var list))
                    foreach (var action in list.ToArray())
                    {
                        action.action?.Invoke();
                        if (!action.persistent)
                            sceneOpenCallbacks.GetValue(obj).Remove(action);
                    }

                if (scene is OpenSceneInfo s)
                    utility.SceneOpened?.Invoke(s, sceneManager);

            }

            void OnSceneClosed(object scene, SceneManagerBase sceneManager)
            {

                ISceneObject obj = null;
                if (scene is OpenSceneInfo info)
                    obj = info.scene;
                else if (scene is SceneCollection collection)
                    obj = collection;

                if (sceneCloseCallbacks.TryGetValue(obj, out var list))
                    foreach (var action in list.ToArray())
                    {
                        action.action?.Invoke();
                        if (!action.persistent)
                            sceneCloseCallbacks.GetValue(obj).Remove(action);
                    }

                if (scene is OpenSceneInfo s)
                    utility.SceneClosed?.Invoke(s, sceneManager);

            }

            void SceneManager_activeSceneChanged(scene oldScene, scene newScene)
            {
                DefaultSceneUtility.OnBeforeActiveSceneChanged(oldScene, newScene, out var cancel);
                if (!cancel)
                    utility.ActiveSceneChanged?.Invoke(oldScene.Scene(), newScene.Scene());
            }

        }

        #endregion
        #region Reopen

        /// <summary>Reopen a scene regardless of whatever it is associated with a collection, or is was opened as stand-alone.</summary>
        public SceneOperation<OpenSceneInfo> Reopen(OpenSceneInfo scene)
        {
            if (scene == null)
                return SceneOperation<OpenSceneInfo>.Done;
            if (standalone.IsOpen(scene.scene)) return standalone.Reopen(scene);
            if (collection.IsOpen(scene.scene)) return collection.Reopen(scene);
            return SceneOperation<OpenSceneInfo>.Done;
        }

        #endregion
        #region Close

        /// <summary>Closes a scene regardless of whatever it is associated with a collection, or is was opened as stand-alone.</summary>
        public SceneOperation Close(OpenSceneInfo scene)
        {
            if (scene == null)
                return SceneOperation.Done;
            if (standalone.IsOpen(scene.scene)) return standalone.Close(scene);
            if (collection.IsOpen(scene.scene)) return collection.Close(scene);
            return SceneOperation.Done;
        }

        /// <summary>Closes all scenes.</summary>
        public SceneOperation CloseAll()
        {
            PersistentUtility.UnsetAll();
            return SceneOperation.Run(collection.current ? (SceneManagerBase)collection : standalone).
                Close(standalone.openScenes).
                Close(openScenes).
                WithCallback(() =>
                {
                    collection.SetNull();
                });
        }

        #endregion
        #region Toggle

        /// <summary>Toggles the scene open or closed, if the scene is part of the current collection, then the scene will be toggled within the collection, otherwise, it will be toggled as a stand-alone scene.</summary>
        /// <param name="enabled">If null, the scene will be toggled on or off depending on whatever the scene is open or not. Pass a value to ensure that the scene either open or closed.</param>
        public SceneOperation Toggle(Scene scene, bool? enabled = null)
        {
            if (collection.current && collection.current.scenes.Any(s => s.path == scene.path))
                return collection.Toggle(scene, enabled);
            else
                return standalone.Toggle(scene, enabled);
        }

        /// <summary>Toggles the scene open or closed, if the scene is part of the current collection, then the scene will be toggled within the collection, otherwise, it will be toggled as a stand-alone scene.</summary>
        /// <param name="enabled">If null, the scene will be toggled on or off depending on whatever the scene is open or not. Pass a value to ensure that the scene either open or closed.</param>
        public SceneOperation Toggle(scene scene, bool? enabled = null)
        {

            if (!scene.IsValid())
                return SceneOperation.Done;

            if (!(SceneManager.assetManagement.FindSceneByPath(scene.path) is Scene scene1))
                return SceneOperation.Done;

            if (collection.current && collection.current.scenes.Any(s => s.path == scene.path))
                return collection.Toggle(scene1, enabled);
            else
                return standalone.Toggle(scene1, enabled);

        }

        #endregion
        #region IsOpen / FindOpenScene

        /// <summary>Gets whatever the scene is open, either as part of a collection, or as stand-alone.</summary>
        public IsOpenReturnValue IsOpen(Scene scene) =>
            (
                standalone.IsOpen(scene),
                collection.IsOpen(scene),
                FindPreloadedScene(scene)?.isPreloaded ?? false
            );

        /// <summary>Gets whatever the scene is open, either as part of a collection, or as stand-alone.</summary>
        public IsOpenReturnValue IsOpen(scene scene) =>
            (
                standalone.IsOpen(scene),
                collection.IsOpen(scene),
                FindPreloadedScene(scene)?.isPreloaded ?? false
            );

        /// <summary>Finds the <see cref="OpenSceneInfo"/> of this <see cref="scene"/>.</summary>
        public OpenSceneInfo FindOpenScene(scene scene)
        {
            if (collection.openScenes.Find(scene) is OpenSceneInfo info)
                return info;
            else
                return standalone.openScenes.Find(scene);
        }

        /// <summary>Finds the first open instance of this <see cref="Scene"/>, if it is open.</summary>
        public OpenSceneInfo FindOpenScene(Scene scene)
        {
            if (collection.openScenes.Find(scene) is OpenSceneInfo info)
                return info;
            else
                return standalone.openScenes.Find(scene);
        }

        /// <summary>Find first preloaded instance this scene.</summary>
        public OpenSceneInfo FindPreloadedScene(Scene scene) =>
            FindPreloadedScenes().FirstOrDefault(s => s.scene.scene == scene).scene;

        /// <summary>Find first preloaded instance this scene.</summary>
        public OpenSceneInfo FindPreloadedScene(scene scene) =>
            FindPreloadedScenes().FirstOrDefault(s => s.scene.unityScene == scene).scene;

        /// <summary>Finds all current preloaded scenes.</summary>
        public IEnumerable<(OpenSceneInfo scene, SceneManagerBase sceneManager)> FindPreloadedScenes() =>
            collection.openScenes.Where(s => s.isPreloaded).Select(s => (s, (SceneManagerBase)collection)).Concat(
            standalone.openScenes.Where(s => s.isPreloaded).Select(s => (s, (SceneManagerBase)standalone)));

        #endregion
        #region Active

        /// <summary>Sets a scene as the activate scene.</summary>
        public void SetActive(scene scene)
        {
            if (scene.isLoaded)
                _ = sceneManager.SetActiveScene(scene);
        }

        /// <inheritdoc cref="SetActive(scene)"/>
        public void SetActive(Scene scene) =>
            SetActive(scene.GetOpenSceneInfo().unityScene.Value);

        /// <summary>Gets the currently open scene.</summary>
        public OpenSceneInfo activeScene =>
            utility.FindOpenScene(sceneManager.GetActiveScene());

        #endregion
        #region DontDestroyOnLoad

        internal class AdvancedSceneManagerHelper : MonoBehaviour
        { }

        static GameObject m_helper;
        static GameObject helper
        {
            get
            {

                if (m_helper == null)
                {

                    if (Object.FindObjectOfType<AdvancedSceneManagerHelper>() is AdvancedSceneManagerHelper h && h)
                    {
                        m_helper = h.gameObject;
                        return m_helper;
                    }


                    m_helper = new GameObject("Advanced Scene Manager helper");
                    m_helper.AddComponent<AdvancedSceneManagerHelper>();
                    Object.DontDestroyOnLoad(helper);

                }

                return m_helper;

            }
        }

        static Scene m_dontDestroyOnLoadScene;
        static Scene dontDestroyOnLoadScene
        {
            get
            {
                if (!m_dontDestroyOnLoadScene)
                {

                    m_dontDestroyOnLoadScene = ScriptableObject.CreateInstance<Scene>();
                    m_dontDestroyOnLoadScene.SetName("DontDestroyOnLoad");

                }
                return m_dontDestroyOnLoadScene;
            }
        }

        OpenSceneInfo m_dontDestroyOnLoad;
        /// <summary>Represents 'DontDestroyOnLoad' scene.</summary>
        public OpenSceneInfo dontDestroyOnLoad
        {
            get
            {
                if (m_dontDestroyOnLoad == null)
                    m_dontDestroyOnLoad = new OpenSceneInfo(dontDestroyOnLoadScene, helper.scene, standalone);
                return m_dontDestroyOnLoad;
            }
        }

        /// <summary>Adds the component to the 'Advanced Scene Manager' gameobject in DontDestroyOnLoad.</summary>
        internal T AddToDontDestroyOnLoad<T>() where T : Component =>
            helper.AddComponent<T>();

        #endregion

    }

}
