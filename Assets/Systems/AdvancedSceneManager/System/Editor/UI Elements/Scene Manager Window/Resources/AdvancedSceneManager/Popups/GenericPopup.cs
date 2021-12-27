#pragma warning disable IDE0017 // Simplify object initialization
#pragma warning disable IDE0051 // Remove unused private members

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    public class GenericPopup : Popup<GenericPopup>
    {

        public override string path => "AdvancedSceneManager/Popups/PickTag/Popup";
        //protected override bool EnableBorder => false;

        /// <summary>Represents an <see cref="Item"/> separator. default keyword can also be used.</summary>
        public static Item Separator => default;

        public struct Item
        {

            public static Item Separator => default;

            public string name { get; set; }
            public bool isChecked { get; set; }
            public Action<bool> onClick { get; set; }
            public bool isCheckable { get; set; }
            public bool isEnabled { get; set; }

            public static Item Create(string name) =>
                new Item() { name = name, isEnabled = true };

            public static Item Create(string name, Action onClick) =>
                Create(name).WhenClicked(onClick);

            public Item AsCheckable()
            {
                isCheckable = true;
                return this;
            }

            public Item AsCheckable(Action<bool> onCheckedChanged)
            {
                isCheckable = true;
                onClick = onCheckedChanged;
                return this;
            }

            public Item WithCheckedStatus(bool isChecked)
            {
                this.isChecked = isChecked;
                return this;
            }

            public Item WithEnabledState(bool isEnabled)
            {
                this.isEnabled = isEnabled;
                return this;
            }

            public Item WhenClicked(Action action)
            {
                onClick = (value) => action?.Invoke();
                return this;
            }

        }

        Item[] items;
        public void Refresh(params Item[] items)
        {
            this.items = items;
            rootVisualElement.Clear();
            foreach (var item in items)
            {

                if (string.IsNullOrWhiteSpace(item.name))
                {
                    var separator = new VisualElement();
                    separator.style.height = 2;
                    separator.style.SetMargin(vertical: 2);
                    separator.style.backgroundColor = Color.gray;
                    rootVisualElement.Add(separator);
                }
                else
                {
                    var button = new ToolbarToggle();
                    button.AddToClassList("MenuItem");
                    button.text = item.name;
                    button.SetEnabled(item.isEnabled);
                    button.RegisterValueChangedCallback(e => { if (!item.isCheckable) button.SetValueWithoutNotify(false); Close(); item.onClick?.Invoke(e.newValue); });
                    rootVisualElement.Add(button);
                }

            }
        }

        protected override void OnReopen(GenericPopup newPopup) =>
            newPopup.Refresh(items);

    }

}
