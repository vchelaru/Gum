using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Code generation skips variables the project's engine profile hides (#7724): a hidden variable is dormant,
/// so it stays in the file but is not generated.
/// </summary>
public class CodeGeneratorEngineProfileTests : BaseTestClass
{
    private static CodeGenerator CreateCodeGenerator(IVariableRestrictions? restrictions = null)
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

        return new CodeGenerator(
            codeGenNameVerifier,
            new LocalizationService(),
            elementSettingsManager,
            directoryProvider,
            variableRestrictions: restrictions);
    }

    private static IVariableRestrictions Hiding(params (string Pattern, VariableRuleAction Action)[] rules)
    {
        EngineProfile profile = new EngineProfile();
        foreach ((string pattern, VariableRuleAction action) in rules)
        {
            profile.VariableRules.Add(new VariableRule { Pattern = pattern, Action = action });
        }
        return new EngineProfileVariableRestrictions(profile);
    }

    private static CodeOutputProjectSettings CreateSettings() => new CodeOutputProjectSettings
    {
        OutputLibrary = OutputLibrary.MonoGame,
        RootNamespace = "MyGame",
    };

    private static void AddVariable(StateSave state, string name, object value, string type) =>
        state.Variables.Add(new VariableSave { Name = name, Value = value, SetsValue = true, Type = type });

    private void DeclareSpriteVariables()
    {
        StateSave spriteState = Project.StandardElements.First(item => item.Name == "Sprite").DefaultState!;
        AddVariable(spriteState, "X", 0f, "float");
        AddVariable(spriteState, "Y", 0f, "float");
        AddVariable(spriteState, "SourceFile", "", "string");
        AddVariable(spriteState, "Rotation", 0f, "float");
        AddVariable(spriteState, "Parent", "", "string");
        StateSave containerState = Project.StandardElements.First(item => item.Name == "Container").DefaultState!;
        AddVariable(containerState, "X", 0f, "float");
        AddVariable(containerState, "Parent", "", "string");
    }

    /// <summary>A screen with a Container named Holder and a Sprite named SpriteInstance parented to it.</summary>
    private ScreenSave CreateScreenWithSprite()
    {
        DeclareSpriteVariables();
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "SpriteInstance", BaseType = "Sprite", ParentContainer = screen });
        AddVariable(state, "SpriteInstance.X", 10f, "float");
        AddVariable(state, "SpriteInstance.Y", 20f, "float");
        AddVariable(state, "SpriteInstance.SourceFile", "hero.png", "string");
        AddVariable(state, "SpriteInstance.Rotation", 45f, "float");
        AddVariable(state, "SpriteInstance.Parent", "Holder", "string");
        Project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = Project;
        return screen;
    }

    private static string Generate(CodeGenerator generator, ElementSave element) =>
        generator.GetGeneratedCodeForElement(element, elementSettings: null!, CreateSettings());

    private const string DumpPath = @"C:\Users\VICPER~1\AppData\Local\Temp\claude\19d6586ccd8bfce0\a113daa6-f95e-4ec7-94bb-c1876e87b9a6\scratchpad\dump";

    [Fact]
    public void Generate_NoRestrictions_EmitsEveryVariableAndParenting()
    {
        ScreenSave screen = CreateScreenWithSprite();

        string code = Generate(CreateCodeGenerator(), screen);

        code.ShouldContain("SpriteInstance.X = 10f;");
        code.ShouldContain("SpriteInstance.Y = 20f;");
        code.ShouldContain("SpriteInstance.SourceFileName = @\"hero.png\";");
        code.ShouldContain("SpriteInstance.Rotation = 45f;");
        code.ShouldContain("Holder.Children.Add(SpriteInstance);");
    }

    [Fact]
    public void Generate_ProfileWithNoRules_IsIdenticalToNoRestrictions()
    {
        ScreenSave screen = CreateScreenWithSprite();

        string withoutProfile = Generate(CreateCodeGenerator(), screen);
        string withEmptyProfile = Generate(CreateCodeGenerator(Hiding()), screen);

        withEmptyProfile.ShouldBe(withoutProfile);
    }

    [Fact]
    public void Generate_HiddenInstanceVariable_IsSkippedAndOthersRemain()
    {
        ScreenSave screen = CreateScreenWithSprite();

        string code = Generate(CreateCodeGenerator(Hiding(("Sprite.Rotation", VariableRuleAction.Hide))), screen);

        code.ShouldNotContain("Rotation");
        code.ShouldContain("SpriteInstance.X = 10f;");
        code.ShouldContain("SpriteInstance.Y = 20f;");
        code.ShouldContain("SpriteInstance.SourceFileName = @\"hero.png\";");
    }

    [Fact]
    public void Generate_RuleForAnotherStandardType_DoesNotAffectTheInstance()
    {
        ScreenSave screen = CreateScreenWithSprite();

        string code = Generate(CreateCodeGenerator(Hiding(("Text.Rotation", VariableRuleAction.Hide))), screen);

        code.ShouldContain("SpriteInstance.Rotation = 45f;");
    }

    [Fact]
    public void Generate_HideByDefaultWithShownVariables_EmitsOnlyTheShownOnes()
    {
        ScreenSave screen = CreateScreenWithSprite();
        EngineProfile profile = new EngineProfile { HideVariablesByDefault = true };
        foreach (string name in new[] { "Sprite.X", "Sprite.Y", "Sprite.SourceFile", "Sprite.Parent" })
        {
            profile.VariableRules.Add(new VariableRule { Pattern = name, Action = VariableRuleAction.Show });
        }

        string code = Generate(CreateCodeGenerator(new EngineProfileVariableRestrictions(profile)), screen);

        code.ShouldContain("SpriteInstance.X = 10f;");
        code.ShouldContain("SpriteInstance.Y = 20f;");
        code.ShouldContain("SpriteInstance.SourceFileName = @\"hero.png\";");
        code.ShouldNotContain("Rotation");
    }

    [Fact]
    public void Generate_HiddenParent_GeneratesNoParentingToTheNamedParent()
    {
        ScreenSave screen = CreateScreenWithSprite();

        string code = Generate(CreateCodeGenerator(Hiding(("Sprite.Parent", VariableRuleAction.Hide))), screen);

        code.ShouldNotContain("Holder.Children.Add(SpriteInstance)");
        code.ShouldContain("SpriteInstance.X = 10f;");
    }

    [Fact]
    public void Generate_HiddenVariableOnElementsOwnState_IsSkipped()
    {
        DeclareSpriteVariables();
        ComponentSave hero = new ComponentSave { Name = "Hero", BaseType = "Sprite" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = hero };
        hero.States.Add(state);
        AddVariable(state, "X", 5f, "float");
        AddVariable(state, "Rotation", 30f, "float");
        Project.Components.Add(hero);
        ObjectFinder.Self.GumProjectSave = Project;

        string shown = Generate(CreateCodeGenerator(), hero);
        string hidden = Generate(CreateCodeGenerator(Hiding(("Sprite.Rotation", VariableRuleAction.Hide))), hero);

        shown.ShouldContain("this.Rotation = 30f;");
        hidden.ShouldNotContain("Rotation");
        hidden.ShouldContain("this.X = 5f;");
    }

    [Fact]
    public void Generate_HiddenVariableInACategoryState_IsSkipped()
    {
        ScreenSave screen = CreateScreenWithSprite();
        StateSaveCategory category = new StateSaveCategory { Name = "Mode" };
        StateSave highlighted = new StateSave { Name = "Highlighted", ParentContainer = screen };
        AddVariable(highlighted, "SpriteInstance.Rotation", 90f, "float");
        AddVariable(highlighted, "SpriteInstance.X", 99f, "float");
        category.States.Add(highlighted);
        screen.Categories.Add(category);

        string shown = Generate(CreateCodeGenerator(), screen);
        string hidden = Generate(CreateCodeGenerator(Hiding(("Sprite.Rotation", VariableRuleAction.Hide))), screen);

        shown.ShouldContain("SpriteInstance.Rotation = 90f;");
        hidden.ShouldNotContain("Rotation");
        hidden.ShouldContain("SpriteInstance.X = 99f;");
    }

    [Fact]
    public void Generate_HideEverything_StillGeneratesCustomAndExposedVariables()
    {
        ScreenSave screen = CreateScreenWithSprite();
        StateSave state = screen.DefaultState!;
        state.Variables.Add(new VariableSave
        {
            Name = "Health",
            Type = "float",
            Value = 3f,
            SetsValue = true,
            IsCustomVariable = true,
        });
        state.Variables.First(item => item.Name == "SpriteInstance.Rotation").ExposedAsName = "SpriteRotation";

        string code = Generate(CreateCodeGenerator(Hiding(("*", VariableRuleAction.Hide))), screen);

        code.ShouldContain("Health");
        code.ShouldContain("SpriteRotation");
        code.ShouldNotContain("SpriteInstance.X = 10f;");
    }

    private static IVariableRestrictions Supporting(params string[] standardTypes) =>
        new EngineProfileVariableRestrictions(new EngineProfile { SupportedStandardTypes = standardTypes.ToList() });

    [Fact]
    public void Generate_BlockedInstanceType_SkipsTheInstanceEverywhere()
    {
        ScreenSave screen = CreateScreenWithSprite();

        string code = Generate(CreateCodeGenerator(Supporting("Container")), screen);

        code.ShouldNotContain("SpriteInstance");
        code.ShouldContain("Holder");
    }

    [Fact]
    public void Generate_EmptySupportedList_IsIdenticalToNoRestrictions()
    {
        ScreenSave screen = CreateScreenWithSprite();

        string withoutProfile = Generate(CreateCodeGenerator(), screen);
        string withEmptyList = Generate(CreateCodeGenerator(Supporting()), screen);

        withEmptyList.ShouldBe(withoutProfile);
    }

    [Fact]
    public void Generate_InstanceOfComponentDerivedFromBlockedType_IsSkipped()
    {
        DeclareSpriteVariables();
        ComponentSave hero = new ComponentSave { Name = "Hero", BaseType = "Sprite" };
        hero.States.Add(new StateSave { Name = "Default", ParentContainer = hero });
        ComponentSave superHero = new ComponentSave { Name = "SuperHero", BaseType = "Hero" };
        superHero.States.Add(new StateSave { Name = "Default", ParentContainer = superHero });
        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        panel.States.Add(new StateSave { Name = "Default", ParentContainer = panel });
        Project.Components.Add(hero);
        Project.Components.Add(superHero);
        Project.Components.Add(panel);
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "HeroInstance", BaseType = "Hero", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "SuperHeroInstance", BaseType = "SuperHero", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "PanelInstance", BaseType = "Panel", ParentContainer = screen });
        Project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = Project;
        CodeGenerator generator = CreateCodeGenerator(Supporting("Container"));

        string code = Generate(generator, screen);

        code.ShouldNotContain("HeroInstance");
        code.ShouldNotContain("SuperHeroInstance");
        code.ShouldContain("PanelInstance");
        generator.IsElementBlockedByEngineProfile(hero).ShouldBeTrue();
        generator.IsElementBlockedByEngineProfile(superHero).ShouldBeTrue();
        generator.IsElementBlockedByEngineProfile(panel).ShouldBeFalse();
        generator.IsElementBlockedByEngineProfile(screen).ShouldBeFalse();
    }

    [Fact]
    public void Generate_ChildOfSkippedInstance_IsAttachedWithoutTheSkippedParent()
    {
        ScreenSave screen = CreateScreenWithSprite();
        StateSave state = screen.DefaultState!;
        StateSave containerState = Project.StandardElements.First(item => item.Name == "Text").DefaultState!;
        AddVariable(containerState, "Parent", "", "string");
        AddVariable(containerState, "X", 0f, "float");
        screen.Instances.Add(new InstanceSave { Name = "TextChild", BaseType = "Text", ParentContainer = screen });
        AddVariable(state, "TextChild.Parent", "SpriteInstance", "string");
        AddVariable(state, "TextChild.X", 7f, "float");

        string code = Generate(CreateCodeGenerator(Supporting("Container", "Text")), screen);

        code.ShouldNotContain("SpriteInstance");
        code.ShouldContain("this.Children.Add(TextChild);");
        code.ShouldContain("TextChild.X = 7f;");
    }

    [Fact]
    public void Generate_ExposedAndStateVariablesOnSkippedInstance_AreSkipped()
    {
        ScreenSave screen = CreateScreenWithSprite();
        screen.DefaultState!.Variables.First(item => item.Name == "SpriteInstance.Rotation").ExposedAsName = "SpriteRotation";
        StateSaveCategory category = new StateSaveCategory { Name = "Mode" };
        StateSave highlighted = new StateSave { Name = "Highlighted", ParentContainer = screen };
        AddVariable(highlighted, "SpriteInstance.X", 99f, "float");
        category.States.Add(highlighted);
        screen.Categories.Add(category);

        string code = Generate(CreateCodeGenerator(Supporting("Container")), screen);

        code.ShouldNotContain("SpriteRotation");
        code.ShouldNotContain("SpriteInstance");
        code.ShouldNotContain("99f");
    }}
