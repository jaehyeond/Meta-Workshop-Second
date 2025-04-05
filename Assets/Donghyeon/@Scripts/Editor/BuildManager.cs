#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public enum BuildType
{
    DEV, 
    TEST,
    REAL
}

public class BuildManager : Editor
{
    public const string DEV_SCRIPTING_DEFINE_SYMBOLS = "DEV_VER;MOREMOUNTAINS_TOOLS;MOREMOUNTAINS_FEEDBACKS;MOREMOUNTAINS_INTERFACE;MOREMOUNTAINS_TOOLS_FOR_MMFEEDBACKS;MOREMOUNTAINS_TEXTMESHPRO_INSTALLED;MOREMOUNTAINS_INVENTORYENGINE;MOREMOUNTAINS_CORGIENGINE;MOREMOUNTAINS_POSTPROCESSING_INSTALLED;UNITY_POST_PROCESSING_STACK_V2";
    public const string REAL_SCRIPTING_DEFINE_SYMBOLS = "MOREMOUNTAINS_TOOLS;MOREMOUNTAINS_FEEDBACKS;MOREMOUNTAINS_INTERFACE;MOREMOUNTAINS_TOOLS_FOR_MMFEEDBACKS;MOREMOUNTAINS_TEXTMESHPRO_INSTALLED;MOREMOUNTAINS_INVENTORYENGINE;MOREMOUNTAINS_CORGIENGINE;MOREMOUNTAINS_POSTPROCESSING_INSTALLED;UNITY_POST_PROCESSING_STACK_V2;";

    private static BuildType m_BuildType = BuildType.DEV;

    [MenuItem("Build/Set AOS DEV Build Settings")]
    public static void SetAOSDEVBuildSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, DEV_SCRIPTING_DEFINE_SYMBOLS);

        m_BuildType = BuildType.DEV;
    }

    [MenuItem("Build/Set AOS TEST Build Settings")]
    public static void SetAOSTESTBuildSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = true;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, DEV_SCRIPTING_DEFINE_SYMBOLS);

        m_BuildType = BuildType.TEST;
    }

    [MenuItem("Build/Set AOS REAL Build Settings")]
    public static void SetAOSREALBuildSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = true;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, REAL_SCRIPTING_DEFINE_SYMBOLS);

        m_BuildType = BuildType.REAL;
    }

    [MenuItem("Build/Start AOS Build")]
    public static void StartAOSBuild()
    {
        PlayerSettings.Android.keystoreName = "Builds/AOS/DreadNought.keystore";
        PlayerSettings.Android.keystorePass = "rla6384303!";
        PlayerSettings.Android.keyaliasName = "master";
        PlayerSettings.Android.keyaliasPass = "q1w2e3R$T%";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[]
        {
            "Assets/@Scenes/StartUp.unity",
            "Assets/@Scenes/MainMenu.unity",
            "Assets/@Scenes/BasicGame.unity"
        };
        buildPlayerOptions.target = BuildTarget.Android;
        string fileExtention = string.Empty;
        BuildOptions compressOption = BuildOptions.None;

        switch (m_BuildType)
        {
            case BuildType.DEV:
                fileExtention = "apk";
                compressOption = BuildOptions.CompressWithLz4;
                break;
            case BuildType.TEST:
            case BuildType.REAL:
                fileExtention = "aab";
                compressOption = BuildOptions.CompressWithLz4HC;
                break;
            default:
                break;
        }

        buildPlayerOptions.locationPathName = $"Builds/AOS/DreadNought_{Application.version}_{DateTime.Now.ToString("yyMMdd_HHmmss")}.{fileExtention}";
        buildPlayerOptions.options = compressOption;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;
        if(summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded. {summary.totalSize} bytes.");
        }
        else if(summary.result == BuildResult.Failed)
        {
            Debug.LogError($"Build failed");
        }
    }
}
#endif