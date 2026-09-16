using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FlowNativeBuild
{
    [MenuItem("FLOW/Build Android APK")]
    public static void AndroidApk() { Build(BuildTarget.Android, false); }

    [MenuItem("FLOW/Build Android App Bundle")]
    public static void AndroidBundle() { Build(BuildTarget.Android, true); }

    [MenuItem("FLOW/Export iOS Xcode project")]
    public static void IOS() { Build(BuildTarget.iOS, false); }

    private static void Build(BuildTarget target, bool bundle)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new BuildFailedException("Exit Play Mode before building FLOW.");

        BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
        if (!BuildPipeline.IsBuildTargetSupported(group, target))
            throw new BuildFailedException("Install the " + target + " build support module in Unity Hub first.");

        // Interactive target switches trigger script recompilation; restart the menu action afterwards.
        // Batch builds must select their target at editor startup instead.
        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            if (Application.isBatchMode)
                throw new BuildFailedException("Start Unity with -buildTarget " + target + " before executing this method.");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
                throw new BuildFailedException("Could not switch the active build target to " + target + ".");
            Debug.Log("FLOW: target switched. After compilation finishes, select the native build menu action again.");
            return;
        }

        if (!File.Exists("Assets/Scenes/MainMenu.unity")) FlowProjectBuilder.Build();
        FlowControlTests.Run();

        string[] scenes = EnabledScenes();
        if (scenes.Length == 0) throw new BuildFailedException("No enabled FLOW scenes exist.");
        string directory = Path.GetFullPath(Path.Combine("Builds", target.ToString()));
        Directory.CreateDirectory(directory);
        string output = target == BuildTarget.iOS ? Path.Combine(directory, "Xcode")
            : Path.Combine(directory, bundle ? "FLOW.aab" : "FLOW.apk");

        bool previousBundle = EditorUserBuildSettings.buildAppBundle;
        try
        {
            if (target == BuildTarget.Android) EditorUserBuildSettings.buildAppBundle = bundle;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.StrictMode
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("FLOW build ended with " + report.summary.result + ". See the Editor log for details.");
            Debug.Log("FLOW native output: " + output + ". Signing, store validation, and physical-device testing remain required.");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(output);
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBundle;
        }
    }

    private static string[] EnabledScenes()
    {
        var paths = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            if (!File.Exists(scene.path)) throw new BuildFailedException("Missing enabled scene: " + scene.path);
            paths.Add(scene.path);
        }
        return paths.ToArray();
    }
}
