using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;

namespace AdvancedSceneManager
{

    /// <summary>
    /// <para>The core of Advanced Scene Manager, provides access to the following:</para>
    /// <para><see cref="assetManagement"/>, provides an overview over all scenes and collections in project and contains functions to manage them.</para>
    /// <para><see cref="collection"/>, contains functions to open or close collections or manage collection scenes.</para>
    /// <para><see cref="standalone"/>, contains functions to manage scenes outside that are not associated with the currently active collection.</para>
    /// <para><see cref="utility"/>, contains functions to manage scenes that may be open in either <see cref="standalone"/> or <see cref="collection"/>.</para>
    /// <para><see cref="editor"/>, a simplified scene manager to manages scenes in editor.</para>
    /// <para><see cref="runtime"/>, manages startup and quit processes of the game.</para>
    /// <para><see cref="settings"/>, settings of the scene manager that isn't stored in the profile.</para>
    /// </summary>
    public static partial class SceneManager
    {

        /// <summary>Provides an overview over all scenes and collections in project and contains functions to manage them</summary>
        public static AssetManagement assetManagement { get; } = new AssetManagement();

        /// <summary>Provides functions to open or close collections or manage collection scenes</summary>
        public static CollectionManager collection { get; } = new CollectionManager();

        /// <summary>Provides functions to manage scenes outside that are not associated with the currently active collection</summary>
        public static StandaloneManager standalone { get; } = new StandaloneManager();

        /// <summary>Provides functions to manage scenes that may be open in either <see cref="standalone"/> or <see cref="collection"/></summary>
        public static UtilitySceneManager utility { get; } = new UtilitySceneManager();

        /// <summary>Manages startup and quit processes of the game</summary>
        public static Runtime runtime { get; } = new Runtime();

        /// <summary>Settings of the scene manager that isn't stored in the profile.</summary>
        public static ASMSettings settings => ASMSettings.current;

        /// <summary>The currently active profile.</summary>
        public static Profile profile => Profile.current;

#if UNITY_EDITOR
        /// <summary>A simplified scene manager to manages scenes in editor.</summary>
        public static Core.Editor editor { get; } = new Core.Editor();
#endif

    }

}
