using AdvancedSceneManager.Models;
using System.Collections;
using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>Base interface for <see cref="ISceneOpen"/>, <see cref="ISceneClose"/>, <see cref="ICollectionOpen"/>, <see cref="ICollectionClose"/>. Does nothing on its own, used by <see cref="CallbackUtility"/>.</summary>
    public interface ISceneManagerCallbackBase
    { }

    /// <summary>Callback for when the scene that a <see cref="MonoBehaviour"/> is contained within is opened.</summary>
    public interface ISceneOpen : ISceneManagerCallbackBase
    {
        /// <inheritdoc cref="ISceneOpen"/>
        IEnumerator OnSceneOpen();
    }

    /// <summary>Callback for when the scene that a <see cref="MonoBehaviour"/> is contained within is closed.</summary>
    public interface ISceneClose : ISceneManagerCallbackBase
    {
        /// <inheritdoc cref="ISceneClose"/>
        IEnumerator OnSceneClose();
    }

    /// <summary>
    /// <para>Callback for when a scene in a collection that a <see cref="MonoBehaviour"/> is contained within is opened.</para>
    /// <para>Called before loading screen is hidden, if one is defined, or else just when collection has opened.</para>
    /// </summary>
    public interface ICollectionOpen : ISceneManagerCallbackBase
    {
        /// <inheritdoc cref="ICollectionOpen"/>
        IEnumerator OnCollectionOpen(SceneCollection collection);
    }

    /// <summary>
    /// <para>Callback for when a scene in a collection that a <see cref="MonoBehaviour"/> is contained within is closed.</para>
    /// <para>Called after loading screen has opened, if one is defined, or else just before collection is closed.</para>
    /// </summary>
    public interface ICollectionClose : ISceneManagerCallbackBase
    {
        /// <inheritdoc cref="ICollectionClose"/>
        IEnumerator OnCollectionClose(SceneCollection collection);
    }

    /// <summary>Callbacks for a <see cref="ScriptableObject"/> that has been set as extra data for a collection.</summary>
    public interface ICollectionExtraData : ICollectionOpen, ICollectionClose
    { }

}
