using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>
    /// <para>An helper for opening and closing scenes or scene collections.</para>
    /// <para>Most common use case would be to open / close scenes or scene collections from <see cref="UnityEngine.Events.UnityEvent"/>.</para>
    /// </summary>
    public static class SceneHelper
    {
        public static ASM current => ASM.current;
    }

    /// <summary>This is <see cref="SceneHelper"/>, but we don't want the script to show up in object picker to avoid confusion, using a different name seems to be the only way?</summary>
    [AddComponentMenu("")]
    public class ASM : ScriptableObject
    {

        //Prevent renaming from UnityEvent
        public new string name { get; }

        #region Singleton

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        static void EnsureExists() =>
            _ = current;

        const string AssetPath = "Assets/Settings/Resources/AdvancedSceneManager/SceneHelper.asset";
        const string ResourcesPath = "AdvancedSceneManager/SceneHelper";

        internal static ASM current =>
            ScriptableObjectUtility.GetSingleton<ASM>(AssetPath, ResourcesPath);

        #endregion
        #region Open

        /// <inheritdoc cref="Core.CollectionManager.Open(SceneCollection, bool)"/>
        public void Open(SceneCollection collection) => SceneManager.collection.Open(collection);

        /// <inheritdoc cref="Core.CollectionManager.Reopen"/>
        public void ReopenCollection() => SceneManager.collection.Reopen();

        /// <inheritdoc cref="Core.CollectionManager.Open(Scene)"/>
        public void Open(Scene scene) => SceneManager.standalone.Open(scene);

        /// <inheritdoc cref="Core.SceneManagerBase.Reopen(OpenSceneInfo)"/>
        public void Reopen(Scene scene) => SceneManager.utility.Reopen(scene ? scene.GetOpenSceneInfo() : null);

        /// <inheritdoc cref="Core.StandaloneManager.OpenSingle(Scene)"/>
        public void OpenSingle(Scene scene) => SceneManager.standalone.OpenSingle(scene);

        #endregion
        #region Close

        /// <inheritdoc cref="Core.CollectionManager.Close"/>
        public void CloseCollection() => SceneManager.collection.Close();

        /// <inheritdoc cref="Core.UtilitySceneManager.Close(OpenSceneInfo)"/>
        public void Close(Scene scene) => SceneManager.utility.Close(scene.GetOpenSceneInfo());

        #endregion
        #region Toggle

        /// <inheritdoc cref="Core.CollectionManager.Toggle(SceneCollection, bool?)"/>
        public void Toggle(SceneCollection collection) => SceneManager.collection.Toggle(collection);

        /// <inheritdoc cref="Core.CollectionManager.Toggle(SceneCollection, bool?)"/>
        public void Toggle(SceneCollection collection, bool enabled) => SceneManager.collection.Toggle(collection, enabled);

        /// <inheritdoc cref="Core.SceneManagerBase.Toggle(OpenSceneInfo, bool?)"/>
        public void Toggle(Scene scene) => SceneManager.utility.Toggle(scene);

        /// <inheritdoc cref="Core.SceneManagerBase.Toggle(OpenSceneInfo, bool?)"/>
        public void Toggle(Scene scene, bool enabled) => SceneManager.utility.Toggle(scene, enabled);

        #endregion

        /// <inheritdoc cref="Core.CollectionManager.IsOpen(SceneCollection)"/>
        public bool IsOpen(SceneCollection collection) => SceneManager.collection.IsOpen(collection);

        /// <inheritdoc cref="Core.UtilitySceneManager.IsOpen(Scene)"/>
        public IsOpenReturnValue IsOpen(Scene scene) => SceneManager.utility.IsOpen(scene);

        /// <inheritdoc cref="Core.SceneManagerBase.Preload(Scene)"/>
        public SceneOperation<PreloadedSceneHelper> Preload(Scene scene) => SceneManager.standalone.Preload(scene);

        /// <inheritdoc cref="Core.UtilitySceneManager.SetActive(Scene)"/>
        public void SetActivateScene(Scene scene) => SceneManager.utility.SetActive(scene);

        /// <summary>Finds the collections that are associated with this <see cref="Scene"/>.</summary>
        public (SceneCollection collection, bool asLoadingScreen)[] FindCollections(Scene scene) => scene.FindCollections();

        /// <inheritdoc cref="SceneManager.Quit(bool)"/>
        public void Quit() => SceneManager.runtime.Quit();

        /// <inheritdoc cref="SceneManager.Startup.Restart()"/>
        public void Restart() => SceneManager.runtime.Restart();

        /// <inheritdoc cref="CollectionManager.Reopen"/>
        public void RestartCollection() => SceneManager.collection.Reopen();

    }

}
