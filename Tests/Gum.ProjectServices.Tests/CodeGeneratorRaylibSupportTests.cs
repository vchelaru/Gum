using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="CodeGenerator.UsesUnifiedGumRuntime"/> and
/// <see cref="CodeGenerator.AssertSupportedCombination"/> — the guard that keeps Raylib codegen
/// scoped to <see cref="ObjectInstantiationType.FindByName"/> for now (see issue #3430).
/// </summary>
public class CodeGeneratorRaylibSupportTests
{
    [Fact]
    public void UsesUnifiedGumRuntime_MonoGame_ReturnsTrue()
    {
        CodeGenerator.UsesUnifiedGumRuntime(OutputLibrary.MonoGame).ShouldBeTrue();
    }

    [Fact]
    public void UsesUnifiedGumRuntime_Raylib_ReturnsTrue()
    {
        CodeGenerator.UsesUnifiedGumRuntime(OutputLibrary.Raylib).ShouldBeTrue();
    }

    [Fact]
    public void UsesUnifiedGumRuntime_Skia_ReturnsFalse()
    {
        CodeGenerator.UsesUnifiedGumRuntime(OutputLibrary.Skia).ShouldBeFalse();
    }

    [Fact]
    public void UsesUnifiedGumRuntime_MonoGameForms_ReturnsFalse()
    {
        // MonoGameForms is handled by its own dedicated checks, not this predicate.
        CodeGenerator.UsesUnifiedGumRuntime(OutputLibrary.MonoGameForms).ShouldBeFalse();
    }

    [Fact]
    public void AssertSupportedCombination_RaylibWithFullyInCode_Throws()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.Raylib,
            ObjectInstantiationType = ObjectInstantiationType.FullyInCode
        };

        Should.Throw<NotSupportedException>(() => CodeGenerator.AssertSupportedCombination(settings));
    }

    [Fact]
    public void AssertSupportedCombination_RaylibWithFindByName_DoesNotThrow()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.Raylib,
            ObjectInstantiationType = ObjectInstantiationType.FindByName
        };

        Should.NotThrow(() => CodeGenerator.AssertSupportedCombination(settings));
    }

    [Fact]
    public void AssertSupportedCombination_MonoGameWithFullyInCode_DoesNotThrow()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGame,
            ObjectInstantiationType = ObjectInstantiationType.FullyInCode
        };

        Should.NotThrow(() => CodeGenerator.AssertSupportedCombination(settings));
    }
}
