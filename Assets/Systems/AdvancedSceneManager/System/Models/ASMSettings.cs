#pragma warning disable CS0649 // Field is not assigned to
#pragma warning disable CS0067 // Event is not used

using AdvancedSceneManager.Utility;
using UnityEngine;
using AdvancedSceneManager.Models;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Core
{

    /// <summary>Global settings. Some properties are set during pre-build, and will as such only have values in build, these are prefixed by 'build'.</summary>
    public class ASMSettings : ScriptableObject
    {

        [SerializeField] private Profile m_profile;
        [SerializeField] private Color m_unitySplashScreenColor = Color.black;
        [SerializeField] private bool m_addLabelsToSceneAssets = false;
        [SerializeField] private bool m_inGameToolbarEnabled = true;

        /// <summary>The profile to use during build, this is set to <see cref="Profile.current"/> before building.</summary>
        public Profile buildProfile => m_profile;

        /// <summary>This is the color of the unity splash screen, used to make fade from splash screen to asm smooth, this is set before building. <see cref="Color.black"/> is used when the unity splash screen is disabled.</summary>
        public Color buildUnitySplashScreenColor => m_unitySplashScreenColor;

        /// <summary>
        /// <para>If <see langword="true"/>, then all SceneAssets will have their asset labels changed to match their associated <see cref="Scene"/> scriptable object.</para>
        /// <para>If <see langword="true"/>, then unity will randomly prompt to reload scenes, even if there are no unsaved user changes, this is due to asm changing labels during asset refresh.</para>
        /// <para>Note: If set to <see langword="false"/> in code, then labels will not be removed automatically.</para>
        /// </summary>
        public bool addLabelsToSceneAssets
        {
            get => m_addLabelsToSceneAssets;
            set => m_addLabelsToSceneAssets = value;
        }

        /// <summary>Enables or disables <see cref="InGameToolbarUtility"/> in builds.</summary>
        public bool inGameToolbarEnabled
        {
            get => m_inGameToolbarEnabled;
            set => m_inGameToolbarEnabled = value;
        }

        #region Scriptable object

        //Don't allow renaming from UnityEvent
        public new string name
        {
            get => base.name;
            internal set => base.name = value;
        }

        #region Singleton

        const string AssetPath = "Assets/Settings/Resources/AdvancedSceneManager/ASMSettings.asset";
        const string ResourcesPath = "AdvancedSceneManager/ASMSettings";

        internal static ASMSettings current =>
            ScriptableObjectUtility.GetSingleton<ASMSettings>(AssetPath, ResourcesPath);

        #endregion

#if UNITY_EDITOR

        /// <summary>
        /// <para>Saves the scriptable object after modifying.</para>
        /// <para>Only available in editor.</para>
        /// </summary>
        public void Save() =>
            ScriptableObjectUtility.Save(this);

        /// <summary>
        /// <para>Mark scriptable object as dirty after modifying.</para>
        /// <para>Only available in editor.</para>
        /// </summary>
        public void MarkAsDirty() =>
            EditorUtility.SetDirty(this);

#endif

        #endregion

#if UNITY_EDITOR

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            BuildEventsUtility.preBuild += PreBuild;
        }

        static void PreBuild()
        {
            SceneManager.settings.m_unitySplashScreenColor = PlayerSettings.SplashScreen.show ? PlayerSettings.SplashScreen.backgroundColor : Color.black;
            SceneManager.settings.m_profile = Profile.current;
            AssetDatabase.Refresh();
            SceneManager.settings.MarkAsDirty();
            AssetDatabase.SaveAssets();
        }

#endif

    }

}
