#pragma warning disable CS0649 // Field is not assigned to

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

using Component = UnityEngine.Component;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Core;

using unityScene = UnityEngine.SceneManagement.Scene;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>
    /// <para>A <see cref="Scene"/> is a <see cref="ScriptableObject"/> that represents a scene in Unity, and are automatically generated or updated when a scene is added, renamed, moved or removed.</para>
    /// <para>The advantage of doing it this way is that we can actually create variables in script that refers to a scene rather than an arbitrary int or magic string. This also allows us to open scenes directly from an <see cref="UnityEngine.Events.UnityEvent"/> and not have to use a proxy script.</para>
    /// </summary>
    public partial class Scene : ScriptableObject, ISceneObject, INotifyPropertyChanged,
        IEquatable<Scene>, IEquatable<OpenSceneInfo>, IEquatable<unityScene>, IEquatable<unityScene?>
#if UNITY_EDITOR
        , IEquatable<SceneAsset>
#endif
    {

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void OnPropertyChanged() =>
            OnPropertyChanged("");

        #endregion
        #region Fields

        [SerializeField] private string m_path;
        [SerializeField] private string m_assetID;

        #endregion
        #region Properties

        public new string name
        {
            get => this ? base.name : "(null)";
            set => SceneManager.assetManagement.Rename(this, value);
        }

        /// <summary>Used by <see cref="SceneManager.AssetManagement.Rename{T}(T, string)"/> to set name after renaming. Don't use directly.</summary>
        public void SetName(string name)
        {
            if (this)
                base.name = name;
        }

        /// <summary>
        /// <para>The path to the scene file, relative to the project folder.</para>
        /// <para>Automatically updated.</para>
        /// </summary>
        public string path
        {
            get => m_path;
            set { m_path = value; OnPropertyChanged(); }
        }

        ///<summary>
        /// <para>The id of the asset in the asset database.</para>
        /// <para>Automatically updated.</para>
        /// </summary>
        public string assetID
        {
            get => m_assetID;
            set { m_assetID = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// <para>The current layer of this <see cref="Scene"/>.</para>
        /// <para>Change tags using the Scene Manager Window, or <see cref="Profile.Tag(Scene, SceneTag)()"/>.</para>
        /// </summary>
        public SceneTag tag =>
            Profile.current && Profile.current.Tag(this) is SceneTag tag
            ? tag
            : SceneTag.Default;

        /// <summary>Gets whatever this scene is included in build, this would be if the scene is contained in a collection, or if it is used as a splash screen / loading screen.</summary>
#if UNITY_EDITOR
        public bool isIncluded => BuildSettingsUtility.IsIncluded(this);
#else
        public bool isIncluded => true;
#endif

        /// <summary>Gets if this scene is currently active.</summary>
        public bool isActive =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == path;

        #endregion
        #region Methods

        /// <inheritdoc cref="UtilitySceneManager.SetActive"/>
        public void SetActiveScene() => SceneManager.utility.SetActive(this);

        /// <inheritdoc cref="UtilitySceneManager.IsOpen"/>
        public IsOpenReturnValue IsOpen() => SceneManager.utility.IsOpen(this);

        /// <inheritdoc cref="SceneManagerBase.Open"/>
        public SceneOperation<OpenSceneInfo> Open() => SceneManager.standalone.Open(this);

        /// <inheritdoc cref="StandaloneManager.OpenSingle"/>
        public SceneOperation<OpenSceneInfo> OpenSingle() => SceneManager.standalone.OpenSingle(this);

        /// <inheritdoc cref="UtilitySceneManager.Reopen"/>
        public SceneOperation<OpenSceneInfo> Reopen() => SceneManager.utility.Reopen(GetOpenSceneInfo());

        /// <inheritdoc cref="UtilitySceneManager.Toggle"/>
        public SceneOperation Toggle() => SceneManager.utility.Toggle(this);

        /// <inheritdoc cref="UtilitySceneManager.Toggle"/>
        public SceneOperation Toggle(bool enabled) => SceneManager.utility.Toggle(this, enabled);

        /// <inheritdoc cref="UtilitySceneManager.Close"/>
        public SceneOperation Close() => SceneManager.utility.Close(GetOpenSceneInfo());

        /// <inheritdoc cref="StandaloneManager.Preload(Scene, bool)"/>
        public SceneOperation<PreloadedSceneHelper> Preload() => SceneManager.standalone.Preload(this);

        #region UnityEvent

        /// <inheritdoc cref="SceneManagerBase.Open"/>
        public void OpenEvent() => Open();

        /// <inheritdoc cref="StandaloneManager.OpenSingle"/>
        public void OpenSingleEvent() => OpenSingle();

        /// <inheritdoc cref="SceneManagerBase.Reopen"/>
        public void ReopenEvent() => Reopen();

        /// <inheritdoc cref="SceneManagerBase.Toggle"/>
        public void ToggleEvent() => Toggle();

        /// <inheritdoc cref="SceneManagerBase.Toggle"/>
        public void ToggleEvent(bool enabled) => Toggle(enabled);

        /// <inheritdoc cref="SceneManagerBase.Close"/>
        public void CloseEvent() => Close();

        #endregion
        #region Find

        /// <inheritdoc cref="UtilitySceneManager.FindOpenScene(Scene)"/>
        public OpenSceneInfo GetOpenSceneInfo() => SceneManager.utility.FindOpenScene(this);

        /// <summary>Finds which collections that this scene is a part of.</summary>
        public (SceneCollection collection, bool asLoadingScreen)[] FindCollections(bool allProfiles = false) =>
            allProfiles
            ? FindCollections(null)
            : FindCollections(Profile.current);

        /// <summary>Finds which collections that this scene is a part of.</summary>
        public (SceneCollection collection, bool asLoadingScreen)[] FindCollections(Profile profile) =>
            (profile ? profile.collections : SceneManager.assetManagement.collections).
            Where(c => c && c.scenes != null && c.scenes.Contains(this)).Select(c => (c, LoadingScreenUtility.FindLoadingScreen(c) == this)).ToArray();

        /// <summary>Gets the root game objects in this <see cref="Scene"/>, only works if scene is loaded.</summary>
        public IEnumerable<GameObject> GetRootGameObjects() =>
            GetOpenSceneInfo()?.unityScene?.GetRootGameObjects() ?? Array.Empty<GameObject>();

        /// <summary>Finds the object in the heirarchy of this <see cref="Scene"/>.</summary>
        /// <remarks>Only works if scene is loaded.</remarks>
        public T FindObject<T>() where T : Component =>
            FindObjects<T>().FirstOrDefault();

        /// <summary>Finds the objects in the heirarchy of this <see cref="Scene"/>.</summary>
        /// <remarks>Only works if scene is loaded.</remarks>
        public IEnumerable<T> FindObjects<T>() where T : Component =>
            GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>()).OfType<T>();

        /// <summary>Finds the scene with the specified name.</summary>
        public static Scene Find(string name, SceneCollection inCollection = null, Profile inProfile = null) =>
            SceneUtility.Find(name, inCollection, inProfile).FirstOrDefault();

        /// <summary>Finds the scenes with the specified name.</summary>
        public static IEnumerable<Scene> FindAll(string name, SceneCollection inCollection = null, Profile inProfile = null) =>
            SceneUtility.Find(name, inCollection, inProfile);

        #endregion

        //Called when scene is renamed or moved
        internal void UpdateAsset(string assetID = null, string path = null)
        {
            if (assetID != null) m_assetID = assetID;
            if (path != null) m_path = path;
        }

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

        public bool Equals(Scene scene) => scene && (scene.assetID == assetID || scene.path == path);
        public bool Equals(OpenSceneInfo scene) => scene != null && (Equals(scene.scene) || Equals(scene.unityScene));
        public bool Equals(unityScene scene) => scene.path == path;
        public bool Equals(unityScene? scene) => scene.HasValue && scene.Value.path == path;

#if UNITY_EDITOR

        public bool Equals(SceneAsset scene)
        {

            if (!scene)
                return false;

            var path = AssetDatabase.GetAssetPath(scene);
            return path == this.path || AssetDatabase.AssetPathToGUID(path) == assetID;

        }

#endif

        #endregion

    }

}
