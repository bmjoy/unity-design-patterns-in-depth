using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

internal class DependencyManagerWindow : EditorWindow
{

    #region Open

#if !ASM

    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        if (!Application.isPlaying)
            EditorApplication.delayCall += Open;
    }

    [MenuItem("File/Scene Manager... %#m", priority = 205)]
    [MenuItem("Tools/Advanced Scene Manager/Window/Scene Manager Window", priority = 40)]
    static void MenuItem() =>
        Open();

#endif

    public static void Open()
    {

        if (Resources.FindObjectsOfTypeAll<DependencyManagerWindow>().FirstOrDefault() is DependencyManagerWindow window)
            window.Show();
        else
        {
            var w = GetWindow<DependencyManagerWindow>();
            w.titleContent = new GUIContent("Scene Manager");
            w.Show();
        }

    }

    #endregion

    Dictionary<Dependency, (Label check, Button install)> items = new Dictionary<Dependency, (Label check, Button install)>();

    VisualElement content;
    VisualElement list;
    Button reloadButton;

    void SetIsReloading(bool isReloading)
    {
        reloadButton.text = isReloading ? "Reloading..." : "Reload";
        content.SetEnabled(!isReloading);
    }

    public void CreateGUI()
    {

        var root = rootVisualElement;

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/AdvancedSceneManager/System/Dependency Manager/Editor/DependencyManagerWindow.uxml");
        var labelFromUXML = visualTree.CloneTree();
        root.Add(labelFromUXML);

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AdvancedSceneManager/System/Dependency Manager/Editor/DependencyManagerWindow.uss");
        root.styleSheets.Add(styleSheet);

        maxSize = minSize = new Vector2(508, 230 + (ASM.dependencies.Length * 16));

        content = root.Q("content");
        reloadButton = root.Q<Button>("reloadButton");
        list = root.Q("list");

        reloadButton.clickable.clicked += Reload;
        list.Clear();

        SetIsReloading(true);
        foreach (var dependency in ASM.dependencies)
        {

            var item = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/AdvancedSceneManager/System/Dependency Manager/Editor/ListItem.uxml").CloneTree();
            item.Q<Label>("packageName").text = dependency.packageName;

            items.Add(dependency, (item.Q<Label>("check"), item.Q<Button>("install")));
            list.Add(item);

            items[dependency].install.clicked += () =>
            {

                items[dependency].install.text = "Installing...";
                SetIsReloading(true);
                var request = Client.Add(dependency.GetManifestValue());

                EditorApplication.update += Update;

                void Update()
                {

                    if (!request.IsCompleted)
                        return;
                    EditorApplication.update -= Update;

                    if (request.Status == StatusCode.Failure)
                        Debug.LogError(request.Error.message);

                    Reload();

                }

            };

        }

        Reload();

    }

    void Reload()
    {

        SetIsReloading(true);
        var request = Client.List();
        EditorApplication.update += Update;

        void Update()
        {

            if (!request.IsCompleted)
                return;

            EditorApplication.update -= Update;
            if (request.Status == StatusCode.Success)
                AfterUpdate();
            else
                Debug.LogError(request.Error.message);

        }

        void AfterUpdate()
        {

            var deps = ASM.dependencies.ToDictionary(d => d.packageName, d => false);
            foreach (var dependency in items)
            {

                var isInstalled = request.Result.Any(p => p.name == dependency.Key.packageName);
                deps[dependency.Key.packageName] = isInstalled;

                dependency.Value.check.style.display =
                    isInstalled
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

                dependency.Value.install.style.display =
                    !isInstalled
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            }

            foreach (var (_, install) in items.Values)
                install.text = "Install";

            if (deps.All(d => d.Value))
                OnAllInstalled();

            SetIsReloading(false);

        }

    }

    public static bool hasJustInstalledDependencies
    {
        get => EditorPrefs.GetBool("AdvancedSceneManager.hasJustInstalledDependencies");
        set => EditorPrefs.SetBool("AdvancedSceneManager.hasJustInstalledDependencies", value);
    }

    void OnAllInstalled()
    {
        ScriptingDefineUtility.Set(ASM.pragma);
        hasJustInstalledDependencies = true;
        Close();
    }

}