using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using RenderingLibrary.Graphics;
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
                OutputLibrary = OutputLibrary.GumForms,
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

    private string GenerateScreenWithMeadowLabelInstance(params (string Member, string Type, object Value)[] overrides)
    {
        return GenerateElementWithMeadowLabelInstance(new ScreenSave { Name = "DemoScreen" }, exposedFontName: null, overrides);
    }

    private string GenerateElementWithMeadowLabelInstance(
        ElementSave element,
        string? exposedFontName,
        params (string Member, string Type, object Value)[] overrides)
    {
        StandardElementsManager.Self.Initialize();
        ProjectLoadResult result = new ProjectLoader().Load(FindMeadowGumx());
        result.Success.ShouldBeTrue();
        GumProjectSave meadow = result.Project!;
        ObjectFinder.Self.GumProjectSave = meadow;

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(defaultState);
        InstanceSave instance = new InstanceSave
        {
            Name = "LabelInstance1",
            BaseType = "Meadow/Controls/Label",
            ParentContainer = element,
        };
        element.Instances.Add(instance);
        foreach ((string member, string type, object value) in overrides)
        {
            defaultState.Variables.Add(new VariableSave
            {
                Name = "LabelInstance1." + member,
                Type = type,
                Value = value,
                SetsValue = true,
                ExposedAsName = member == "Font" ? exposedFontName : null,
            });
        }

        return CreateCodeGenerator().GetGeneratedCodeForElement(
            element,
            new CodeOutputElementSettings(),
            new CodeOutputProjectSettings
            {
                OutputLibrary = OutputLibrary.GumForms,
                ObjectInstantiationType = ObjectInstantiationType.FullyInCode,
                RootNamespace = "MyGame",
            });
    }

    [Fact]
    public void GetGeneratedCodeForElement_LabelInstanceFontOverrides_CastVisualToTextRuntime()
    {
        string code = GenerateScreenWithMeadowLabelInstance(
            ("Font", "string", "Fonts/Custom.ttf"),
            ("FontSize", "int", 14),
            ("IsBold", "bool", true),
            ("IsItalic", "bool", false));

        TextOnlyVisualAssignment.Matches(code).Select(m => m.Value).ShouldBeEmpty();
        code.ShouldMatch(@"\(\(global::[\w.]+\.TextRuntime\)this\.LabelInstance1\.Visual\)\.Font = ");
        code.ShouldMatch(@"\(\(global::[\w.]+\.TextRuntime\)this\.LabelInstance1\.Visual\)\.FontSize = ");
        code.ShouldMatch(@"\(\(global::[\w.]+\.TextRuntime\)this\.LabelInstance1\.Visual\)\.IsBold = ");
        code.ShouldMatch(@"\(\(global::[\w.]+\.TextRuntime\)this\.LabelInstance1\.Visual\)\.IsItalic = ");
    }

    [Fact]
    public void GetGeneratedCodeForElement_ExposedLabelInstanceFont_CastsVisualToTextRuntime()
    {
        string code = GenerateElementWithMeadowLabelInstance(
            new ComponentSave { Name = "Card", BaseType = "Container" },
            exposedFontName: "TitleFont",
            ("Font", "string", "Fonts/Custom.ttf"));

        code.ShouldMatch(@"get => \(\(global::[\w.]+\.TextRuntime\) ?LabelInstance1\.Visual\)\.Font;");
        code.ShouldMatch(@"set => \(\(global::[\w.]+\.TextRuntime\) ?LabelInstance1\.Visual\)\.Font = value;");
    }

    [Fact]
    public void GetGeneratedCodeForElement_LabelInstanceAlignment_CastsVisualOnce()
    {
        string code = GenerateScreenWithMeadowLabelInstance(
            ("HorizontalAlignment", "HorizontalAlignment", HorizontalAlignment.Center));

        code.ShouldNotContain("(TextRuntime)((");
        code.ShouldMatch(@"\(\(global::[\w.]+\.TextRuntime\)this\.LabelInstance1\.Visual\)\.HorizontalAlignment = ");
    }

    [Fact]
    public void GetGeneratedCodeForElement_MeadowLabel_CreatesATextRuntimeVisual()
    {
        string code = GenerateMeadowLabel();

        // The cast above fails at runtime if the parameterless constructor builds any other visual.
        code.ShouldMatch(@"public Label\(\) : base\(new global::[\w.]+\.TextRuntime\(\)\)");
    }
}
