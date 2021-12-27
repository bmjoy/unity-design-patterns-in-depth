using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Linq;
using unityScene = UnityEngine.SceneManagement.Scene;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Core
{

    /// <summary>A runtime class that identifies an open scene.</summary>
    public class OpenSceneInfo : IEquatable<OpenSceneInfo>, IEquatable<Scene>, IEquatable<unityScene>
#if UNITY_EDITOR
        , IEquatable<SceneAsset>
#endif
    {

        private OpenSceneInfo()
        { }

        internal OpenSceneInfo(Scene scene, unityScene unityScene, SceneManagerBase sceneManager, object asyncOperation = null)
        {
            this.scene = scene;
            this.unityScene = unityScene;
            this.asyncOperation = asyncOperation;
            this.sceneManager = sceneManager;
        }

        #region Properties

        /// <summary>The <see cref="Scene"/> that this <see cref="OpenSceneInfo"/> is associated with.</summary>
        public Scene scene { get; private set; }

        /// <summary>The <see cref="UnityEngine.SceneManagement.Scene"/> that this <see cref="OpenSceneInfo"/> is associated with.</summary>
        public unityScene? unityScene { get; private set; }

        /// <summary>Gets whatever this scene is preloaded.</summary>
        public bool isPreloaded => (unityScene?.IsValid() ?? false) && (isPreloadedOverride ?? asyncOperation != null);

        /// <summary>Gets whatever this scene is persistent. See <see cref="PersistentUtility"/> for more details.</summary>
        public bool isPersistent => (unityScene?.IsValid() ?? false) && (PersistentUtility.GetPersistentOption(unityScene.Value) != SceneCloseBehavior.Close);

        /// <summary>Gets whatever this scene is a collection scene.</summary>
        public bool isCollection => (unityScene?.IsValid() ?? false) && (SceneManager.collection.openScenes.Contains(this));

        /// <summary>Gets whatever this scene is a collection scene.</summary>
        public bool isStandalone => (unityScene?.IsValid() ?? false) && (SceneManager.standalone.openScenes.Contains(this));

        /// <summary>Gets whatever this scene is a special scene, i.e. splash screen / loading screen.</summary>
        public bool isSpecial => (unityScene?.IsValid() ?? false) && LoadingScreenUtility.IsLoadingScreenOpen(this);

        /// <summary>Gets whatever this scene is a untracked scene, this should never return <see langword="true"/>, that would be a bug.</summary>
        internal bool isUntracked => (unityScene?.IsValid() ?? false) && (!SceneManager.utility.openScenes.Contains(this));

        /// <summary>Can be used to override <see cref="isPreloaded"/>, for use in support packages.</summary>
        internal bool? isPreloadedOverride { get; set; }

        /// <summary>The async operation associated preloaded scenes.</summary>
        /// <remarks>For normal scenes this would be <see cref="UnityEngine.AsyncOperation"/>, but can be overriden by addressable support package, which would provide its own async operation.</remarks>
        internal object asyncOperation { get; set; }

        /// <summary>Gets whatever this scene is currently open.</summary>
        public bool isOpen =>
            (isPreloaded
                ? unityScene?.IsValid()
                : unityScene?.isLoaded)
            ?? false;

        /// <summary>The scene manager associated with this <see cref="OpenSceneInfo"/>.</summary>
        public SceneManagerBase sceneManager { get; private set; }

        #endregion
        #region Equality

        public override bool Equals(object other)
        {

            if (other is Scene s)
                return Equals(s);
            else if (other is OpenSceneInfo s1)
                return Equals(s1);
            else if (other is unityScene s2)
                return Equals(s2);
#if UNITY_EDITOR
            else if (other is SceneAsset s3)
                return Equals(s3);
#endif

            return false;

        }

        public override int GetHashCode() =>
            base.GetHashCode();

        public bool Equals(OpenSceneInfo osi) => Equals(osi?.scene) || Equals(osi?.unityScene);
        public bool Equals(Scene scene) => scene && scene.Equals(this);
        public bool Equals(unityScene scene) => scene == unityScene;
        public bool Equals(unityScene? scene) => scene.HasValue && scene == unityScene;

#if UNITY_EDITOR

        public bool Equals(SceneAsset asset)
        {

            if (!asset)
                return false;

            var path = AssetDatabase.GetAssetPath(asset);
            if (scene)
                return scene.path == path || AssetDatabase.AssetPathToGUID(path) == scene.assetID;
            else
                return unityScene.HasValue && unityScene.Value.path == path;

        }

#endif

        #endregion

        /// <summary>Called by <see cref="AsyncOperations.SceneUnloadAction"/> when scene is closed.</summary>
        internal void OnSceneClosed() =>
            unityScene = null;

        /// <inheritdoc/>
        public override string ToString() =>
            scene.name;

    }

}
