using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="CodeGenerator.UsesUnifiedGumRuntime"/> and
/// <see cref="CodeGenerator.AssertSupportedCombination"/> — the guard that keeps Silk.NET
/// (<c>Gum.SilkNet</c>) codegen scoped to <see cref="ObjectInstantiationType.FindByName"/> for now,
/// mirroring Raylib's #3430 rollout (see #3573).
/// </summary>
public class CodeGeneratorSilkSupportTests
{
    [Fact]
    public void UsesUnifiedGumRuntime_Silk_ReturnsTrue()
    {
        CodeGenerator.UsesUnifiedGumRuntime(OutputLibrary.Silk).ShouldBeTrue();
    }

    [Fact]
    public void AssertSupportedCombination_SilkWithFullyInCode_Throws()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.Silk,
            ObjectInstantiationType = ObjectInstantiationType.FullyInCode
        };

        Should.Throw<NotSupportedException>(() => CodeGenerator.AssertSupportedCombination(settings));
    }

    [Fact]
    public void AssertSupportedCombination_SilkWithFindByName_DoesNotThrow()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.Silk,
            ObjectInstantiationType = ObjectInstantiationType.FindByName
        };

        Should.NotThrow(() => CodeGenerator.AssertSupportedCombination(settings));
    }

    [Fact]
    public void CoerceToSupportedCombination_SilkWithFullyInCode_ChangesToFindByName()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.Silk,
            ObjectInstantiationType = ObjectInstantiationType.FullyInCode
        };

        bool changed = CodeGenerator.CoerceToSupportedCombination(settings);

        changed.ShouldBeTrue();
        settings.ObjectInstantiationType.ShouldBe(ObjectInstantiationType.FindByName);
    }

    [Fact]
    public void CoerceToSupportedCombination_SilkWithFindByName_LeavesUnchanged()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.Silk,
            ObjectInstantiationType = ObjectInstantiationType.FindByName
        };

        bool changed = CodeGenerator.CoerceToSupportedCombination(settings);

        changed.ShouldBeFalse();
        settings.ObjectInstantiationType.ShouldBe(ObjectInstantiationType.FindByName);
    }
}
