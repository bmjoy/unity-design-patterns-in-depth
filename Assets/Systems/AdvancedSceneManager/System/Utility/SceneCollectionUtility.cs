using AdvancedSceneManager.Models;
using System.Linq;

namespace AdvancedSceneManager.Utility
{

    /// <summary>An utility class to perform actions on collections.</summary>
    public static class SceneCollectionUtility
    {

#if UNITY_EDITOR

        /// <summary>Creates a <see cref="SceneCollection"/>.</summary>
        /// <param name="name">The name of the collection.</param>
        /// <param name="profile">The profile to add this collection to. Defaults to <see cref="Profile.current"/>.</param>
        public static SceneCollection Create(string name, Profile profile = null)
        {

            if (!profile)
                profile = Profile.current;

            return Profile.current
                ? Profile.current.CreateCollection(name)
                : null;

        }

        /// <summary>Removes a <see cref="SceneCollection"/>.</summary>
        public static void Remove(SceneCollection collection) =>
            SceneManager.assetManagement.Remove(collection);

        /// <summary>Removes all null scenes in the collection.</summary>
        public static void RemoveNullScenes(SceneCollection collection)
        {
            collection.m_scenes = collection.m_scenes.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
            collection.Save();
        }

#endif

    }


}
