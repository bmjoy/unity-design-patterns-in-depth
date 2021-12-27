using AdvancedSceneManager.Exceptions;
using AdvancedSceneManager.Models;
using System;
using System.Linq;
using UnityEngine;
using static AdvancedSceneManager.SceneManager;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Core
{

    /// <summary>
    /// <para>The manager for collection scenes.</para>
    /// <para>Usage: <see cref="collection"/>.</para>
    /// </summary>
    public class CollectionManager : SceneManagerBase
    {

        public static implicit operator SceneCollection(CollectionManager manager) =>
            manager.current;

        public static implicit operator bool(CollectionManager manager) =>
            manager.current;

        /// <summary>Called when a collection is opened.</summary>
        public event Action<SceneCollection> opened;
        /// <summary>Called when a collection is closed.</summary>
        public event Action<SceneCollection> closed;

        /// <summary>The currently open collection.</summary>
        public SceneCollection current { get; private set; }

        /// <summary>Sets <see cref="current"/> to null, make sure to only use this after manually closing <see cref="CollectionManager"/> scenes!</summary>
        internal void SetNull() =>
            current = null;

        /// <summary>Sets the collection.</summary>
        internal void Set(SceneCollection collection, params OpenSceneInfo[] scenes)
        {
            current = collection;
            Clear();
            standalone.Remove(scenes);
            Add(scenes);
        }

        #region ISceneOperationsManager<SceneCollection>

        /// <summary>
        /// <para>Opens the collection.</para>
        /// </summary>
        public SceneOperation Open(SceneCollection collection, bool ignoreLoadingScreen = false, bool force = false)
        {
            if (!IsOpen(collection))
                return OpenInternal(collection, ignoreLoadingScreen, force);
            else
                return SceneOperation.Done;
        }

        /// <summary>Reopens the current collection.</summary>
        public SceneOperation Reopen()
        {
            if (current)
                return OpenInternal(current);
            else
                return SceneOperation.Done;
        }

        SceneOperation OpenInternal(SceneCollection collection, bool ignoreLoadingScreen = false, bool forceOpen = false)
        {

            if (!collection)
                return SceneOperation.Done;

            var operation = SceneOperation.Run(this).
                    WithCollection(collection, withCallbacks: true).
                    Close(utility.openScenes.Where(s => !s.isPersistent)).
                    WithLoadingScreen(use: !ignoreLoadingScreen).
                    WithCallback(() =>
                    {
                        if (current)
                            closed?.Invoke(current);
                        current = collection;
                        try
                        {
                            opened?.Invoke(collection);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    });

            //If already open, then we want to reopen it, so we'll need to call Reopen() method instead of Open()
            if (collection == current)
                operation.Reopen(collection.scenes.Where(s => s.tag.openBehavior == SceneOpenBehavior.OpenNormally).Select(s => s.GetOpenSceneInfo()));
            else
                operation.Open(collection.scenes, force: forceOpen);

            return operation;

        }

        /// <summary>Closes the current collection.</summary>
        public SceneOperation Close()
        {

            if (!standalone.openScenes.Any() && !current)
                return SceneOperation.Done;

            return SceneOperation.Run(this).
                WithCollection(current, withCallbacks: true).
                Close(standalone.openScenes).
                Close(openScenes).
                WithCallback(() =>
                {
                    var prev = current;
                    SetNull();
                    try
                    {
                        closed?.Invoke(prev);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                });

        }

        /// <summary>Toggles the collection.</summary>
        /// <param name="enabled">If null, collection will be toggled on or off depending on whatever collection is open or not. Pass a value to ensure that collection either open or closed.</param>
        public SceneOperation Toggle(SceneCollection collection, bool? enabled = null)
        {

            var isOpen = IsOpen(collection);
            var isEnabled = enabled.GetValueOrDefault();

            if (enabled.HasValue)
            {
                if (isEnabled && !isOpen)
                    return Open(collection);
                else if (!isEnabled && isOpen)
                    return Close();
            }
            else
            {
                if (isOpen)
                    return Close();
                else
                    return Open(collection);
            }

            return SceneOperation.Done;

        }

        /// <summary>Gets whatever the collection is currently open.</summary>
        public bool IsOpen(SceneCollection collection) =>
            current == collection;

        #endregion
        #region ISceneOperationsManager<Scene>

        /// <summary>Gets whatever the scene can be opened by the current collection.</summary>
        public override bool CanOpen(Scene scene) =>
            current && current.scenes.Contains(scene);

        /// <summary>
        /// <para>Opens a scene.</para>
        /// <para>Throws a <see cref="OpenSceneException"/> if the scene cannot be opened by the current collection.</para>
        /// </summary>
        public override SceneOperation<OpenSceneInfo> Open(Scene scene)
        {
            if (!CanOpen(scene))
                throw new OpenSceneException(scene, current, "The scene is not part of the current open collection.");
            else
                return base.Open(scene).WithCollection(this);
        }

        /// <summary>
        /// <para>Opens the scenes.</para>
        /// <para>Throws a <see cref="OpenSceneException"/> if a scene cannot be opened by the current collection.</para>
        /// </summary>
        public override SceneOperation<OpenSceneInfo[]> OpenMultiple(params Scene[] scenes)
        {

            foreach (var scene in scenes)
                if (!CanOpen(scene))
                    throw new OpenSceneException(scene, current, "The scene is not part of the current open collection.");

            return base.OpenMultiple(scenes).WithCollection(this);

        }

        /// <summary>
        /// <para>Closes a scene.</para>
        /// <para>Throws a <see cref="CloseSceneException"/> if the scene is not a part of the current collection.</para>
        /// </summary>
        public override SceneOperation Close(OpenSceneInfo scene)
        {
            if (!CanOpen(scene.scene))
                throw new CloseSceneException(scene.scene, scene.unityScene.Value, current, "The scene is not part of the current open collection.");
            else
                return base.Close(scene).WithCollection(this);
        }

        /// <summary>
        /// <para>Closes the scenes.</para>
        /// <para>Throws a <see cref="CloseSceneException"/> if a scene is not a part of the current collection.</para>
        /// </summary>
        public override SceneOperation CloseMultiple(params OpenSceneInfo[] scenes)
        {

            scenes = scenes.Where(s => s?.unityScene.HasValue ?? false).ToArray();
            foreach (var scene in scenes)
                if (!CanOpen(scene.scene))
                    throw new CloseSceneException(scene.scene, scene.unityScene.Value, current, "The scene is not part of the current open collection.");

            return base.CloseMultiple(scenes).WithCollection(this);

        }

        #endregion

    }

}
