using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System.Linq;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for issue #3119: GumService moved from the MonoGameGum namespace to Gum at runtime
/// syntax version 3. Codegen must emit <c>using MonoGameGum;</c> for versions 0-2 (legacy,
/// resolved via the permanent back-compat shims) and <c>using Gum;</c> for version 3+.
/// At version 3+ the MonoGameGum AddChild extension crutch is no longer imported, so the
/// Parent-assignment path must reach a GraphicalUiElement owner's instance AddChild by
/// passing the Forms child's <c>.Visual</c>.
/// </summary>
public class CodeGeneratorGumServiceNamespaceTests : BaseTestClass
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

    #region GetGumServiceNamespace

    [Fact]
    public void GetGumServiceNamespace_Version0_ReturnsMonoGameGum()
    {
        string result = CodeGenerator.GetGumServiceNamespace(syntaxVersion: 0);

        result.ShouldBe("MonoGameGum");
    }

    [Fact]
    public void GetGumServiceNamespace_Version2_ReturnsMonoGameGum()
    {
        string result = CodeGenerator.GetGumServiceNamespace(syntaxVersion: 2);

        result.ShouldBe("MonoGameGum");
    }

    [Fact]
    public void GetGumServiceNamespace_Version3_ReturnsGum()
    {
        string result = CodeGenerator.GetGumServiceNamespace(syntaxVersion: 3);

        result.ShouldBe("Gum");
    }

    [Fact]
    public void GetGumServiceNamespace_Version4_ReturnsGum()
    {
        // Any version >= 3 should use the unified namespace (forward compatible).
        string result = CodeGenerator.GetGumServiceNamespace(syntaxVersion: 4);

        result.ShouldBe("Gum");
    }

    [Fact]
    public void GetGumServiceNamespace_RaylibVersion1_ReturnsRaylibGum()
    {
        // Raylib's back-compat shim lives in the RaylibGum namespace (GumServiceCompat.cs's
        // #elif RAYLIB branch), not MonoGameGum — using the MonoGame shim namespace would not
        // compile against RaylibGum.
        string result = CodeGenerator.GetGumServiceNamespace(syntaxVersion: 1, isRaylib: true);

        result.ShouldBe("RaylibGum");
    }

    [Fact]
    public void GetGumServiceNamespace_RaylibVersion3_ReturnsGum()
    {
        string result = CodeGenerator.GetGumServiceNamespace(syntaxVersion: 3, isRaylib: true);

        result.ShouldBe("Gum");
    }

    #endregion

    #region CollectUsingNamespaces

    [Fact]
    public void CollectUsingNamespaces_MonoGameFormsVersion2_EmitsMonoGameGumUsing()
    {
        ComponentSave component = new ComponentSave { Name = "Widgets/Host" };
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeGenerator generator = CreateCodeGenerator();
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGameForms,
            RootNamespace = "MyGame",
        };

        IReadOnlyList<string> usings = generator.CollectUsingNamespaces(
            component,
            elementSettings: null,
            settings,
            resolvedSyntaxVersion: 2);

        usings.ShouldContain("MonoGameGum");
        usings.ShouldNotContain("Gum");
    }

    [Fact]
    public void CollectUsingNamespaces_MonoGameFormsVersion3_EmitsGumUsing()
    {
        ComponentSave component = new ComponentSave { Name = "Widgets/Host" };
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeGenerator generator = CreateCodeGenerator();
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGameForms,
            RootNamespace = "MyGame",
        };

        IReadOnlyList<string> usings = generator.CollectUsingNamespaces(
            component,
            elementSettings: null,
            settings,
            resolvedSyntaxVersion: 3);

        usings.ShouldContain("Gum");
        usings.ShouldNotContain("MonoGameGum");
    }

    [Fact]
    public void CollectUsingNamespaces_MonoGameVersion3_EmitsGumUsing()
    {
        ComponentSave component = new ComponentSave { Name = "Widgets/Host" };
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeGenerator generator = CreateCodeGenerator();
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGame,
            RootNamespace = "MyGame",
        };

        IReadOnlyList<string> usings = generator.CollectUsingNamespaces(
            component,
            elementSettings: null,
            settings,
            resolvedSyntaxVersion: 3);

        usings.ShouldContain("Gum");
        usings.ShouldNotContain("MonoGameGum");
    }

    [Fact]
    public void CollectUsingNamespaces_RaylibVersion1_EmitsRaylibGumUsing()
    {
        ComponentSave component = new ComponentSave { Name = "Widgets/Host" };
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeGenerator generator = CreateCodeGenerator();
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.Raylib,
            RootNamespace = "MyGame",
        };

        IReadOnlyList<string> usings = generator.CollectUsingNamespaces(
            component,
            elementSettings: null,
            settings,
            resolvedSyntaxVersion: 1);

        usings.ShouldContain("RaylibGum");
        usings.ShouldNotContain("MonoGameGum");
    }

    #endregion

    #region Parent assignment AddChild

    private string GetParentAssignmentCode(int syntaxVersion)
    {
        // A GraphicalUiElement-typed owner (standard Container instance) parenting a
        // Forms-typed child (component instance) in MonoGameForms output.
        ComponentSave button = new ComponentSave { Name = "Controls/MyButton" };
        StateSave buttonDefault = new StateSave { Name = "Default", ParentContainer = button };
        button.States.Add(buttonDefault);
        Project.Components.Add(button);

        ComponentSave main = new ComponentSave { Name = "MainComponent", BaseType = "Container" };
        StateSave mainDefault = new StateSave { Name = "Default", ParentContainer = main };
        main.States.Add(mainDefault);

        InstanceSave containerInstance = new InstanceSave
        {
            Name = "ContainerInstance",
            BaseType = "Container",
            ParentContainer = main,
        };
        main.Instances.Add(containerInstance);

        InstanceSave buttonInstance = new InstanceSave
        {
            Name = "ButtonInstance",
            BaseType = "Controls/MyButton",
            ParentContainer = main,
        };
        main.Instances.Add(buttonInstance);

        main.DefaultState.Variables.Add(new VariableSave
        {
            Name = "ButtonInstance.Parent",
            Value = "ContainerInstance",
            SetsValue = true,
            Type = "string",
        });

        Project.Components.Add(main);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGameForms,
            RootNamespace = "MyGame",
            SyntaxVersion = syntaxVersion.ToString(),
        };

        return CreateCodeGenerator().GetCodeForInstance(buttonInstance, main, settings);
    }

    [Fact]
    public void GetCodeForInstance_GueOwnerFormsChild_Version2_EmitsAddChildWithoutVisual()
    {
        string code = GetParentAssignmentCode(syntaxVersion: 2);

        code.ShouldContain("ContainerInstance.AddChild(ButtonInstance);");
    }

    [Fact]
    public void GetCodeForInstance_GueOwnerFormsChild_Version3_EmitsAddChildWithVisual()
    {
        string code = GetParentAssignmentCode(syntaxVersion: 3);

        code.ShouldContain("ContainerInstance.AddChild(ButtonInstance.Visual);");
    }

    #endregion
}
