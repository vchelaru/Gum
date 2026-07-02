using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="CodeGenerator.CollectUsingNamespaces"/> — verifies that the set of
/// namespaces emitted as <c>using X;</c> lines includes the instance-derived namespaces and the
/// component's own non-standard BaseType namespace (regression: components inheriting from a
/// user-defined component with no matching instances were missing the BaseType using).
/// </summary>
public class CodeGeneratorCollectUsingsTests
{
    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
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

    private static CodeOutputProjectSettings CreateProjectSettings(string rootNamespace = "MyGame")
    {
        return new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGame,
            RootNamespace = rootNamespace,
            AppendFolderToNamespace = true,
        };
    }

    [Fact]
    public void Component_BaseTypeAndInstance_SameNamespace_DeduplicatedToOne()
    {
        // Both BaseType and an instance resolve to the same namespace -> appears exactly once.
        ComponentSave label = new ComponentSave { Name = "BubbleGumTheme/Controls/Label" };
        ComponentSave floatingLabel = new ComponentSave { Name = "BubbleGumTheme/Controls/FloatingLabel" };
        floatingLabel.BaseType = "BubbleGumTheme/Controls/Label";

        InstanceSave inner = new InstanceSave
        {
            Name = "InnerLabel",
            BaseType = "BubbleGumTheme/Controls/Label",
            ParentContainer = floatingLabel,
        };
        floatingLabel.Instances.Add(inner);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(label);
        project.Components.Add(floatingLabel);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            CodeGenerator generator = CreateCodeGenerator();
            CodeOutputProjectSettings settings = CreateProjectSettings();

            IReadOnlyList<string> usings = generator.CollectUsingNamespaces(
                floatingLabel,
                elementSettings: null,
                settings,
                resolvedSyntaxVersion: 0);

            string expected = "MyGame.Components.BubbleGumTheme.Controls";
            usings.Count(n => n == expected).ShouldBe(1);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void Component_BaseTypeStandardElement_DoesNotAddProjectNamespace()
    {
        // BaseType resolves to a StandardElementSave -> should NOT add a project-component
        // namespace for it (standards are referenced via the GueDeriving using, not the project ns).
        StandardElementSave container = new StandardElementSave { Name = "Container" };

        ComponentSave bareComponent = new ComponentSave { Name = "Widgets/BareComponent" };
        bareComponent.BaseType = "Container";

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(container);
        project.Components.Add(bareComponent);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            CodeGenerator generator = CreateCodeGenerator();
            CodeOutputProjectSettings settings = CreateProjectSettings();

            IReadOnlyList<string> usings = generator.CollectUsingNamespaces(
                bareComponent,
                elementSettings: null,
                settings,
                resolvedSyntaxVersion: 0);

            usings.ShouldNotContain("MyGame.Components.Widgets");
            usings.ShouldNotContain("MyGame.Standards");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void Component_WithNoInstances_BaseTypeNonStandard_IncludesBaseTypeNamespace()
    {
        // RED test for the bug: FloatingLabel-style component inherits from a user component
        // in another folder and has no instances. Without the fix, the BaseType's namespace
        // is never added and the generated code fails to compile.
        ComponentSave label = new ComponentSave { Name = "BubbleGumTheme/Controls/Label" };
        ComponentSave floatingLabel = new ComponentSave { Name = "Widgets/FloatingLabel" };
        floatingLabel.BaseType = "BubbleGumTheme/Controls/Label";

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(label);
        project.Components.Add(floatingLabel);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            CodeGenerator generator = CreateCodeGenerator();
            CodeOutputProjectSettings settings = CreateProjectSettings();

            IReadOnlyList<string> usings = generator.CollectUsingNamespaces(
                floatingLabel,
                elementSettings: null,
                settings,
                resolvedSyntaxVersion: 0);

            usings.ShouldContain("MyGame.Components.BubbleGumTheme.Controls");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void Component_WithNonStandardInstance_IncludesInstanceNamespace()
    {
        // Pinning test: instance whose BaseType is a non-standard component in a different
        // folder contributes its namespace.
        ComponentSave label = new ComponentSave { Name = "BubbleGumTheme/Controls/Label" };

        ComponentSave host = new ComponentSave { Name = "Widgets/Host" };
        InstanceSave instance = new InstanceSave
        {
            Name = "MyLabel",
            BaseType = "BubbleGumTheme/Controls/Label",
            ParentContainer = host,
        };
        host.Instances.Add(instance);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(label);
        project.Components.Add(host);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            CodeGenerator generator = CreateCodeGenerator();
            CodeOutputProjectSettings settings = CreateProjectSettings();

            IReadOnlyList<string> usings = generator.CollectUsingNamespaces(
                host,
                elementSettings: null,
                settings,
                resolvedSyntaxVersion: 0);

            usings.ShouldContain("MyGame.Components.BubbleGumTheme.Controls");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void Component_WithStandardInstanceOnly_DoesNotAddExtraNamespace()
    {
        // Pinning test: instance is a StandardElementSave (ColoredRectangle) -> no project
        // component namespace beyond the runtime/library defaults.
        StandardElementSave coloredRectangle = new StandardElementSave { Name = "ColoredRectangle" };

        ComponentSave host = new ComponentSave { Name = "Widgets/Host" };
        InstanceSave instance = new InstanceSave
        {
            Name = "Bg",
            BaseType = "ColoredRectangle",
            ParentContainer = host,
        };
        host.Instances.Add(instance);

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(coloredRectangle);
        project.Components.Add(host);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            CodeGenerator generator = CreateCodeGenerator();
            CodeOutputProjectSettings settings = CreateProjectSettings();

            IReadOnlyList<string> usings = generator.CollectUsingNamespaces(
                host,
                elementSettings: null,
                settings,
                resolvedSyntaxVersion: 0);

            usings.ShouldNotContain("MyGame.Components.Widgets");
            usings.ShouldNotContain("MyGame.Standards");
            // Sanity check: the default monogame entries should be there.
            usings.ShouldContain("MonoGameGum");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void Raylib_IncludesGumServiceAndGueDerivingUsings()
    {
        // Raylib should get the same GumService/GueDeriving usings as plain MonoGame, at the
        // unified (>= 1) namespace scheme. GumService itself only moves to the "Gum" namespace
        // at syntax version >= 3 (permanent back-compat shims cover versions 1-2 either way).
        ComponentSave host = new ComponentSave { Name = "Widgets/Host" };

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(host);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            CodeGenerator generator = CreateCodeGenerator();
            CodeOutputProjectSettings settings = CreateProjectSettings();
            settings.OutputLibrary = OutputLibrary.Raylib;

            IReadOnlyList<string> usings = generator.CollectUsingNamespaces(
                host,
                elementSettings: null,
                settings,
                resolvedSyntaxVersion: 3);

            usings.ShouldContain("Gum");
            usings.ShouldContain("Gum.GueDeriving");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
