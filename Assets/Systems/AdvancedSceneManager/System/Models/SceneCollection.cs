using UnityEngine;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using Object = UnityEngine.Object;
using AdvancedSceneManager.Core;
using System.Collections.Generic;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Models
{

    public enum LoadingScreenUsage
    {
        DoNotUse, UseDefault, Override
    }

    public enum CollectionStartupOption
    {
        DoNotOpen, Open, OpenAsPersistent
    }

    /// <summary>
    /// <para>Wrapper for <see cref="ThreadPriority"/>, adds <see cref="CollectionThreadPriority.Auto"/>.</para>
    /// <see cref="ThreadPriority"/>: <inheritdoc cref="ThreadPriority"/>
    /// </summary>
    public enum CollectionThreadPriority
    {

        /// <summary>Automatically decide <see cref="ThreadPriority"/> based on if loading screen is open.</summary>
        Auto = -2,

        /// <inheritdoc cref="ThreadPriority.Low"/>
        Low = ThreadPriority.Low,

        /// <inheritdoc cref="ThreadPriority.BelowNormal"/>
        BelowNormal = ThreadPriority.BelowNormal,

        /// <inheritdoc cref="ThreadPriority.Normal"/>
        Normal = ThreadPriority.Normal,

        /// <inheritdoc cref="ThreadPriority.High"/>
        High = ThreadPriority.High,

    }

    /// <summary>A <see cref="SceneCollection"/> contains a list of <see cref="Scene"/>, all of which are opened when the <see cref="SceneCollection"/> is opened (except for scenes tagged DoNotOpen).</summary>
    /// <remarks>Only one <see cref="SceneCollection"/> can be open at a time.</remarks>
    public partial class SceneCollection : ScriptableObject, IReadOnlyList<Scene>, ISceneObject
#if UNITY_EDITOR
        , INotifyPropertyChanged
#endif
    {

        #region ScriptableObject

        /// <summary>
        /// <para>Mark scriptable object as dirty after modifying.</para>
        /// <para>No effect in build.</para>
        /// </summary>
        internal void MarkAsDirty()
        {
#if UNITY_EDITOR
            if (this && AssetDatabase.LoadAssetAtPath<SceneCollection>(AssetDatabase.GetAssetPath(this)) is Object o)
                EditorUtility.SetDirty(this);
#endif
        }

        #endregion
        #region INotifyPropertyChanged

        /// <summary>Occurs when a property changes on this collection.</summary>
        /// <remarks>Only available in editor.</remarks>
        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>Used by <see cref="SceneManager.AssetManagement.Rename{T}(T, string)"/> to set name after renaming. Don't use directly.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void SetName(string name)
        {
            if (this)
                base.name = name;
        }

        /// <summary>Raises <see cref="PropertyChanged"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void OnPropertyChanged() =>
            OnPropertyChanged(string.Empty);

        #endregion
        #region IList<Scene>

        public int Count => scenes.Length;
        public bool IsReadOnly => true;
        public Scene this[int index] => ((IList<Scene>)scenes)[index];

        public bool Contains(Scene item) => scenes.Contains(item);
        public IEnumerator<Scene> GetEnumerator() => ((IEnumerable<Scene>)scenes).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => scenes.GetEnumerator();

        #endregion
        #region Fields

        [SerializeField] internal string[] m_scenes;
        [SerializeField] private LoadingScreenUsage m_loadingScreenUsage = LoadingScreenUsage.UseDefault;
        [SerializeField] internal string m_loadingScreen;
        [SerializeField] internal string m_activeScene;
        [SerializeField] private CollectionStartupOption m_startupOption;
        [SerializeField] private CollectionThreadPriority m_loadingPriority = CollectionThreadPriority.Auto;
        [SerializeField] private ScriptableObject m_extraData;

        #endregion
        #region Properties

        ///<inheritdoc cref="Object.name"/>
        public new string name
        {
            get => m_title;
            internal set
            {
                base.name = value;
#if UNITY_EDITOR
                SceneManager.assetManagement.Rename(this, value);
#endif
            }
        }

        [SerializeField] private string m_title = "New Collection";

        /// <summary>The title of this collection.</summary>
        public string title
        {
            get => m_title;
            set { m_title = value; name = FindProfile().prefix + value; OnPropertyChanged(); }
        }

        internal void SetTitle(string title) =>
            m_title = title;

        /// <summary>
        /// <para>The extra data that is associated with this collection.</para>
        /// <para>Use <see cref="ExtraData{T}"/> to cast it to the desired type.</para>
        /// </summary>
        public ScriptableObject extraData
        {
            get => m_extraData;
            set { m_extraData = value; OnPropertyChanged(); }
        }

        /// <summary>Gets the scenes in this collection, note that some might be null if no reference is added in scene manager window.</summary>
        public Scene[] scenes
        {
            get
            {
                var scenes = SceneManager.assetManagement.scenes;
                return m_scenes?.Select(SceneManager.assetManagement.FindSceneByPath)?.ToArray() ?? Array.Empty<Scene>();
            }
            set => m_scenes = value?.Select(s => s ? s.path : "")?.ToArray() ?? Array.Empty<string>();
        }

        /// <summary>The loading screen that is associated with this collection.</summary>
        public Scene loadingScreen
        {
            get => SceneManager.assetManagement.FindSceneByPath(m_loadingScreen);
            set { m_loadingScreen = value ? value.path : ""; OnPropertyChanged(); }
        }

        /// <summary>Specifies what loading screen to use.</summary>
        public LoadingScreenUsage loadingScreenUsage
        {
            get => m_loadingScreenUsage;
            set { m_loadingScreenUsage = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies the scene that should be activated after collection is opened.</summary>
        public Scene activeScene
        {
            get => SceneManager.assetManagement.FindSceneByPath(m_activeScene);
            set { m_activeScene = value ? value.path : ""; OnPropertyChanged(); }
        }

        /// <summary>Specifies startup option.</summary>
        public CollectionStartupOption startupOption
        {
            get => m_startupOption;
            set { m_startupOption = value; OnPropertyChanged(); }
        }

        /// <summary>The thread priority to use when opening this collection.</summary>
        public CollectionThreadPriority loadingPriority
        {
            get => m_loadingPriority;
            set { m_loadingPriority = value; OnPropertyChanged(); }
        }

        /// <summary>The label of this collection, can be used as a filter in object picker and project explorer to only show scenes that are contained within this collection.</summary>
        internal string label => "ASM:" + title.Replace(" ", "");

        #endregion
        #region Methods

        /// <inheritdoc cref="CollectionManager.Open"/>
        public SceneOperation Open() => SceneManager.collection.Open(this);

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public SceneOperation Toggle() => SceneManager.collection.Toggle(this);

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public SceneOperation Toggle(bool enabled) => SceneManager.collection.Toggle(this, enabled);

        /// <inheritdoc cref="CollectionManager.Reopen"/>
        public SceneOperation Reopen() => SceneManager.collection.Reopen();

        /// <inheritdoc cref="CollectionManager.Close"/>
        public SceneOperation Close() => SceneManager.collection.Close();

        /// <inheritdoc cref="CollectionManager.IsOpen"/>
        public bool IsOpen() => SceneManager.collection.IsOpen(this);

        #region UnityEvent

        /// <inheritdoc cref="CollectionManager.Open"/>
        public void OpenEvent() => Open();

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public void ToggleEvent() => Toggle();

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public void ToggleEvent(bool enabled) => Toggle(enabled);

        /// <inheritdoc cref="CollectionManager.Reopen"/>
        public void ReopenEvent() => Reopen();

        /// <inheritdoc cref="CollectionManager.Close"/>
        public void CloseEvent() => Close();

        #endregion

        /// <summary>Gets all scenes contained in this collection, including overriden loading screen, if set.</summary>
        public Scene[] AllScenes() =>
            (loadingScreenUsage == LoadingScreenUsage.Override && loadingScreen)
            ? scenes.Concat(new[] { loadingScreen }).ToArray()
            : scenes;

        /// <summary>Find the <see cref="Profile"/> that this collection is associated with.</summary>
        public Profile FindProfile() =>
            Profile.Find(p => p && p.collections.Contains(this));

        /// <summary>Finds the <see cref="SceneCollection"/> with the specified name.</summary>
        public static SceneCollection Find(string name, bool onlyActiveProfile = true) =>
            SceneManager.assetManagement.collections.FirstOrDefault(c => c.name == name && (!onlyActiveProfile || c.FindProfile() == Profile.current));

        /// <summary>Casts and returns <see cref="extraData"/> as the specified type. Returns null if invalid type.</summary>
        public T ExtraData<T>() where T : ScriptableObject =>
            extraData as T;

        #endregion

    }

}
