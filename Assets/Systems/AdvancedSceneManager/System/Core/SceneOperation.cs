using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core.AsyncOperations;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Core
{

    /// <inheritdoc cref="SceneOperationBase{TSelf}"/>
    public class SceneOperation<ReturnValue> : SceneOperationBase<SceneOperation<ReturnValue>>
    {

        public static SceneOperation<ReturnValue> Done { get; } = new SceneOperation<ReturnValue>(null) { isDone = true };

        public SceneOperation(SceneManagerBase sceneManager) :
            base(sceneManager) =>
            WithCallback(() =>
            {
                if (action != null)
                    value = action.Invoke(this);
            });

        public static SceneOperation<ReturnValue> Run(SceneManagerBase sceneManager) =>
            new SceneOperation<ReturnValue>(sceneManager);

        public ReturnValue value { get; private set; }

        Func<SceneOperation<ReturnValue>, ReturnValue> action;

        public SceneOperation<ReturnValue> Return(Func<SceneOperation<ReturnValue>, ReturnValue> action) =>
            Set(() => this.action = (s) => action.Invoke(this));

        public SceneOperation<ReturnValue> Return(Func<ReturnValue> action) =>
            Set(() => this.action = (s) => action.Invoke());

    }

    /// <inheritdoc cref="SceneOperationBase{TSelf}"/>
    public class SceneOperation : SceneOperationBase<SceneOperation>
    {

        public enum Phase
        {
            /// <summary>The scene operation is currently executing close callbacks on the scenes that are being closed, if any.</summary>
            CloseCallbacks,
            /// <summary>The scene operation is currently unloading the scenes, if any.</summary>
            UnloadScenes,
            /// <summary>The scene operation is currently loading the scenes, if any.</summary>
            LoadScenes,
            /// <summary>The scene operation is currently executing open callbacks on the scenes that are being opened, if any.</summary>
            OpenCallbacks,
            /// <summary>The scene operation is currently finishing loading / activating the scenes, if any.</summary>
            FinishLoad,
            /// <summary>The scene operation is currently executing custom actions, added through <see cref="SceneOperationBase{TSelf}.WithAction(SceneAction[])"/> or similar methods, if any.</summary>
            CustomActions
        }

        public static SceneOperation Done { get; } = new SceneOperation(null) { isDone = true };

        public SceneOperation(SceneManagerBase sceneManager) :
            base(sceneManager)
        { }

        public static SceneOperation Run(SceneManagerBase sceneManager) =>
            new SceneOperation(sceneManager);

    }

    /// <inheritdoc cref="SceneOperationBase{TSelf}"/>
    public interface ISceneOperation
    {

        /// <inheritdoc cref="SceneOperationBase{TSelf}.phase"/>
        SceneOperation.Phase phase { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.open"/>
        ReadOnlyCollection<(Scene scene, bool force)> open { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.close"/>
        ReadOnlyCollection<(OpenSceneInfo scene, bool force)> close { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.reopen"/>
        ReadOnlyCollection<OpenSceneInfo> reopen { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.collection"/>
        SceneCollection collection { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.loadingScreen"/>
        Scene loadingScreen { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.useLoadingScreen"/>
        bool useLoadingScreen { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.sceneManager"/>
        SceneManagerBase sceneManager { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.clearUnusedAssets"/>
        bool? clearUnusedAssets { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.doCollectionCallbacks"/>
        bool doCollectionCallbacks { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.loadingScreenCallback"/>
        Action<LoadingScreen> loadingScreenCallback { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.actions"/>
        ReadOnlyCollection<SceneAction> actions { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.totalProgress"/>
        float totalProgress { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.openedLoadingScreen"/>
        SceneOperation<LoadingScreen> openedLoadingScreen { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.current"/>
        SceneAction current { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.cancelled"/>
        bool cancelled { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.keepWaiting"/>
        bool keepWaiting { get; }

        /// <inheritdoc cref="SceneOperationBase{TSelf}.loadingPriority"/>
        ThreadPriority? loadingPriority { get; }

    }

    /// <summary>A scene operation is a queueable operation that can open or close scenes. See also: <see cref="SceneAction"/>.</summary>
    public abstract class SceneOperationBase<TSelf> : CustomYieldInstruction, ISceneOperation where TSelf : SceneOperationBase<TSelf>
    {

        public SceneOperationBase(SceneManagerBase sceneManager)
        {
            this.sceneManager = sceneManager;
            open = new ReadOnlyCollection<(Scene scene, bool ignoreDoNotOpen)>(m_open);
            close = new ReadOnlyCollection<(OpenSceneInfo scene, bool force)>(m_close);
            reopen = new ReadOnlyCollection<OpenSceneInfo>(m_reopen);
            actions = new ReadOnlyCollection<SceneAction>(m_actions);
            Run()?.StartCoroutine(description: "SceneOperation.Run()", onComplete: () =>
            {
                m_running.Remove(this);
                isDone = true;
            });
        }

        #region Queue

        /// <summary>Gets whatever ASM is currently busy.</summary>
        public static bool isBusy => queue.Any() || running.Any();

        /// <summary>Occurs when an queued operation finishes and queue is empty.</summary>
        public static event Action queueEmpty;

        /// <summary>Gets the operations currently in queue.</summary>
        public static ReadOnlyCollection<ISceneOperation> queue { get; }
        static readonly List<ISceneOperation> m_queue = new List<ISceneOperation>();

        /// <summary>Gets the operations that are currently running.</summary>
        public static ReadOnlyCollection<ISceneOperation> running { get; }
        static readonly List<ISceneOperation> m_running = new List<ISceneOperation>();

        static SceneOperationBase()
        {
            queue = new ReadOnlyCollection<ISceneOperation>(m_queue);
            running = new ReadOnlyCollection<ISceneOperation>(m_running);
        }

        /// <summary>Queues this scene operation.</summary>
        void Queue()
        {
            if (!m_queue.Contains(this))
                m_queue.Add(this);
        }

        void DequeueInternal()
        {

            m_queue.Remove(this);
            if (!m_queue.Any())
            {
                ResetThreadPriority();
                queueEmpty?.Invoke();
            }

        }

        bool ignoreQueue;
        public TSelf IgnoreQueue(bool ignore = true) =>
            Set(() => ignoreQueue = ignore, allowAfterStarted: true);

        internal TSelf SetParent(ISceneOperation parent) =>
            Set(() => (parent as SceneOperationBase<TSelf>)?.AddChildOperation(this));

        #endregion
        #region Properties

        /// <summary>Inherited from <see cref="CustomYieldInstruction"/>. Tells unity whatever the operation is done or not.</summary>
        public override bool keepWaiting => !isDone;

        /// <summary>The phase the this scene operation is currently in.</summary>
        public SceneOperation.Phase phase { get; private set; }

        /// <summary>The scenes to open.</summary>
        public ReadOnlyCollection<(Scene scene, bool force)> open { get; private set; }

        /// <summary>The scenes to close.</summary>
        public ReadOnlyCollection<(OpenSceneInfo scene, bool force)> close { get; private set; }

        /// <summary>The scenes to reopen.</summary>
        public ReadOnlyCollection<OpenSceneInfo> reopen { get; private set; }

        /// <summary>The collection that is associated with this scene operation.</summary>
        public SceneCollection collection { get; private set; }

        /// <summary>The loading screen that this loading screen will use, unless null or useLoadingScreen is false, in which case collection loading screen will be used, if one is associated.</summary>
        public Scene loadingScreen { get; private set; }

        /// <summary>
        /// Specifies whatever this scene operation will show a loading screen, if one is set through loadingScreen or associated collection.
        /// <para>In other words: If false, no loading screen will be shown regardless of loadingScreen or associated collection.</para>
        /// </summary>
        public bool useLoadingScreen { get; private set; } = true;

        /// <summary>The scene manager that requested this scene operation.</summary>
        public SceneManagerBase sceneManager { get; private set; }

        /// <summary>Specifies whatever unused assets should be cleared to save memory.</summary>
        public bool? clearUnusedAssets { get; private set; }

        /// <summary>Specifies whatever <see cref="ICollectionOpen"/> and <see cref="ICollectionClose"/> callbacks are executed on the associated collection, if one is.</summary>
        public bool doCollectionCallbacks { get; private set; }

        /// <summary>Specifies the callback to use after loading screen scene is opened, but loading screen not yet shown. See <see cref="LoadingScreenUtility.OpenLoadingScreen{T}(Scene, float?, Action{T})"/>.</summary>
        public Action<LoadingScreen> loadingScreenCallback { get; private set; }

        readonly List<(Scene scene, bool ignoreDoNotOpen)> m_open = new List<(Scene scene, bool ignoreDoNotOpen)>();
        readonly List<(OpenSceneInfo scene, bool force)> m_close = new List<(OpenSceneInfo scene, bool force)>();
        readonly List<OpenSceneInfo> m_reopen = new List<OpenSceneInfo>();

        public ReadOnlyCollection<SceneAction> actions { get; private set; }
        readonly List<SceneAction> m_actions = new List<SceneAction>();
        readonly List<SceneAction> m_customActions = new List<SceneAction>();
        readonly List<Action> m_customActionsAction = new List<Action>();
        readonly List<Func<IEnumerator>> m_customActionsCoroutine = new List<Func<IEnumerator>>();

        /// <summary>Finds the actions of a specified type that was used in this operation.</summary>
        public IEnumerable<SceneAction> FindActions<T>() where T : SceneAction
        {
            //Lets proxy SceneOpenAction and SceneCloseAction to their actual action,
            //since Open and Close actions are just convinience actions, that are not used by scene operation.
            if (typeof(T) == typeof(SceneOpenAction))
                return FindActions<SceneLoadAction>();
            else if (typeof(T) == typeof(SceneCloseAction))
                return FindActions<SceneUnloadAction>();
            else
                return actions.OfType<T>();
        }

        /// <summary>Finds the last action of a specified type that was used in this operation.</summary>
        public SceneAction FindLastAction<T>() where T : SceneAction
        {
            //Lets proxy SceneOpenAction and SceneCloseAction to their actual action,
            //since Open and Close actions are just convinience actions, that are not used by scene operation.
            if (typeof(T) == typeof(SceneOpenAction))
                return FindLastAction<SceneLoadAction>();
            else if (typeof(T) == typeof(SceneCloseAction))
                return FindLastAction<SceneUnloadAction>();
            else
                return actions.OfType<T>().LastOrDefault();
        }

        /// <summary>The total progress made by this operation.</summary>
        public float totalProgress
        {
            get
            {

                var value = 0f;
                if (actions.Count <= 1)
                    value = actions.FirstOrDefault()?.progress ?? 0;
                else
                    value = actions.Sum(a => a.progress) / (actions.Count() - 1);

                if (ChildOperations != null && ChildOperations.Any())
                {
                    var childrenProgress = ChildOperations.Sum(a => a.totalProgress) / ChildOperations.Count;
                    value += childrenProgress;
                    if (actions.Any())
                        value /= 2;
                }

                return value;

            }
        }

        /// <summary>Child operations progress is added to <see cref="totalProgress"/>.</summary>
        List<ISceneOperation> ChildOperations { get; set; }

        /// <summary>Adds the <see cref="SceneOperation"/> as a child to this operation, causing this operation to report child progress in <see cref="totalProgress"/>.</summary>
        void AddChildOperation(ISceneOperation operation)
        {
            if (ChildOperations == null) ChildOperations = new List<ISceneOperation>();
            ChildOperations.Add(operation);
        }

        /// <summary>Gets the loading screen that was opened.</summary>
        public SceneOperation<LoadingScreen> openedLoadingScreen { get; private set; }

        /// <summary>The current action that is executing.</summary>
        public SceneAction current { get; private set; }

        /// <summary>Gets if this scene operation is cancelled.</summary>
        public bool cancelled { get; private set; }

        /// <summary>
        /// <para>Gets the loading priority for the background thread.</para>
        /// <para>Defaults to <see cref="SceneCollection.loadingPriority"/> when collection is used, otherwise <see cref="Profile.backgroundLoadingPriority"/>.</para>
        /// </summary>
        public ThreadPriority? loadingPriority { get; private set; }

        #endregion
        #region Fluent api

        /// <inheritdoc cref="open"/>
        public TSelf Open(params Scene[] scene) =>
            Set(() => m_open.AddRange(scene.Select(s => (s, ignoreDoNotOpen: true))));

        /// <inheritdoc cref="close"/>
        public TSelf Close(bool force, params OpenSceneInfo[] scenes) =>
            Set(() => m_close.AddRange(scenes.Select(s => (s, force))));

        /// <inheritdoc cref="close"/>
        public TSelf Close(params OpenSceneInfo[] scenes) =>
            Close(force: false, scenes);

        /// <inheritdoc cref="reopen"/>
        public TSelf Reopen(params OpenSceneInfo[] scene) =>
            Set(() => m_reopen.AddRange(scene));

        /// <inheritdoc cref="open"/>
        public TSelf Open(IEnumerable<Scene> scene, bool force = false) =>
            Set(() => m_open.AddRange(scene.Select(s => (s, force))));

        /// <inheritdoc cref="close"/>
        public TSelf Close(IEnumerable<OpenSceneInfo> scenes, bool force = false) =>
            Set(() => m_close.AddRange(scenes.Select(s => (s, force))));

        /// <inheritdoc cref="reopen"/>
        public TSelf Reopen(IEnumerable<OpenSceneInfo> scene) =>
            Set(() => m_reopen.AddRange(scene));

        /// <inheritdoc cref="collection"/>
        public TSelf WithCollection(SceneCollection collection, bool withCallbacks = false) =>
            Set(() => { this.collection = collection; doCollectionCallbacks = withCallbacks; });

        /// <inheritdoc cref="useLoadingScreen"/>
        public TSelf WithLoadingScreen(bool use) =>
            Set(() => useLoadingScreen = use);

        /// <inheritdoc cref="loadingScreen"/>
        public TSelf WithLoadingScreen(Scene scene) =>
            Set(() => loadingScreen = scene);

        /// <inheritdoc cref="m_customActions"/>
        public TSelf WithAction(params SceneAction[] actions) =>
            Set(() => m_customActions.AddRange(actions));

        /// <inheritdoc cref="m_customActionsAction"/>
        public TSelf WithAction(params Action[] actions) =>
            Set(() => m_customActionsAction.AddRange(actions));

        /// <inheritdoc cref="m_customActionsCoroutine"/>
        public TSelf WithAction(params Func<IEnumerator>[] actions) =>
            Set(() => m_customActionsCoroutine.AddRange(actions));

        /// <inheritdoc cref="clearUnusedAssets"/>
        public TSelf WithClearUnusedAssets(bool enable) =>
            Set(() => clearUnusedAssets = enable);

        /// <inheritdoc cref="loadingScreenCallback"/>
        public TSelf WithLoadingScreenCallback(Action<LoadingScreen> callback) =>
            Set(() => loadingScreenCallback = callback);

        /// <inheritdoc cref="loadingPriority"/>
        public TSelf WithLoadingPriority(ThreadPriority priority) =>
            Set(() => loadingPriority = priority);

        readonly List<Action> _callbacks = new List<Action>();
        readonly List<Action<TSelf>> callbacks = new List<Action<TSelf>>();
        internal TSelf WithCallback(Action<TSelf> action, bool enabled = true) =>
            Set(() =>
            {
                if (enabled && !callbacks.Contains(action))
                    callbacks.Add(action);
                else if (!enabled)
                    callbacks.Remove(action);
            }, allowAfterStarted: true);

        internal TSelf WithCallback(Action action, bool enabled = true) =>
            Set(() =>
            {
                if (enabled && !_callbacks.Contains(action))
                    _callbacks.Add(action);
                else if (!enabled)
                    _callbacks.Remove(action);
            }, allowAfterStarted: true);

        void Callbacks()
        {
            foreach (var callback in _callbacks) callback?.Invoke();
            foreach (var callback in callbacks) callback?.Invoke((TSelf)this);
        }

        protected TSelf Set(Action action, bool allowAfterStarted = false)
        {
            if (isExecuting && !allowAfterStarted)
                throw new Exception("Cannot change SceneOperation properties once it has started executing!");
            action?.Invoke();
            return (TSelf)this;
        }

        #endregion
        #region Run

        Action cancelCallback; //Called in Run().Cancel()

        /// <summary>
        /// Cancel this operation.
        /// <para>Note that the operation might not be cancelled immediately, if user defined callbacks are currently running
        /// (WithAction(), WithCallback()) they will run to completion before operation is cancelled. 'cancelled' property can be used in callbacks to check whatever a operation is cancelled.</para>
        /// </summary>
        public void Cancel(Action callbackWhenFullyCancelled = null)
        {
            cancelCallback = callbackWhenFullyCancelled;
            cancelled = true;
        }

        GlobalCoroutine coroutine;

        bool isExecuting; //Used to prevent changing properties after execution has started
        protected bool isDone;
        IEnumerator Run()
        {

            //Lets wait a bit so that users can change properties, since most cannot be changed once started
            yield return new WaitForSecondsRealtime(0.1f);

            if (!ignoreQueue)
            {
                //Wait until we're at the top of the queue
                Queue();
                while (queue.FirstOrDefault() != this && !ignoreQueue)
                    yield return null;
            }

            m_running.Add(this);

            //Evaluate current state and generate actions
            actions = new ReadOnlyCollection<SceneAction>(CreateActions());
            isExecuting = true;

            //Show loading screen
            if (useLoadingScreen)
            {

                void Callback(LoadingScreen loadingScreen)
                {
                    loadingScreen.operation = this;
                    loadingScreenCallback?.Invoke(loadingScreen);
                }

                if (loadingScreen)
                    openedLoadingScreen = LoadingScreenUtility.OpenLoadingScreen<LoadingScreen>(loadingScreen, callbackBeforeBegin: Callback);
                else if (collection)
                    openedLoadingScreen = LoadingScreenUtility.OpenLoadingScreen(collection, callbackBeforeBegin: Callback);

                if (openedLoadingScreen != null)
                    yield return openedLoadingScreen;

            }

            //Set loading thread priority
            SetThreadPriority();

            //Call collection close callbacks
            if (doCollectionCallbacks)
                yield return CollectionCloseCallbacks();

            //Run actions one by one
            foreach (var action in actions)
            {

                if (Cancel())
                    yield break;

                CheckPhaseChanged(action);

                //If action has invalid properties, it will call Done() early
                if (action.isDone)
                    continue;

                current = action;
                coroutine = action.DoAction(sceneManager).StartCoroutine(description: "SceneOperation.Run(" + action.GetType().Name + ")");
                while (coroutine.isRunning)
                    if (Cancel())
                        yield break;
                    else
                        yield return null;

                current = null;

            }

            //Set active scene in collections
            SetActiveScene();

            //Coroutine callbacks
            foreach (var action in m_customActionsCoroutine)
                yield return action?.Invoke();

            //Action callbacks
            foreach (var action in m_customActionsAction)
                action?.Invoke();

            if (clearUnusedAssets ?? collection)
                yield return Resources.UnloadUnusedAssets();

            //Call all callbacks added to this operation
            Callbacks();

            //Lets check if operation has been cancelled while running callbacks
            //(callbacks should run to completion before cancelling, callbacks can check cancelled property)
            if (Cancel())
                yield break;

            //Call collection open callbacks
            if (doCollectionCallbacks)
                yield return CollectionOpenCallbacks();

            //Hide loading screen
            if (openedLoadingScreen?.value)
            {
                yield return LoadingScreenUtility.CloseLoadingScreen(openedLoadingScreen.value);
                openedLoadingScreen = null;
            }
            Dequeue();

            bool Cancel()
            {
                if (cancelled && queue.Contains(this))
                {

                    //Reset background loading priority
                    ResetThreadPriority();

                    coroutine?.Stop();

                    if (openedLoadingScreen?.value)
                        openedLoadingScreen.value.OnCancel(this);
                    cancelCallback?.Invoke();

                    DequeueInternal();

                }
                return cancelled;
            }

            void Dequeue()
            {
                DequeueInternal();
            }

        }

        Type lastAction;
        static readonly Dictionary<Type, SceneOperation.Phase> phases = new Dictionary<Type, SceneOperation.Phase>()
        {
            { typeof(SceneCloseCallbackAction), SceneOperation.Phase.CloseCallbacks },
            { typeof(SceneUnloadAction), SceneOperation.Phase.UnloadScenes },
            { typeof(SceneLoadAction), SceneOperation.Phase.LoadScenes },
            { typeof(SceneOpenCallbackAction), SceneOperation.Phase.OpenCallbacks },
            { typeof(SceneFinishLoadAction), SceneOperation.Phase.FinishLoad },
        };

        void CheckPhaseChanged(SceneAction action)
        {

            var type = action.GetType();
            if (action.GetType() != lastAction)
            {

                var previousPhase = phase;
                var nextPhase = phases.GetValue(type, defaultValue: SceneOperation.Phase.CustomActions);

                if (previousPhase != nextPhase && openedLoadingScreen?.value)
                    openedLoadingScreen.value.OnScenePhaseChanged(this, previousPhase, phase);

            }
            lastAction = type;

        }

        IEnumerator CollectionOpenCallbacks()
        {

            if (collection)
            {
                yield return (collection.extraData as ICollectionOpen)?.OnCollectionOpen(collection);
                yield return CallbackUtility.Invoke<ICollectionOpen>().WithParam(collection).On(collection);
            }

        }

        IEnumerator CollectionCloseCallbacks()
        {

            //Use SceneManager.collection.current since we still want to call callbacks when a new collection is opening and
            //collection property will be set to new collection
            var old = SceneManager.collection.current;
            if (old)
            {
                yield return (old.extraData as ICollectionClose)?.OnCollectionClose(old);
                yield return (SceneManager.collection.current.extraData as ICollectionClose)?.OnCollectionClose(SceneManager.collection.current);
                yield return CallbackUtility.Invoke<ICollectionClose>().WithParam(old).On(old);
            }

        }

        void SetActiveScene()
        {

            if (collection)
            {

                var openActions = FindActions<SceneOpenAction>().ToArray();
                if (openActions.Any())
                {

                    var activeScene = collection.scenes.Contains(collection.activeScene) ? collection.activeScene : collection.scenes.FirstOrDefault();
                    var uScene = openActions.FirstOrDefault(a => a.scene == activeScene)?.openScene?.unityScene;

                    if (!uScene.HasValue)
                        uScene = openActions.FirstOrDefault().openScene.unityScene.Value;

                    SceneManager.utility.SetActive(uScene.Value);

                }

            }

        }

        static IEnumerator RestoreCrossSceneReferences()
        {

            var e = SceneUtility.GetAllOpenUnityScenes().GetEnumerator();
            var i = 0;
            while (e.MoveNext())
            {
                var e1 = CrossSceneReferenceUtility.RestoreCrossSceneReferencesWithWarnings_IEnumerator(e.Current, respectSettingsSuppressingWarnings: true);
                while (e1.MoveNext())
                {
                    i += 1;
                    if (i > 20)
                    {
                        i = 0;
                        yield return null;
                    }
                }
            }

        }

        #endregion
        #region Thread priority

        void SetThreadPriority()
        {

            //Set loading thread priority, if queued.
            //This property is global, and race conditions will occur if we allow non-queued operations to also set this

            if (!Profile.current || !Profile.current.enableChangingBackgroundLoadingPriority)
                return;

            if (ignoreQueue)
                return;

            Application.backgroundLoadingPriority = GetPriority();

            ThreadPriority GetPriority()
            {

                if (loadingPriority.HasValue)
                    return loadingPriority.Value;

                if (!collection)
                    return Profile.current.backgroundLoadingPriority;
                else
                {

                    if (collection.loadingPriority != CollectionThreadPriority.Auto)
                        return (ThreadPriority)collection.loadingPriority;
                    else
                    {

                        return LoadingScreenUtility.IsAnyLoadingScreenOpen
                            ? ThreadPriority.Normal
                            : ThreadPriority.Low;

                    }

                }

            }

        }

        void ResetThreadPriority()
        {
            if (Profile.current && Profile.current.enableChangingBackgroundLoadingPriority)
                Application.backgroundLoadingPriority = Profile.current.backgroundLoadingPriority;
        }

        #endregion
        #region Generate actions

        bool ShouldOpen((Scene scene, bool force) scene)
        {

            if (!scene.scene)
                return false;
            else if (scene.scene.IsOpen())
                return false;
            else if (scene.force)
                return true;
            else if (!collection)
                return true;
            else if (scene.scene.tag.openBehavior == SceneOpenBehavior.DoNotOpenInCollection)
                return false;

            return true;

        }

        bool ShouldClose((OpenSceneInfo scene, bool force) scene)
        {

            if (!scene.scene.unityScene.HasValue)
                return false;
            else if (DefaultSceneUtility.IsDefaultScene(scene.scene.unityScene.Value))
                return false;

            switch (scene.scene.scene.tag.closeBehavior)
            {
                case SceneCloseBehavior.KeepOpenIfNextCollectionAlsoContainsScene:
                    return !collection.scenes.Contains(scene.scene.scene);
                case SceneCloseBehavior.KeepOpenAlways:
                    return false;
                case SceneCloseBehavior.Close:
                default:
                    return true;
            }

        }

        List<SceneAction> CreateActions()
        {

            var reopen = this.reopen.Where(s => s.scene);
            var open = this.open.Where(ShouldOpen).Select(s => s.scene).Concat(reopen.Select(s => s.scene)).Distinct();
            var close = this.close.Where(ShouldClose).Select(s => s.scene).Concat(reopen).GroupBy(s => s.scene).Select(g => g.First());

            //Construct list of actions, order:
            //Call close callbacks
            //Close scenes
            //Preload scenes
            //Call pre-activation callbacks
            //Finish preload and activate scenes
            //Call post-activation callbacks

            var preCloseCallbackActions = close.Select(s => new SceneCloseCallbackAction(s)).ToArray();
            var unloadActions = close.Select(s => new SceneUnloadAction(s, collection)).ToArray();
            var loadActions = open.Select(s => new SceneLoadAction(s, collection)).ToArray();
            var finishLoadActions = loadActions.Select(s => new SceneFinishLoadAction(() => s.openScene)).ToArray();
            var postActivateCallbackActions = loadActions.Select(s => new SceneOpenCallbackAction(() => s.openScene)).ToArray();

            var actions = new List<SceneAction>();
            actions.AddRange(preCloseCallbackActions);
            actions.AddRange(unloadActions);
            actions.AddRange(loadActions);
            actions.AddRange(finishLoadActions);
            if (loadActions.Any())
                actions.Add(new CallbackAction(RestoreCrossSceneReferences));
            actions.AddRange(postActivateCallbackActions);

            actions.AddRange(m_customActions);

            return actions;

        }

        #endregion

    }

}
