using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests that the code generator emits the unified <c>Gum.GueDeriving</c> namespace when the
/// resolved syntax version is &gt;= 1, and falls back to the legacy <c>MonoGameGum.GueDeriving</c>
/// / <c>SkiaGum.GueDeriving</c> namespaces for version 0.
/// </summary>
public class CodeGeneratorSyntaxVersionNamespaceTests
{
    [Fact]
    public void GetGueDerivingNamespace_Version0_MonoGame_ReturnsMonoGameGumNamespace()
    {
        string result = CodeGenerator.GetGueDerivingNamespace(syntaxVersion: 0, isSkia: false);

        result.ShouldBe("MonoGameGum.GueDeriving");
    }

    [Fact]
    public void GetGueDerivingNamespace_Version0_Skia_ReturnsSkiaGumNamespace()
    {
        string result = CodeGenerator.GetGueDerivingNamespace(syntaxVersion: 0, isSkia: true);

        result.ShouldBe("SkiaGum.GueDeriving");
    }

    [Fact]
    public void GetGueDerivingNamespace_Version1_MonoGame_ReturnsUnifiedNamespace()
    {
        string result = CodeGenerator.GetGueDerivingNamespace(syntaxVersion: 1, isSkia: false);

        result.ShouldBe("Gum.GueDeriving");
    }

    [Fact]
    public void GetGueDerivingNamespace_Version1_Skia_ReturnsUnifiedNamespace()
    {
        string result = CodeGenerator.GetGueDerivingNamespace(syntaxVersion: 1, isSkia: true);

        result.ShouldBe("Gum.GueDeriving");
    }

    [Fact]
    public void GetGueDerivingNamespace_Version2_MonoGame_ReturnsUnifiedNamespace()
    {
        // Any version >= 1 should use the unified namespace (forward compatible)
        string result = CodeGenerator.GetGueDerivingNamespace(syntaxVersion: 2, isSkia: false);

        result.ShouldBe("Gum.GueDeriving");
    }

    [Fact]
    public void GetInheritance_Version0_MonoGame_StandardElement_UsesLegacyNamespace()
    {
        // Set up a Container standard element that GetInheritance will resolve against.
        // Use "Sprite" — for "Container" + MonoGame the codegen takes a special branch that
        // emits an unqualified ContainerRuntime (relying on the using directive). Sprite goes
        // through the namespace-prefixed branch we want to verify.
        StandardElementSave sprite = new StandardElementSave { Name = "Sprite" };
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(sprite);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            ComponentSave component = new ComponentSave();
            component.BaseType = "Sprite";

            CodeOutputProjectSettings settings = new CodeOutputProjectSettings
            {
                OutputLibrary = OutputLibrary.MonoGame
            };

            string? result = CodeGenerator.GetInheritance(component, settings, resolvedSyntaxVersion: 0);

            result.ShouldBe("global::MonoGameGum.GueDeriving.SpriteRuntime");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetInheritance_Version1_MonoGame_StandardElement_UsesUnifiedNamespace()
    {
        // Use "Sprite" — for "Container" + MonoGame the codegen takes a special branch that
        // emits an unqualified ContainerRuntime (relying on the using directive). Sprite goes
        // through the namespace-prefixed branch we want to verify.
        StandardElementSave sprite = new StandardElementSave { Name = "Sprite" };
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(sprite);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            ComponentSave component = new ComponentSave();
            component.BaseType = "Sprite";

            CodeOutputProjectSettings settings = new CodeOutputProjectSettings
            {
                OutputLibrary = OutputLibrary.MonoGame
            };

            string? result = CodeGenerator.GetInheritance(component, settings, resolvedSyntaxVersion: 1);

            result.ShouldBe("global::Gum.GueDeriving.SpriteRuntime");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetInheritance_Version1_Skia_StandardElement_UsesUnifiedNamespace()
    {
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            ComponentSave component = new ComponentSave();
            component.BaseType = "Container";

            CodeOutputProjectSettings settings = new CodeOutputProjectSettings
            {
                OutputLibrary = OutputLibrary.Skia
            };

            string? result = CodeGenerator.GetInheritance(component, settings, resolvedSyntaxVersion: 1);

            // Skia always emits ContainerRuntime; check the namespace flips.
            result.ShouldBe("Gum.GueDeriving.ContainerRuntime");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetInheritance_Version0_Skia_StandardElement_UsesLegacyNamespace()
    {
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            ComponentSave component = new ComponentSave();
            component.BaseType = "Container";

            CodeOutputProjectSettings settings = new CodeOutputProjectSettings
            {
                OutputLibrary = OutputLibrary.Skia
            };

            string? result = CodeGenerator.GetInheritance(component, settings, resolvedSyntaxVersion: 0);

            result.ShouldBe("SkiaGum.GueDeriving.ContainerRuntime");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
