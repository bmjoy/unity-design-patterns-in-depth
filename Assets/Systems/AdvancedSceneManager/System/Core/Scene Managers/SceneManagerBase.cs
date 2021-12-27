using AdvancedSceneManager.Core.AsyncOperations;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using static AdvancedSceneManager.SceneManager;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Core
{

    /// <summary>Base class for <see cref="collection"/> and <see cref="standalone"/> classes. Contains shared functionality for scene management.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class SceneManagerBase
    {

#pragma warning disable 67

        /// <summary>Occurs when a scene is opened in this scene manager.</summary>
        public event Action<OpenSceneInfo> sceneOpened;

        /// <summary>Occurs when a scene is closed in this scene manager.</summary>
        public event Action<OpenSceneInfo> sceneClosed;

        internal void RaiseSceneOpened(OpenSceneInfo scene) =>
            ActionUtility.Try(() => sceneOpened?.Invoke(scene));

        internal void RaiseSceneClosed(OpenSceneInfo scene) =>
            ActionUtility.Try(() => sceneClosed?.Invoke(scene));

#pragma warning restore 67

        #region Open scene list

        public SceneManagerBase() =>
            openScenes = new ReadOnlyCollection<OpenSceneInfo>(m_scenes);

        readonly List<OpenSceneInfo> m_scenes = new List<OpenSceneInfo>();

        internal void Add(params OpenSceneInfo[] scenes)
        {
            foreach (var scene in scenes)
                if (!m_scenes.Contains(scene))
                {
                    scene.scene.OnPropertyChanged();
                    m_scenes.Add(scene);
                }
        }

        internal void Remove(params OpenSceneInfo[] scenes)
        {
            foreach (var scene in scenes)
            {
                m_scenes.Remove(scene);
                scene.scene.OnPropertyChanged();
            }
        }

        internal void Clear() =>
            m_scenes.Clear();

        /// <summary>The open scenes in this scene manager.</summary>
        public ReadOnlyCollection<OpenSceneInfo> openScenes { get; }

        /// <summary>Finds last open instance of the specified scene.</summary>
        public OpenSceneInfo Find(Scene scene) =>
            openScenes.LastOrDefault(s => s.scene == scene);

        /// <summary>Finds the open instance of the specified scene.</summary>
        public OpenSceneInfo Find(scene scene) =>
            openScenes.Find(scene);

        /// <summary>Gets the last opened scene.</summary>
        public OpenSceneInfo GetLastScene() =>
            m_scenes.LastOrDefault();

        /// <summary>Gets if the scene is open.</summary>
        public bool IsOpen(Scene scene) =>
            Find(scene)?.isOpen ?? false;

        /// <summary>Gets if the scene is open.</summary>
        public bool IsOpen(scene scene) =>
            Find(scene)?.isOpen ?? false;

        #endregion
        #region Open, Activate

        /// <summary>Open the scene.</summary>
        public virtual SceneOperation<OpenSceneInfo> Open(Scene scene) =>
            SceneOperation<OpenSceneInfo>.Run(this).
            Open(scene).
            Return((o) => o.FindLastAction<SceneOpenAction>().openScene);

        /// <summary>Opens the scenes.</summary>
        public virtual SceneOperation<OpenSceneInfo[]> OpenMultiple(params Scene[] scenes) =>
            SceneOperation<OpenSceneInfo[]>.Run(this).
            Open(scenes).
            Return(o => o.FindActions<SceneOpenAction>().Select(action => action.openScene).ToArray());

        /// <summary>Open the scene.</summary>
        public virtual SceneOperation OpenWithoutReturnValue(Scene scene) =>
            SceneOperation.Run(this).
            Open(scene);

        /// <summary>Reopens the scene.</summary>
        public SceneOperation<OpenSceneInfo> Reopen(OpenSceneInfo scene) =>
            SceneOperation<OpenSceneInfo>.Run(this).
            Reopen(scene).
            Return((o) => o.FindLastAction<SceneOpenAction>().openScene);

        #endregion
        #region Close

        /// <summary>Close the scene.</summary>
        public virtual SceneOperation Close(OpenSceneInfo scene)
        {

            if (!scene?.unityScene.HasValue ?? false)
                return SceneOperation.Done;

            return SceneOperation.Run(this).
                Close(force: true, scene);

        }

        /// <summary>Close the scenes.</summary>
        public virtual SceneOperation CloseMultiple(params OpenSceneInfo[] scenes)
        {

            scenes = scenes.Where(s => s?.unityScene.HasValue ?? false).ToArray();

            if (!scenes.Any())
                return SceneOperation.Done;

            return SceneOperation.Run(this).
                Close(scenes, force: true);

        }

        /// <summary>Close all open scenes in the list.</summary>
        internal virtual SceneOperation CloseAll()
        {

            if (!openScenes.Any())
                return SceneOperation.Done;

            return SceneOperation.Run(this).
                Close(openScenes, force: true);

        }

        #endregion
        #region Toggle

        /// <summary>
        /// <para>Gets if this scene manager can open the specified scene.</para>
        /// <para><see cref="standalone"/> always returns true.</para>
        /// </summary>
        public virtual bool CanOpen(Scene scene) => true;

        /// <summary>Toggles the scene open or closed.</summary>
        /// <param name="enabled">If null, the scene will be toggled on or off depending on whatever the scene is open or not. Pass a value to ensure that the scene is either open or closed.</param>
        public SceneOperation Toggle(Scene scene, bool? enabled = null)
        {

            if (!CanOpen(scene) || !scene)
                return SceneOperation.Done;

            var openSceneInfo = scene.GetOpenSceneInfo();
            var isOpen = openSceneInfo.isOpen;
            var isEnabled = enabled.GetValueOrDefault();

            //Debug.LogError($"Toggle ({enabled}): " + scene.scene.path + $"(is open: {isOpen})");

            if (enabled.HasValue)
            {
                if (isEnabled && !isOpen)
                    return OpenWithoutReturnValue(scene);
                else if (!isEnabled && isOpen)
                    return Close(openSceneInfo);
            }
            else
            {
                if (!isOpen)
                    return OpenWithoutReturnValue(scene);
                else if (isOpen)
                    return Close(openSceneInfo);
            }

            return SceneOperation.Done;

        }

        /// <summary>Ensures that the scene is open.</summary>
        public SceneOperation EnsureOpen(Scene scene) =>
            Toggle(scene, true);

        #endregion

    }

}
