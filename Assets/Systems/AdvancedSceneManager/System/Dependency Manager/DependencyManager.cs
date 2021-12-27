#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0046 // Convert to conditional expression

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("plugin.asm.package-manager")]
[assembly: InternalsVisibleTo("PackageManager.Internal")]
internal static class ASM
{

    public static string version { get; } = GetVersion();

    const string versionFile = "Assets/AdvancedSceneManager/System/Dependency Manager/Resources/AdvancedSceneManager/version.txt";
    const string versionResource = "AdvancedSceneManager/version";

    static string GetVersion()
    {
        var resource = Resources.Load<TextAsset>(versionResource);
        if (resource)
            return resource.text;
        else
            return "1.3.1"; //This code was added in this version, and plugin code for earlier versions are compatible with this one
    }

    public static void SetVersion(string version)
    {
        Directory.GetParent(versionFile).Create();
        File.WriteAllText(versionFile, version);
    }

    public static string[] assemblyNames =
    {
        "AdvancedSceneManager",
        "AdvancedSceneManager.Editor",
        "plugin.asm.package-manager",
        thisAssembly,
    };

    public const string thisAssembly = "plugin.asm.dependency-manager";
    public const string pragma = "ASM";

    public static Dependency PackageManagerDependency { get; } = Dependency.OnGitPackage(packageName: "plugin.asm.package-manager", uri: "https://github.com/Lazy-Solutions/plugin.asm.package-manager.git");
    public static readonly Dependency[] dependencies = new Dependency[]
    {
        PackageManagerDependency,
        Dependency.OnGitPackage(packageName: "utility.lazy.coroutines", uri: "https://github.com/Lazy-Solutions/Unity.CoroutineUtility.git#asm"),
        Dependency.OnUnityPackage(packageName: "com.unity.editorcoroutines", version : "1.0.0" ),
    };

    public static bool IsInstalled()
    {
        var assemblies = AssetDatabase.FindAssets("t:asmdef").Select(AssetDatabase.GUIDToAssetPath);
        return assemblyNames.All(assembly => assemblies.Any(a => a.EndsWith(assembly + ".asmdef")));
    }

}

class Deactivator : AssetPostprocessor
{

    static void OnPostprocessAllAssets(string[] _, string[] deletedAssets, string[] __, string[] ___)
    {

        var removedAssemblies = deletedAssets.Where(a => AssetDatabase.IsValidFolder(a)).SelectMany(a => AssetDatabase.FindAssets("t:asmdef", new[] { a })).Concat(deletedAssets.Where(a => a.EndsWith(".asmdef"))).ToArray();
        var isDependencyRemoved = ASM.dependencies.Any(d => IsRemoved(d.packageName, isPackage: true));

        var isCoreAssemblyRemoved = !ASM.IsInstalled();

        if (isDependencyRemoved || isCoreAssemblyRemoved)
        {
#if ASM
            AdvancedSceneManager.Editor.SceneManagerWindow.Close();
#endif
            ScriptingDefineUtility.Unset(ASM.pragma);
        }

        bool IsRemoved(string packageName, bool isPackage) =>
            removedAssemblies.Any(a => a.StartsWith("Packages/" + packageName + "/")) || //Check package
            removedAssemblies.Any(a => a.EndsWith("/" + packageName + ".asmdef")); //Check in assets

    }

}

/// <summary>Represents a dependency.</summary>
public class Dependency
{

    public static Dependency OnGitPackage(string packageName, string uri) =>
        new Dependency() { packageName = packageName, uri = uri };

    public static Dependency OnUnityPackage(string packageName, string version = "1.0.0") =>
        new Dependency() { packageName = packageName, version = version };

    /// <summary>The package name of the dependency.</summary>
    public string packageName;

    /// <summary>The version number of the dependency, not supported for git packages.</summary>
    public string version;

    /// <summary>The git uri of the dependency, not supported for unity packages.</summary>
    public string uri;

    /// <summary>Gets the value of the item in the package manifest, this is either <see cref="version"/> or <see cref="uri"/>.</summary>
    public string GetManifestValue()
    {
        if (!string.IsNullOrWhiteSpace(version))
            return packageName;
        else if (!string.IsNullOrWhiteSpace(uri))
            return uri;
        else
            throw new NullReferenceException($"Dependencies must define either '{nameof(version)}' or '{nameof(uri)}'.");
    }

}
