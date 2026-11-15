using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System.IO;
using System.Linq;

namespace Gum.Cli.Tests;

/// <summary>
/// <c>gumcli codegen</c> reads the engine profile the project references and does not generate the variables it
/// hides (#7724).
/// </summary>
public class CodegenEngineProfileTests : BaseTestClass
{
    private string CreateProjectWithSprite(string engineProfileReference)
    {
        File.WriteAllText(Path.Combine(TempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>MyGame</RootNamespace></PropertyGroup></Project>");
        string gumx = Path.Combine(TempDirectory, "Content", "GumProject", "GumProject.gumx");
        CliTestHelper.Run("new", gumx, "--template", "empty").ExitCode.ShouldBe(0);
        CliTestHelper.Run("codegen-init", gumx).ExitCode.ShouldBe(0);
        CodeOutputProjectSettingsManager settingsManager = new CodeOutputProjectSettingsManager(
            new SilentLogger(), new FixedProjectDirectoryProvider(Path.GetDirectoryName(gumx)! + Path.DirectorySeparatorChar));
        CodeOutputProjectSettings settings = settingsManager.CreateOrLoadSettingsForProject();
        settings.OutputLibrary = OutputLibrary.MonoGame;
        settings.ObjectInstantiationType = ObjectInstantiationType.FullyInCode;
        settingsManager.WriteSettingsForProject(settings);

        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = "SpriteInstance", BaseType = "Sprite", ParentContainer = screen });
        state.Variables.Add(new VariableSave { Name = "SpriteInstance.X", Value = 10f, SetsValue = true, Type = "float" });
        state.Variables.Add(new VariableSave { Name = "SpriteInstance.Rotation", Value = 45f, SetsValue = true, Type = "float" });
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = "MainScreen", ElementType = ElementType.Screen });
        project.EngineProfile = engineProfileReference;
        project.Save(gumx, saveElements: true);
        return gumx;
    }

    private string ReadGeneratedScreen() =>
        File.ReadAllText(Directory.GetFiles(TempDirectory, "MainScreen*.Generated.cs", SearchOption.AllDirectories).Single());

    [Fact]
    public void Codegen_ProfileHidesRotation_DoesNotGenerateIt()
    {
        string gumx = CreateProjectWithSprite("Engine.gumengine");
        File.WriteAllText(
            Path.Combine(Path.GetDirectoryName(gumx)!, "Engine.gumengine"),
            """{ "variableRules": [ { "pattern": "Sprite.Rotation", "action": "Hide" } ] }""");

        CliTestHelper result = CliTestHelper.Run("codegen", gumx);

        result.ExitCode.ShouldBe(0);
        string generated = ReadGeneratedScreen();
        generated.ShouldContain("SpriteInstance.X = 10f;");
        generated.ShouldNotContain("Rotation");
    }

    [Fact]
    public void Codegen_ProfileFileMissing_WarnsAndGeneratesEverything()
    {
        string gumx = CreateProjectWithSprite("Missing.gumengine");

        CliTestHelper result = CliTestHelper.Run("codegen", gumx);

        result.ExitCode.ShouldBe(0);
        result.StandardError.ShouldContain("was not found");
        ReadGeneratedScreen().ShouldContain("SpriteInstance.Rotation = 45f;");
    }

    [Fact]
    public void Codegen_NoProfile_GeneratesEverythingWithoutWarning()
    {
        string gumx = CreateProjectWithSprite("");

        CliTestHelper result = CliTestHelper.Run("codegen", gumx);

        result.ExitCode.ShouldBe(0);
        result.StandardError.ShouldNotContain("engine profile", Case.Insensitive);
        ReadGeneratedScreen().ShouldContain("SpriteInstance.Rotation = 45f;");
    }

    private class SilentLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }

        public void PrintError(string message) { }
    }
}
