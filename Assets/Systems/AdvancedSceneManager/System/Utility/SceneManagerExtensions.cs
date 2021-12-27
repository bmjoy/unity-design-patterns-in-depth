using AdvancedSceneManager.Core;
using UnityEditor;
using UnityEngine;
using static AdvancedSceneManager.SceneManager;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager
{
    public static class SceneManagerExtensions
    {

        /// <summary>Gets the runtime info of the associated scene to this <see cref="GameObject"/>.</summary>
        public static OpenSceneInfo Scene(this GameObject gameObject) =>
            gameObject
            ? utility.FindOpenScene(gameObject.scene)
            : null;

        /// <summary>Gets the runtime info of the associated scene to this <see cref="Component"/>.</summary>
        public static OpenSceneInfo Scene(this Component component) =>
            component && component.gameObject
            ? utility.FindOpenScene(component.gameObject.scene)
            : null;

        /// <summary>Gets the ASM runtime info of this <see cref="scene"/>.</summary>
        public static OpenSceneInfo Scene(this scene scene) =>
            utility.FindOpenScene(scene);

#if UNITY_EDITOR
        /// <summary>Finds the asm representation of this <see cref="SceneAsset"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Scene FindASMScene(this SceneAsset scene) =>
            assetManagement.FindSceneByPath(AssetDatabase.GetAssetPath(scene));
#endif

    }

}
