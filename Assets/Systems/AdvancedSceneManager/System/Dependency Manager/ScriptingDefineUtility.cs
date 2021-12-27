using UnityEditor;

static class ScriptingDefineUtility
{

    public static BuildTargetGroup BuildTarget =>
        EditorUserBuildSettings.selectedBuildTargetGroup;

    static string Enumerate() =>
        PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTarget);

    public static bool IsSet(string name, out string actualString)
    {

        var defines = Enumerate();

        //We need to prevent finding substrings,
        //so lets check for 'name;' or ';name',
        //and then only check directly for name if no ';' exists (this means either zero or one defines defined)
        return defines.Contains(actualString = name + ";") ||
            defines.Contains(actualString = ";" + name) ||
            (!defines.Contains(";" + name) && defines.Contains(actualString = name));

    }

    public static void Unset(string name) =>
        Set(name, false);

    public static void Set(string name, bool enabled = true)
    {

        var defines = Enumerate();
        var originalDefines = defines;

        if (enabled && !IsSet(name, out _))
            defines += ";" + name;
        else if (!enabled && IsSet(name, out var actualString))
            defines = defines.Replace(actualString, "");

        if (defines != originalDefines)
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTarget, defines);

    }

}
