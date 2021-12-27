using AdvancedSceneManager.Models;
using System;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Exceptions
{

    /// <summary>Thrown when a scene could not be opened.</summary>
    public class OpenSceneException : Exception
    {

        public OpenSceneException(Scene scene, SceneCollection collection = null, string message = null)
            : base(message ?? "The scene could not be opened.")
        {
            this.scene = scene;
            this.collection = collection;
        }

        /// <summary>
        /// <para>The collection that the scene was associated with.</para>
        /// <para>Null if scene was opened as stand-alone.</para>
        /// </summary>
        public SceneCollection collection { get; }

        /// <summary>The scene that was attempted to be opened.</summary>
        public Scene scene { get; }

    }

    /// <summary>Thrown when a scene could not be closed.</summary>
    public class CloseSceneException : Exception
    {

        public CloseSceneException(Scene scene, scene unityScene, SceneCollection collection = null, string message = null)
            : base(message ?? "The scene could not be closed.")
        {
            this.scene = scene;
            this.unityScene = unityScene;
            this.collection = collection;
        }

        public Scene scene;
        public scene unityScene;
        public SceneCollection collection;

    }

}
