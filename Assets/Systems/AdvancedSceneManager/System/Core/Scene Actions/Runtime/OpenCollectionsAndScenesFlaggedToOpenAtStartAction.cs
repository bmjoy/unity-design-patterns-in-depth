using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Opens all collections and scenes that are set to open at startup.</summary>
    public class OpenCollectionsAndScenesFlaggedToOpenAtStartAction : SceneAction
    {

        new readonly SceneCollection collection;
        readonly bool ignoreDoNotOpen;
        /// <param name="collection">Opens the specified collection after all other collections and scenes has been opened.</param>
        public OpenCollectionsAndScenesFlaggedToOpenAtStartAction(SceneCollection collection = null, bool ignoreDoNotOpen = false)
        {
            this.collection = collection;
            this.ignoreDoNotOpen = ignoreDoNotOpen;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Enables warnings during startup when a collection is opened, then immedietly closed, because another collection was opened, and did not contain any persistent scenes.
        /// <para>Only available in editor.</para>
        /// </summary>
        public static bool pointlessCollectionWarning
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.Warnings.pointlessCollection", true);
            set => EditorPrefs.SetBool("AdvancedSceneManager.Warnings.pointlessCollection", value);
        }

        /// <summary>
        /// Enables warnings during startup when a standalone scene is opened, then immediately closed, because it was not set as persistent.
        /// <para>Only available in editor.</para>
        /// </summary>
        public static bool pointlessSceneWarning
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.Warnings.pointlessScene", true);
            set => EditorPrefs.SetBool("AdvancedSceneManager.Warnings.pointlessScene", value);
        }

#endif

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            if (!Profile.current)
                yield break;

            //Open collections flagged to open at start
            var collections = Profile.current.collections.Where(c => c && c.startupOption != CollectionStartupOption.DoNotOpen).ToArray();
            if (collections.Any())
                foreach (var collection in collections)
                {

#if UNITY_EDITOR
                    if (SceneManager.collection &&
                        SceneManager.collection.openScenes.All(s => PersistentUtility.GetPersistentOption(s.unityScene.Value) == SceneCloseBehavior.Close) &&
                        pointlessCollectionWarning)
                        Debug.LogWarning("A collection was opened, then closed during startup that did not have any persistent scenes, this is more often than not pointless. If this was intentional, you may disable this warning in settings.");
#endif

                    yield return Open(collection);

                }
            else if (!collection)
                //Open first collection in list, if none are explicitly flagged to open at start
                yield return Open(Profile.current.collections.FirstOrDefault());

            if (collection && collection != SceneManager.collection.current)
                yield return Open(collection);

        }

        IEnumerator Open(SceneCollection collection)
        {
            return SceneManager.collection.
                Open(collection, ignoreLoadingScreen: true, force: ignoreDoNotOpen).
                WithAction(() => SetScenesPersistent(collection)).
                IgnoreQueue().
                SetParent(SceneManager.utility.currentOperation);
        }

        void SetScenesPersistent(SceneCollection collection)
        {
            if (collection.startupOption == CollectionStartupOption.OpenAsPersistent)
                foreach (var scene in collection.scenes)
                {
                    var s = scene.GetOpenSceneInfo()?.unityScene ?? default;
                    if (s != default)
                        PersistentUtility.Set(s, SceneCloseBehavior.KeepOpenAlways);
                }
        }

    }

}
