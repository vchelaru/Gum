using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

/// <summary>
/// Batch-mode entry point that builds the sample as a Windows x64 player:
/// <c>Unity.exe -batchmode -quit -projectPath Samples/UnityGum -executeMethod SampleBuild.Build [-il2cpp] [-stripping Low]</c>.
/// Output goes to Build/Mono or Build/IL2CPP. Run Unity/build-unity-package.ps1 first so the Gum package
/// has its DLLs.
/// </summary>
public static class SampleBuild
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    public static void Build()
    {
        bool il2cpp = Array.IndexOf(Environment.GetCommandLineArgs(), "-il2cpp") >= 0;
        var target = NamedBuildTarget.Standalone;

        // SkiaGameRendering's GPU path is Direct3D 11 only so far; anything else uses Gum's CPU fallback.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
        PlayerSettings.SetScriptingBackend(target, il2cpp ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);

        // IL2CPP uses Low stripping until a Minimal build is verified: RichTextKit is still built
        // against SkiaSharp 2.88 (#5529). -stripping overrides it.
        string[] args = Environment.GetCommandLineArgs();
        int strippingIndex = Array.IndexOf(args, "-stripping");
        ManagedStrippingLevel stripping = strippingIndex >= 0
            ? (ManagedStrippingLevel)Enum.Parse(typeof(ManagedStrippingLevel), args[strippingIndex + 1])
            : ManagedStrippingLevel.Low;
        PlayerSettings.SetManagedStrippingLevel(target, stripping);

        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects), ScenePath);
        }

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = $"Build/{(il2cpp ? "IL2CPP" : "Mono")}/UnityGumSample.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new Exception($"Build {report.summary.result}: {report.summary.totalErrors} error(s).");
        }
    }
}
