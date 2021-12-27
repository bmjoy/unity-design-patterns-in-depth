using AdvancedSceneManager.Models;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SceneAsset))]
    public class SceneAssetEditor : UnityEditor.Editor, IUIToolkitEditor
    {

        public VisualElement rootVisualElement { get; private set; }
        public Rect position { get; set; }

        Vector2 popupOffset = new Vector2(22, 0);
        protected override void OnHeaderGUI()
        {

            rootVisualElement.style.minHeight = Screen.height - 64;
            position = rootVisualElement.worldBound;
            rootVisualElement.style.marginTop = 16;

            if (OpenInEditorPopup.IsOpen(this))
                OpenInEditorPopup.SetOffset(popupOffset);

            base.OnHeaderGUI();

        }

        public override VisualElement CreateInspectorGUI()
        {

            var scenes = targets.OfType<SceneAsset>().ToArray();

            rootVisualElement = SceneOverviewUtility.CreateSceneOverview(
                this,
                scenes: scenes,
                allCheckboxDefaultValue: () => scenes.All(s => { var scene = s.FindASMScene(); return scene && scene.isIncluded; }),
                allCheckboxHandler: b =>
                {
                    foreach (var scene in scenes)
                    {
                        var s = scene.FindASMScene();
                        if (Profile.current)
                            Profile.current.SetStandalone(s, b);
                    }
                });

            return rootVisualElement;

        }

    }

}
