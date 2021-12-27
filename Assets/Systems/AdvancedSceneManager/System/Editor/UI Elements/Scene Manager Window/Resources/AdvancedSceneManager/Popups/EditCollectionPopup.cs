using AdvancedSceneManager.Models;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    public class EditCollectionPopup : Popup<EditCollectionPopup>
    {

        public override string path => "AdvancedSceneManager/Popups/EditCollection/Popup";

        SceneCollection collection;
        Action onTitleChanged;
        Action onStartChanged;

        public void Refresh(SceneCollection collection, Action onTitleChanged = null, Action onStartChanged = null)
        {

            if (!Profile.current)
                return;

            rootVisualElement.SetLocked(AssetDatabase.GetAssetPath(collection));

            this.collection = collection;
            this.onTitleChanged = onTitleChanged;
            this.onStartChanged = onStartChanged;

            SceneManager.assetManagement.DelayOnCollectionTitleChanged(() => current == null);

            rootVisualElement.Q<TextField>("Collection-title").Setup(collection, nameof(collection.title), () => onTitleChanged?.Invoke());
            rootVisualElement.Q<EnumField>("Collection-StartupOption").Setup(collection, nameof(collection.startupOption), () => onStartChanged?.Invoke());
            rootVisualElement.Q<EnumField>("Collection-loadingPriority").
                SetEnabledExt(Profile.current.enableChangingBackgroundLoadingPriority).
                Setup(collection, nameof(collection.loadingPriority), tooltip: "The thread priority to use for the loading thread when opening this collection.\n\nHigher equals faster loading, but more processing time used, and will as such produce lag ingame.\n\nSo using high during loading screen, and low during background loading gameplay, is recommended.\n\nAuto will attempt to automatically decide.");

            rootVisualElement.Q<EnumField>("Collection-loadingScreenEnum").Setup(collection, nameof(collection.loadingScreenUsage), () => { UpdateLoadingScreen(collection); SetPosition(); });
            UpdateLoadingScreen(collection, setup: true);

            var activeScene = rootVisualElement.Q<SceneField>("Collection-activeSceneEnum");
            activeScene.labelFilter = collection.label;
            activeScene.RegisterValueChangedCallback(e => { collection.activeScene = e.newValue; SceneManagerWindow.Save(collection); RefreshActiveScene(); });

            RefreshActiveScene();
            void RefreshActiveScene() =>
                activeScene.SetValueWithoutNotify(collection.scenes.Contains(collection.activeScene) ? collection.activeScene : collection.scenes.FirstOrDefault());

            rootVisualElement.Q<ObjectField>("Collection-Extra-Data").Setup(collection, nameof(SceneCollection.extraData));

            rootVisualElement.style.width = 350;

            activeScene.OnSceneOpen += Close;

        }

        void UpdateLoadingScreen(SceneCollection collection, bool setup = false)
        {

            var loadingSceneField = rootVisualElement.Q<SceneField>("Collection-loadingScreen");
            loadingSceneField.labelFilter = "ASM:LoadingScreen";

            loadingSceneField.visible = collection.loadingScreenUsage == LoadingScreenUsage.Override;
            loadingSceneField.EnableInClassList("hidden", !loadingSceneField.visible);

            if (setup)
            {
                loadingSceneField.OnSceneOpen += Close;
                loadingSceneField.Setup(collection, nameof(collection.loadingScreen));
            }

        }

        protected override void OnReopen(EditCollectionPopup newPopup) =>
            newPopup.Refresh(collection, onTitleChanged, onStartChanged);

    }

}
