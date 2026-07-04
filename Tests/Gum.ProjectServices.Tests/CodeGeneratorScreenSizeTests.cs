using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Regression: GenerateApplyDefaultVariables has a "October 5, 2025 - Generate the screen size"
/// block that makes a FullyInCode Screen fill its parent (Width/HeightUnits = RelativeToParent),
/// but it only emitted that for OutputLibrary.MonoGameForms ("this.Visual.Width = ..."). Every
/// other Gum-API output library (plain MonoGame - which KNI/FNA projects also select, Raylib,
/// Skia) fell into an empty else branch, so a Screen with no explicit Width/Height in its .gusx
/// kept Gum.Wireframe.GraphicalUiElement's hardcoded 32x32 constructor default instead of filling
/// the window.
/// </summary>
public class CodeGeneratorScreenSizeTests : BaseTestClass
{
    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        LocalizationService localizationService = new LocalizationService();

        return new CodeGenerator(
            codeGenNameVerifier,
            localizationService,
            elementSettingsManager,
            directoryProvider);
    }

    private static ScreenSave CreateScreenWithNoSize(GumProjectSave project)
    {
        ScreenSave screen = new ScreenSave { Name = "MainMenuUI" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        project.Screens.Add(screen);
        return screen;
    }

    [Fact]
    public void FullyInCode_MonoGame_ScreenWithNoExplicitSize_FillsParent()
    {
        GumProjectSave project = Project;
        ScreenSave screen = CreateScreenWithNoSize(project);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                screen,
                elementSettings: null!,
                new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "MyGame" });

            code.ShouldContain("this.Width = 0f;");
            code.ShouldContain("this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;");
            code.ShouldContain("this.Height = 0f;");
            code.ShouldContain("this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;");
            // Not Forms-wrapped, so it must not emit the ".Visual." variant.
            code.ShouldNotContain("this.Visual.Width");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void FullyInCode_Skia_ScreenWithNoExplicitSize_FillsParent()
    {
        // Skia Screens with no BaseType also resolve to bare GraphicalUiElement (same fallback as
        // MonoGame/Raylib - see CodeGenerator.GetInheritance), so they need the same fill-in.
        GumProjectSave project = Project;
        ScreenSave screen = CreateScreenWithNoSize(project);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                screen,
                elementSettings: null!,
                new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.Skia, RootNamespace = "MyGame" });

            code.ShouldContain("this.Width = 0f;");
            code.ShouldContain("this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;");
            code.ShouldContain("this.Height = 0f;");
            code.ShouldContain("this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void FullyInCode_MonoGameForms_ScreenWithNoExplicitSize_StillFillsParentThroughVisual()
    {
        // Pin the pre-existing MonoGameForms behavior so the fix above doesn't regress it.
        GumProjectSave project = Project;
        ScreenSave screen = CreateScreenWithNoSize(project);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                screen,
                elementSettings: null!,
                new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGameForms, RootNamespace = "MyGame" });

            code.ShouldContain("this.Visual.Width = 0f;");
            code.ShouldContain("this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;");
            code.ShouldContain("this.Visual.Height = 0f;");
            code.ShouldContain("this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
