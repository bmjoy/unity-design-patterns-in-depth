#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0051 // Remove unused private members
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Object = UnityEngine.Object;
using Lazy.Utility;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>A profile, contains settings, collections and scenes.</summary>
    public class Profile : ScriptableObject, INotifyPropertyChanged
    {

        public static Profile[] FindAll() =>
            SceneManager.assetManagement.profiles.ToArray();

        public static Profile Find(Func<Profile, bool> predicate) =>
            FindAll().FirstOrDefault(predicate);

        public static Profile Find(string name) =>
            FindAll().FirstOrDefault(p => p.name == name);

        #region Scriptable object

        //Don't allow renaming from UnityEvent
        public new string name
        {
            get => this ? base.name : "";
            internal set => base.name = value;
        }

        /// <summary>
        /// Saves the scriptable object after modifying.
        /// <para>No effect in build.</para>
        /// </summary>
        public void Save() =>
            ScriptableObjectUtility.Save(this);

        /// <summary>
        /// <para>Mark scriptable object as dirty after modifying.</para>
        /// <para>No effect in build.</para>
        /// </summary>
        public void MarkAsDirty()
        {
#if UNITY_EDITOR
            if (this && AssetDatabase.LoadAssetAtPath<Profile>(AssetDatabase.GetAssetPath(this)) is Object o)
                EditorUtility.SetDirty(o);
#endif
        }

        #endregion
        #region Prefix

        internal const char ZeroWidthSpace = '​';
        internal static readonly string PrefixDelimiter = ZeroWidthSpace + " - " + ZeroWidthSpace;

        /// <summary>
        /// Gets the prefix that is used on collections in this profile.
        /// <para>This would be <see cref="name"/> + <see cref="PrefixDelimiter"/>.</para>
        /// </summary>
        internal string prefix => name + PrefixDelimiter;

        internal static string RemovePrefix(string name) =>
            name.Contains(PrefixDelimiter)
            ? name.Substring(name.IndexOf(PrefixDelimiter) + PrefixDelimiter.Length)
            : name;

        #endregion
        #region Current

#if UNITY_EDITOR
        static string currentProfileID
        {
            get => EditorPrefs.GetString("AdvancedSceneManager.ActiveProfile");
            set => EditorPrefs.SetString("AdvancedSceneManager.ActiveProfile", value);
        }
#else
        static string currentProfileID { get; set; }
#endif

        internal static Profile FindProfile(string name) =>
            Find(p => p.name == name);

        internal static Profile m_profile;

        /// <summary>Gets the currently active profile.</summary>
        public static Profile current
        {
            get
            {

                var id = currentProfileID;
                if (m_profile && m_profile.name == id)
                    return m_profile;

                if (!string.IsNullOrWhiteSpace(id) && !(m_profile && m_profile.name == id))
                    m_profile = FindProfile(id);

                return m_profile;

            }
#if UNITY_EDITOR
            set => SetProfile(value);
#endif
        }

        internal static void SetProfile(Profile value)
        {

            m_profile = value;
            currentProfileID = value ? value.name : "";
#if UNITY_EDITOR
            BuildSettingsUtility.UpdateBuildSettings();
            lastProfile = value ? value.name : "";
#endif
            onProfileChanged?.Invoke();

        }

        public static event Action onProfileChanged;

        #endregion
        #region Collection and scene lists

        [SerializeField] internal List<SceneCollection> m_collections = new List<SceneCollection>();
        [NonSerialized] internal ReadOnlyCollection<SceneCollection> _collections;

        /// <summary>Gets the collections contained within this profile.</summary>
        public ReadOnlyCollection<SceneCollection> collections =>
            _collections ?? (_collections = new ReadOnlyCollection<SceneCollection>(m_collections));

        /// <summary>
        /// Gets the scenes managed by this profile.
        /// <para>Includes both collection and standalone scenes.</para>
        /// </summary>
        public IEnumerable<Scene> scenes => collections.
            Where(c => c).
            SelectMany(c => c.AllScenes()).
            Concat(standaloneScenes).
            Concat(new[] { loadingScreen, splashScreen, startupLoadingScreen }).
            Where(s => s);

        #endregion
        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            MarkAsDirty();
        }

        [SerializeField] internal string m_loadingScreen;
        [SerializeField] internal string m_splashScreen;
        [SerializeField] private bool m_createCameraForSplashScreen = true;
        [SerializeField] private bool m_useDefaultPauseScreen = true;
        [SerializeField] private bool m_enableCrossSceneReferences = false;
        [SerializeField] internal string[] m_standalone = Array.Empty<string>();
        [SerializeField] private string m_startupLoadingScreen;
        [SerializeField] private ThreadPriority m_backgroundLoadingPriority = ThreadPriority.BelowNormal;
        [SerializeField] private bool m_enableChangingBackgroundLoadingPriority;

        /// <summary>The loading screen to use during startup.</summary>
        public Scene startupLoadingScreen
        {
            get => SceneManager.assetManagement.FindSceneByPath(m_startupLoadingScreen);
            set { m_startupLoadingScreen = value ? value.path : ""; OnPropertyChanged(); }
        }

        /// <summary>The default loading screen.</summary>
        public Scene loadingScreen
        {
            get => SceneManager.assetManagement.FindSceneByPath(m_loadingScreen);
            set { m_loadingScreen = value ? value.path : ""; OnPropertyChanged(); }
        }

        /// <summary>The splash screen.</summary>
        public Scene splashScreen
        {
            get => SceneManager.assetManagement.FindSceneByPath(m_splashScreen);
            set { m_splashScreen = value ? value.path : ""; OnPropertyChanged(); }
        }

        /// <summary>Automatically create camera if no main camera found during splash screen.</summary>
        public bool createCameraForSplashScreen
        {
            get => m_createCameraForSplashScreen;
            set { m_createCameraForSplashScreen = value; OnPropertyChanged(); }
        }

        /// <summary>Enables message when a cross-scene reference could not be resolved.</summary>
        public bool enableCrossSceneReferences
        {
            get => m_enableCrossSceneReferences;
            set { m_enableCrossSceneReferences = value; OnPropertyChanged(); }
        }

        /// <summary>Enables the default pause screen, has no effect while in play mode.</summary>
        public bool useDefaultPauseScreen
        {
            get => m_useDefaultPauseScreen;
            set { m_useDefaultPauseScreen = value; OnPropertyChanged(); }

        }

        /// <summary><see cref="Application.backgroundLoadingPriority"/> setting is not saved, and must be manually set every time build or editor starts, this property persists the value and automatically sets it during startup.</summary>
        public ThreadPriority backgroundLoadingPriority
        {
            get => m_backgroundLoadingPriority;
            set { m_backgroundLoadingPriority = value; OnPropertyChanged(); }

        }

        /// <summary>Enable or disable ASM automatically changing <see cref="Application.backgroundLoadingPriority"/>.</summary>
        public bool enableChangingBackgroundLoadingPriority
        {
            get => m_enableChangingBackgroundLoadingPriority;
            set { m_enableChangingBackgroundLoadingPriority = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets standalone scenes that are set to be included to build.</summary>
        public Scene[] standaloneScenes => SceneManager.assetManagement.scenes.Where(s => s && (m_standalone?.Contains(s.path) ?? false)).ToArray();

        /// <summary>The layers defined in the tags tab in the scene manager window.</summary>
        public SceneTag[] tagDefinitions = new SceneTag[3] { SceneTag.Default, SceneTag.Persistent, SceneTag.DoNotOpen };

        #endregion
        #region Order

        /// <summary>Returns the order of this collection.</summary>
        public int Order(SceneCollection collection) =>
            collection && collections != null
            ? collections.IndexOf(collection)
            : -1;

#if UNITY_EDITOR

        /// <summary>
        /// <para>Returns and/or sets the order of this collection in the scene manager window.</para>
        /// <para>Cannot use in build.</para>
        /// </summary>
        public int Order(SceneCollection collection, int? newIndex = null)
        {

            if (m_collections == null)
                m_collections = new List<SceneCollection>();

            if (newIndex.HasValue)
            {

                m_collections.Remove(collection);

                if (!newIndex.HasValue || newIndex.Value == -1)
                {
                    newIndex = collections.Count - 1;
                    if (newIndex < 0) newIndex = 0;
                }

                if (newIndex > collections.Count)
                    m_collections.AddRange(Enumerable.Repeat<SceneCollection>(null, newIndex.Value - collections.Count).ToArray());

                m_collections.Insert(newIndex.Value, collection);

                //MarkAsDirty();
                //AssetDatabase.SaveAssets();
                //AssetDatabase.Refresh();

            }

            return Order(collection);

        }

        #region Assets

        public SceneCollection CreateCollection(string name, Action<SceneCollection> initializeBeforeSave = null) =>
            SceneManager.assetManagement.Create(name, profile: this, initializeBeforeSave);

        public void Add(SceneCollection collection) =>
            SceneManager.assetManagement.Add(collection, profile: this);

        public void Remove(SceneCollection collection) =>
            SceneManager.assetManagement.Remove(collection);

        #endregion

#endif

        #endregion

#if UNITY_EDITOR

        static string lastProfile
        {
            get => PlayerPrefs.GetString("AdvancedSceneManager.LastProfile");
            set => PlayerPrefs.SetString("AdvancedSceneManager.LastProfile", value);
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            if (Find(lastProfile) is Profile profile && profile)
                SetProfile(profile);
        }

#endif

        [SerializeField] private TagList m_tags = new TagList();

        public SceneTag Tag(Scene scene, SceneTag setTo = null)
        {

            if (m_tags == null)
            {
                m_tags = new TagList();
                MarkAsDirty();
            }

            if (setTo != null)
            {
                m_tags.Set(scene ? scene.path : "", setTo.id);
                MarkAsDirty();
                scene.OnPropertyChanged();
            }

            return m_tags[scene ? scene.path : ""];

        }

#if UNITY_EDITOR

        /// <summary>
        /// <para>Sets the scene as standalone in this profile.</para>
        /// <para>Only available in editor.</para>
        /// </summary>
        public void SetStandalone(Scene scene, bool enabled)
        {

            if (m_standalone == null)
                m_standalone = Array.Empty<string>();

            var path = scene ? scene.path : "";
            if (!enabled)
                ArrayUtility.Remove(ref m_standalone, path);
            else if (!m_standalone.Contains(path))
                ArrayUtility.Add(ref m_standalone, path);

            m_standalone = m_standalone.Distinct().ToArray();

            BuildSettingsUtility.UpdateBuildSettings();
            EditorUtility.SetDirty(this);
            CoroutineUtility.Run(AssetDatabase.SaveAssets, after: 0.1f);

        }

        #region Delete collections on profile deletion

        public void Delete() =>
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(this));

        class OnAssetsChanged : UnityEditor.AssetModificationProcessor
        {

            private static AssetDeleteResult OnWillDeleteAsset(string asset, RemoveAssetOptions _)
            {

                var profile = AssetDatabase.LoadAssetAtPath<Profile>(asset);
                if (profile)
                {

                    if (!Prompt())
                        return AssetDeleteResult.FailedDelete;

                    AssetDatabase.DeleteAsset(SceneManager.assetManagement.path<SceneCollection>().assets + "/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile)));

                }

                return AssetDeleteResult.DidNotDelete;

            }

            static bool Prompt() =>
                EditorUtility.DisplayDialog("Deleting profile...", "Profile is about to be deleted, this will also delete all associated collections, are you sure?", "Yes", "No", DialogOptOutDecisionType.ForThisSession, "ASM.PromptDeleteCollections");

        }

        #endregion

#endif

    }

}
