using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace Gum.Presentation.Tests.Managers;

/// <summary>
/// The tool hands its ProjectRestrictions to the code generator, so a variable the project's engine profile
/// hides is not generated (#7724).
/// </summary>
public class ProjectRestrictionsCodeGenerationTests : BaseTestClass
{
    private readonly string _folder;
    private readonly GumProjectSave _project;
    private readonly ProjectRestrictions _restrictions;

    public ProjectRestrictionsCodeGenerationTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "gum-restrictions-codegen-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        File.WriteAllText(
            Path.Combine(_folder, "Engine.gumengine"),
            """{ "variableRules": [ { "pattern": "Sprite.Rotation", "action": "Hide" } ] }""");

        _project = new GumProjectSave
        {
            Version = GumProjectSave.NativeVersion,
            FullFileName = Path.Combine(_folder, "Test.gumx"),
            EngineProfile = "Engine.gumengine",
        };
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);
        ObjectFinder.Self.GumProjectSave = _project;

        _restrictions = new ProjectRestrictions(new EngineProfileLoader(), new Mock<IOutputManager>().Object);
    }

    public override void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
        base.Dispose();
    }

    private CodeGenerator CreateCodeGenerator(IVariableRestrictions? restrictions)
    {
        Mock<INameVerifier> nameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        nameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        return new CodeGenerator(
            new CodeGenerationNameVerifier(nameVerifier.Object),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider,
            variableRestrictions: restrictions);
    }

    private ScreenSave CreateScreenWithSprite()
    {
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = "SpriteInstance", BaseType = "Sprite", ParentContainer = screen });
        state.Variables.Add(new VariableSave { Name = "SpriteInstance.X", Value = 10f, SetsValue = true, Type = "float" });
        state.Variables.Add(new VariableSave { Name = "SpriteInstance.Rotation", Value = 45f, SetsValue = true, Type = "float" });
        _project.Screens.Add(screen);
        return screen;
    }

    private string Generate(IVariableRestrictions? restrictions, ScreenSave screen) =>
        CreateCodeGenerator(restrictions).GetGeneratedCodeForElement(
            screen,
            elementSettings: null!,
            new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "MyGame" });

    [Fact]
    public void Generate_ProfileHidesRotation_SkipsRotationAndKeepsTheRest()
    {
        ScreenSave screen = CreateScreenWithSprite();

        string withoutRestrictions = Generate(null, screen);
        string withRestrictions = Generate(_restrictions, screen);

        withoutRestrictions.ShouldContain("SpriteInstance.Rotation = 45f;");
        withRestrictions.ShouldNotContain("Rotation");
        withRestrictions.ShouldContain("SpriteInstance.X = 10f;");
    }
}
