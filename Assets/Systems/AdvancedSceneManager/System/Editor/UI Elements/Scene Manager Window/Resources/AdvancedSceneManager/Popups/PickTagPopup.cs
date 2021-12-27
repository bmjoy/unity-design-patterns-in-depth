using AdvancedSceneManager.Models;
using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    public class PickTagPopup : Popup<PickTagPopup>
    {

        public override string path => "AdvancedSceneManager/Popups/PickTag/Popup";
        protected override bool EnableBorder => false;

        SceneTag tag;
        Action<SceneTag> onSelected;

        public void Refresh(SceneTag selected, Action<SceneTag> onSelected)
        {

            if (!Profile.current)
                return;

            this.tag = selected;
            this.onSelected = onSelected;

            rootVisualElement.Clear();
            foreach (var layer in Profile.current.tagDefinitions)
            {
                var toggle = new ToolbarToggle();
                toggle.AddToClassList("MenuItem");
                toggle.text = layer.name;
                toggle.SetValueWithoutNotify(layer == selected);
                toggle.RegisterValueChangedCallback(e => { onSelected?.Invoke(layer); Close(); });
                rootVisualElement.Add(toggle);
            }

        }

        protected override void OnReopen(PickTagPopup newPopup) =>
            newPopup.Refresh(tag, onSelected);

    }

}
