#pragma warning disable IDE0051 // Remove unused private members: Unity callbacks

using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
//using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AdvancedSceneManager.Editor {

  class AssetsSavedUtility : UnityEditor.AssetModificationProcessor {

    public static event Action<string[]> onAssetsSaved;

    static string[] OnWillSaveAssets(string[] paths) {
      onAssetsSaved?.Invoke(paths);
      return paths;
    }

  }

  /// <summary>A class that tracks when scenes are created, removed, renamed or moved in the project and automatically updates the list in <see cref="SceneManager"/>.</summary>
  internal class AssetRefreshUtility : AssetPostprocessor {

    static SynchronizationContext syncContext;
    [InitializeOnLoadMethod]
    static void OnLoad() {

      syncContext = SynchronizationContext.Current;

      Coroutine().StartCoroutine();
      IEnumerator Coroutine() {

        SceneManager.assetManagement.AssetsCleared += Refresh;
        AssetRefreshHelper.OnRefreshRequest += Refresh;
        EditorApplication.playModeStateChanged += state => {

          cancel = true;

#if UNITY_2019
                    EditorUtility.ClearProgressBar();
#else
          if (progressID.HasValue)
            UnityEditor.Progress.Remove(progressID.Value);
          progressID = null;
#endif

        };

        yield return new WaitForSeconds(1);
        Refresh();

      }
    }

    #region Triggers

    /// <summary>Refresh the scenes.</summary>
    public static void Refresh() {

      var currentScenes = SceneManager.assetManagement.scenes.Where(s => s).ToArray();
      var added = AssetDatabase.FindAssets("t:" + nameof(SceneAsset)).Select(AssetDatabase.GUIDToAssetPath);
      currentScenes = currentScenes.Where(s => s).ToArray();

      var removed = currentScenes.Where(scene => !added.Contains(scene.path)).Select(s => s.path);
      var moved = currentScenes.Select(s => (from: s.path, to: AssetDatabase.GUIDToAssetPath(s.assetID))).Where(s => s.from != s.to);

      Refresh(added.ToArray(), removed.ToArray(), moved.ToArray());

    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath) {

      if (importedAssets.Where(IsScene).Any() || deletedAssets.Where(IsScene).Any() || movedAssets.Where(IsScene).Any() || movedFromPath.Where(IsScene).Any())
        Refresh(importedAssets.Where(IsScene).ToArray(), deletedAssets.Where(IsScene).ToArray(), movedAssets.Select((path, i) => (movedFromPath[i], movedAssets[i])).ToArray());
      else if (deletedAssets.Any())
        Refresh();

    }

    static bool IsScene(string path) =>
        Path.GetExtension(path) == ".unity";

    #endregion
    #region Refresh

    static bool cancel;

    static ReadOnlyCollection<string> added;
    static ReadOnlyCollection<string> deleted;
    static ReadOnlyCollection<(string from, string to)> moved;

    static int progress;
    static string progressString;

    static void Progress(string message = "", bool increment = true) {
      //Debug.Log(message);
      if (increment)
        progress += 1;
      progressString = message;
    }

    internal delegate IEnumerator RefreshAction();
    delegate int RefreshActionCount();
    readonly static (RefreshAction action, RefreshActionCount progressCount)[] actions =
    {
            (MakeSureAssetsAddedToAssetManagement, () => 0),
            (RefreshDeletedFiles,                  () => deleted.Count),
            (RefreshAddedFiles,                    () => added.Count),
            (UpdateLabels,                         () => SceneManager.assetManagement.scenes.Count),
            (MakeSureSceneHelperExists,            () => 0),
        };

    internal delegate void OnRefresh(ReadOnlyCollection<string> added, ReadOnlyCollection<string> deleted, ReadOnlyCollection<(string from, string to)> moved);
    internal static event OnRefresh RefreshCallback;

    static readonly List<(string[] added, string[] deleted, (string from, string to)[] moved)> queuedRefreshes = new List<(string[] added, string[] deleted, (string from, string to)[] moved)>();

    static void Refresh(string[] added, string[] deleted, (string from, string to)[] moved) {

      if (!SceneManager.assetManagement.allowAutoRefresh)
        return;

#if UNITY_2021 || UNITY_2021_OR_NEWER
      if (UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
        return;
#else
            if (UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
                return;
#endif

      if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        return;

      //RefreshMoved has to run before regular actions
      if (moved.Any()) {
        AssetRefreshUtility.moved = new ReadOnlyCollection<(string from, string to)>(moved.ToList());
        RefreshMoved().StartCoroutine(onComplete: Refresh);
      } else
        DoRefresh(added, deleted);

    }

#if !UNITY_2019
    static int? progressID;
#endif
    static void DoRefresh(string[] added, string[] deleted) {

      Coroutine().StartCoroutine();
      IEnumerator Coroutine() {

        ClearProgress();

        cancel = false;

        AssetRefreshUtility.added = new ReadOnlyCollection<string>(added.ToList());
        AssetRefreshUtility.deleted = new ReadOnlyCollection<string>(deleted.ToList());

        var maxProgress = actions.Sum(action => action.progressCount.Invoke());
        float GetProgress() => progress / (float)maxProgress;

        EditorApplication.update -= UpdateProgress;
        EditorApplication.update += UpdateProgress;

        SceneManager.assetManagement.RemoveNull();

        progress = 0;

        AssetDatabase.DisallowAutoRefresh();
        for (int i = 0; i < actions.Length; i++) {
          yield return actions[i].action.Invoke();
          if (cancel)
            break;
        }
        AssetDatabase.AllowAutoRefresh();

        RefreshCallback?.Invoke(AssetRefreshUtility.added, AssetRefreshUtility.deleted, AssetRefreshUtility.moved);

        BuildSettingsUtility.UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorApplication.update -= UpdateProgress;
        ClearProgress();
        EditorApplication.delayCall += EditorApplication.QueuePlayerLoopUpdate;
        EditorApplication.QueuePlayerLoopUpdate();

        CoroutineUtility.Run(ClearProgress, after: 0.1f);

        void UpdateProgress() {
#if UNITY_2019
                    EditorUtility.DisplayProgressBar("Advanced Scene Manager: Refreshing assets...", progressString, GetProgress());
#else
          progressID ??= UnityEditor.Progress.Start("Refreshing assets...");
          UnityEditor.Progress.Report(progressID.Value, GetProgress(), progressString);
#endif
        }

        void ClearProgress() {
#if UNITY_2019
                    EditorUtility.ClearProgressBar();
#else
          if (progressID.HasValue)
            UnityEditor.Progress.Remove(progressID.Value);
          progressID = null;
#endif
        }

      }

    }

    #endregion
    #region Actions

    #region Moved files

    static IEnumerator RefreshMoved() {

      var allScenes = AssetDatabase.FindAssets("t:AdvancedSceneManager.Models.Scene").
          Select(id => AssetDatabase.LoadAssetAtPath<Scene>(AssetDatabase.GUIDToAssetPath(id))).
          Select(s => s).
          Where(s => s);

      foreach (var (from, to) in moved) {

        Progress($"Updating path: " + from + " -> " + to);

        foreach (var profile in SceneManager.assetManagement.profiles) {
          if (profile.m_loadingScreen == from) profile.m_loadingScreen = to;
          if (profile.m_splashScreen == from) profile.m_splashScreen = to;
        }

        foreach (var collection in SceneManager.assetManagement.collections) {

          for (int i = 0; i < collection.Count; i++)
            if (collection.m_scenes[i] == from)
              collection.m_scenes[i] = to;

          if (collection.m_loadingScreen == from) collection.m_loadingScreen = to;

        }

        var scene = SceneManager.assetManagement.FindSceneByPath(from);

        scene.UpdateAsset(path: to);
        SceneManager.assetManagement.Rename(scene, Path.GetFileNameWithoutExtension(to));

        yield return null;

      }

    }

    #endregion
    #region Added and deleted files

    static IEnumerator RefreshDeletedFiles() {
      foreach (var path in deleted.ToArray()) {

        Progress("Deleting: " + path);

        var scene = SceneManager.assetManagement.scenes.Find(path);

        if (scene)
          SceneManager.assetManagement.Remove(scene);

        yield return null;

      }
    }

    static IEnumerator RefreshAddedFiles() {
      foreach (var path in added.ToArray()) {
        Progress("Adding: " + path);
        SceneUtility.Create(path, createSceneScriptableObject: true);
      }
      yield return null;
    }

    #endregion
    #region Update labels

    static IEnumerator UpdateLabels() {

      foreach (var scene in SceneManager.assetManagement.scenes.Where(s => s).ToArray()) {

        Progress("Updating labels: " + scene.name, increment: false);

        if (!scene)
          continue;

        SetLabels(scene, scene);

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
        if (SceneManager.settings.addLabelsToSceneAssets)
          SetLabels(sceneAsset, scene);
        else
          ClearLabels(sceneAsset, scene);

      }

      yield return null;

      void ClearLabels(Object obj, Scene scene) {

        var oldLabels = AssetDatabase.GetLabels(obj);
        var labels = oldLabels.Where(l => !l.StartsWith("ASM:")).ToArray();

        if (!oldLabels.SequenceEqual(labels))
          AssetDatabase.SetLabels(obj, labels);

      }

      void SetLabels(Object obj, Scene scene) {

        var oldLabels = AssetDatabase.GetLabels(obj);
        var labels = oldLabels.Where(l => !l.StartsWith("ASM")).ToArray();
        var newLabels = scene.FindCollections(true).
            Where(c => c.collection).
            GroupBy(c => c.collection).
            Select(c => c.First()).
            Select(c => c.collection.label).
            Where(l => l != "ASM:").
            Distinct().
            ToArray();

        var file = File.ReadAllText(scene.path);
        if (file.Contains("isSplashScreen: 1"))
          ArrayUtility.Add(ref newLabels, "ASM:SplashScreen");
        else if (file.Contains("isLoadingScreen: 1"))
          ArrayUtility.Add(ref newLabels, "ASM:LoadingScreen");

        if (!newLabels.Any())
          return;

        ArrayUtility.AddRange(ref labels, newLabels);

        if (!oldLabels.SequenceEqual(labels))
          AssetDatabase.SetLabels(obj, labels);

      }

    }

    #endregion
    #region Make sure scene helper exists

    static IEnumerator MakeSureSceneHelperExists() {
      _ = SceneHelper.current;
      yield return null;
    }

    #endregion
    #region Make sure assets are added to asset management

    static IEnumerator MakeSureAssetsAddedToAssetManagement() {

      if (AssetRef.instance.profiles == null) AssetRef.instance.profiles = Array.Empty<Profile>();
      if (AssetRef.instance.collections == null) AssetRef.instance.collections = Array.Empty<SceneCollection>();
      if (AssetRef.instance.scenes == null) AssetRef.instance.scenes = Array.Empty<Scene>();

      foreach (var profile in AssetDatabase.FindAssets("t:Profile").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Profile>))
        if (!AssetRef.instance.profiles.Contains(profile))
          ArrayUtility.Add(ref AssetRef.instance.profiles, profile);

      yield return null;

      foreach (var collection in AssetDatabase.FindAssets("t:SceneCollection").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<SceneCollection>))
        if (!AssetRef.instance.collections.Contains(collection))
          ArrayUtility.Add(ref AssetRef.instance.collections, collection);

      yield return null;

      foreach (var scene in AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Scene>))
        if (!AssetRef.instance.scenes.Contains(scene))
          ArrayUtility.Add(ref AssetRef.instance.scenes, scene);

      AssetRef.instance.CleanUp();

      yield return null;

      EditorUtility.SetDirty(AssetRef.instance);

    }

    #endregion

    #endregion

  }

}
