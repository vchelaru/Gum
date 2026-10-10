using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A Forms component whose BaseType is Text (Meadow's Label) has a Visual typed InteractiveGue. Text-only
/// members such as Font, FontSize, IsBold and the Red/Green/Blue color channels live on TextRuntime, so
/// assigning them through Visual does not compile (#5933).
/// </summary>
public class CodeGeneratorTextBasedFormsComponentTests : BaseTestClass
{
    // Text-only members that exist on TextRuntime but not on InteractiveGue.
    private static readonly Regex TextOnlyVisualAssignment = new Regex(
        @"Visual\.(Font|FontSize|IsBold|IsItalic|Red|Green|Blue)\s*=");

    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);

        return new CodeGenerator(
            new CodeGenerationNameVerifier(mockNameVerifier.Object),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider);
    }

    private static string FindMeadowGumx()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates",
                "FormsThemes", "Meadow", "GumProject.gumx");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            current = Path.GetDirectoryName(current) ?? throw new InvalidOperationException("Meadow template not found");
        }
        throw new InvalidOperationException("Meadow template not found");
    }

    private string GenerateMeadowLabel()
    {
        StandardElementsManager.Self.Initialize();
        ProjectLoadResult result = new ProjectLoader().Load(FindMeadowGumx());
        result.Success.ShouldBeTrue();
        GumProjectSave meadow = result.Project!;
        ObjectFinder.Self.GumProjectSave = meadow;

        ComponentSave label = meadow.Components.Find(item => item.Name == "Meadow/Controls/Label")!;

        return CreateCodeGenerator().GetGeneratedCodeForElement(
            label,
            new CodeOutputElementSettings(),
            new CodeOutputProjectSettings
            {
                OutputLibrary = OutputLibrary.MonoGameForms,
                ObjectInstantiationType = ObjectInstantiationType.FullyInCode,
                RootNamespace = "MyGame",
            });
    }

    [Fact]
    public void GetGeneratedCodeForElement_MeadowLabel_DoesNotAssignTextOnlyMembersThroughVisual()
    {
        string code = GenerateMeadowLabel();

        MatchCollection matches = TextOnlyVisualAssignment.Matches(code);
        string offending = string.Join(", ", matches.Select(m => m.Value));
        offending.ShouldBeEmpty();
    }

    [Fact]
    public void GetGeneratedCodeForElement_MeadowLabel_AssignsTextMembersThroughTextRuntime()
    {
        string code = GenerateMeadowLabel();

        code.ShouldMatch(@"\(\(global::[\w.]+\.TextRuntime\)this\.Visual\)\.Font = ");
        code.ShouldMatch(@"\(\(global::[\w.]+\.TextRuntime\)this\.Visual\)\.Red = ");
    }

    [Fact]
    public void GetGeneratedCodeForElement_MeadowLabel_CreatesATextRuntimeVisual()
    {
        string code = GenerateMeadowLabel();

        // The cast above fails at runtime if the parameterless constructor builds any other visual.
        code.ShouldMatch(@"public Label\(\) : base\(new global::[\w.]+\.TextRuntime\(\)\)");
    }
}
