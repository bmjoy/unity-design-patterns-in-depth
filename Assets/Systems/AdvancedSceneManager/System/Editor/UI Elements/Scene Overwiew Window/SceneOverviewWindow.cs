using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    public class SceneOverviewWindow : EditorWindow_UIElements<SceneOverviewWindow>, IUIToolkitEditor
    {

        public override string path => "AdvancedSceneManager/SceneOverview";

        public override bool autoReloadOnWindowFocus => false;

        bool IsExpanded(string key, bool? newValue)
        {

            if (newValue.HasValue)
            {
                expanded.Set(key, newValue.Value);
                Save();
                ReloadOverview((Profile)profileField.value);
            }

            return expanded.GetValue(key, true);

        }

        [SerializeField] private SerializableStringBoolDict expanded = new SerializableStringBoolDict();

        ObjectField profileField;
        VisualElement list;
        public override void OnEnable()
        {

            Coroutine().StartCoroutine();
            IEnumerator Coroutine()
            {

                var json = EditorPrefs.GetString("AdvancedSceneManager.SceneOverviewWindow", JsonUtility.ToJson(this));
                JsonUtility.FromJsonOverwrite(json, this);

                base.OnEnable();

                ReloadContent();

                while (!isMainContentLoaded)
                    yield return null;

                SceneManager.assetManagement.AssetsChanged -= OnEnable;
                SceneManager.assetManagement.AssetsChanged += OnEnable;

                minSize = new Vector2(520, 200);

                list = rootVisualElement.Q("root");

                profileField = rootVisualElement.Q<ObjectField>("profileField");

                Profile profile = (Profile)profileField.value;
                if (!profileField.value && !string.IsNullOrEmpty(profileField.viewDataKey))
                    profile = Profile.Find(p => p.name == profileField.viewDataKey);

                if (!profile)
                    profile = Profile.current;

                ReloadOverview(profile);

                yield return new WaitForSeconds(1);

                profileField.SetValueWithoutNotify(profile);
                profileField.viewDataKey = profile ? profile.name : "";
                profileField.RegisterValueChangedCallback(e =>
                {
                    ReloadOverview(e.newValue as Profile);
                });

            }

        }

        void OnDisable() =>
            Save();

        void Save()
        {
            var json = JsonUtility.ToJson(this);
            EditorPrefs.SetString("AdvancedSceneManager.SceneOverviewWindow", json);
        }

        void OnDestroy()
        {
            profileField.viewDataKey = "";
        }

        void ReloadOverview(Profile profile)
        {

            profileField.viewDataKey = profile ? profile.name : "";
            profileField.SetValueWithoutNotify(profile);

            var element = SceneOverviewUtility.CreateSceneOverview(this, SceneManager.assetManagement.scenes.ToArray(), profile, isExpanded: IsExpanded);
            list.Clear();
            list.Add(element);

        }

    }

}
