using System.Reflection;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Shouldly;
using SkiaSharp;

namespace Gum.Cli.Tests;

/// <summary>
/// One process that renders with skia, then registers MonoGame's standard runtimes, then renders with skia
/// again. The second skia SystemManagers.Initialize must put skia's runtimes back.
/// </summary>
public class ScreenshotAfterOtherBackendRegistrationTests : BaseTestClass
{
    [Fact]
    public void Screenshot_Skia_AfterMonoGameRegisteredStandardRuntimes_RendersWithSkiaRuntimes()
    {
        string projectPath = CreateRectangleProject();
        string outputPath = Path.Combine(TempDirectory, "Panel.png");
        string[] args =
        {
            "screenshot", projectPath, "Panel", "--output", outputPath, "--backend", "skia",
            "--background", "000000", "--width", "16", "--height", "16",
        };
        CliTestHelper.Run(args).ExitCode.ShouldBe(0);

        RegisterMonoGameStandardRuntimes();
        CliTestHelper result = CliTestHelper.Run(args);

        result.ExitCode.ShouldBe(0, result.StandardError);
        using SKBitmap bitmap = SKBitmap.Decode(outputPath);
        bitmap.GetPixel(0, 0).ShouldBe(SKColors.White);
    }

    // What MonoGame's SystemManagers.Initialize registers, without a graphics device.
    private static void RegisterMonoGameStandardRuntimes()
    {
        Type systemManagersType = Assembly.Load("MonoGameGum").GetType("RenderingLibrary.SystemManagers", throwOnError: true)!;
        object systemManagers = Activator.CreateInstance(systemManagersType)!;
        systemManagersType.GetMethod("RegisterComponentRuntimeInstantiations", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(systemManagers, new object[] { true });
    }

    private string CreateRectangleProject()
    {
        string projectPath = Path.Combine(TempDirectory, "Project.gumx");
        GumProjectSave project = new ProjectCreator().Create(projectPath);

        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = panel };
        panel.States.Add(state);
        panel.Instances.Add(new InstanceSave { Name = "Fill", BaseType = "Rectangle", ParentContainer = panel });
        state.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 16f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Height", Type = "float", Value = 16f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Fill.Width", Type = "float", Value = 16f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Fill.Height", Type = "float", Value = 16f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Fill.IsFilled", Type = "bool", Value = true, SetsValue = true });

        project.Components.Add(panel);
        project.ComponentReferences.Add(new ElementReference { Name = panel.Name, ElementType = ElementType.Component });
        project.Save(projectPath, saveElements: true);
        return projectPath;
    }
}
