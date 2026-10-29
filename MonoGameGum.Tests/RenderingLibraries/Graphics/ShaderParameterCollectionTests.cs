using MonoGameGum.TestsCommon;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

public class ShaderParameterCollectionTests
{
    [Fact]
    public void Set_NewName_CanBeReadBack()
    {
        ShaderParameterCollection sut = new();

        sut.Set("Thickness", ShaderParameterValue.FromFloat(1.5f));

        sut.TryGetValue("Thickness", out ShaderParameterValue value).ShouldBeTrue();
        value.Kind.ShouldBe(ShaderParameterKind.Float);
        value.Count.ShouldBe(1);
        value.X.ShouldBe(1.5f);
    }

    [Fact]
    public void Set_ExistingName_ReplacesKindCountAndValue()
    {
        ShaderParameterCollection sut = new();
        sut.Set("Value", ShaderParameterValue.FromFloat(1f));

        sut.Set("Value", ShaderParameterValue.FromInt(7));

        sut.Count.ShouldBe(1);
        sut.TryGetValue("Value", out ShaderParameterValue value).ShouldBeTrue();
        value.Kind.ShouldBe(ShaderParameterKind.Int);
        value.X.ShouldBe(7f);
    }

    [Fact]
    public void FromVector_StoresComponentCountAndComponents()
    {
        ShaderParameterValue value = ShaderParameterValue.FromVector4(0.1f, 0.2f, 0.3f, 0.4f);

        value.Kind.ShouldBe(ShaderParameterKind.Float);
        value.Count.ShouldBe(4);
        value.X.ShouldBe(0.1f);
        value.Y.ShouldBe(0.2f);
        value.Z.ShouldBe(0.3f);
        value.W.ShouldBe(0.4f);
    }

    [Fact]
    public void FromBool_StoresOneForTrueAndZeroForFalse()
    {
        ShaderParameterValue on = ShaderParameterValue.FromBool(true);
        ShaderParameterValue off = ShaderParameterValue.FromBool(false);

        on.Kind.ShouldBe(ShaderParameterKind.Bool);
        on.X.ShouldBe(1f);
        off.X.ShouldBe(0f);
    }

    [Fact]
    public void TryGetValue_UnknownName_ReturnsFalse()
    {
        ShaderParameterCollection sut = new();

        sut.TryGetValue("Missing", out _).ShouldBeFalse();
    }

    [Fact]
    public void Remove_And_Clear_DropEntries()
    {
        ShaderParameterCollection sut = new();
        sut.Set("A", ShaderParameterValue.FromFloat(1f));
        sut.Set("B", ShaderParameterValue.FromFloat(2f));

        sut.Remove("A").ShouldBeTrue();
        sut.Remove("A").ShouldBeFalse();
        sut.Count.ShouldBe(1);

        sut.Clear();
        sut.Count.ShouldBe(0);
    }

    [Fact]
    public void Set_ExistingName_DoesNotAllocate()
    {
        ShaderParameterCollection sut = new();
        sut.Set("Thickness", ShaderParameterValue.FromFloat(0f));
        float next = 0f;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(() =>
        {
            next += 1f;
            sut.Set("Thickness", ShaderParameterValue.FromFloat(next));
        });

        result.BytesPerIteration.ShouldBe(0);
    }

    [Fact]
    public void Enumerating_DoesNotAllocate()
    {
        ShaderParameterCollection sut = new();
        sut.Set("A", ShaderParameterValue.FromFloat(1f));
        sut.Set("B", ShaderParameterValue.FromVector3(1f, 2f, 3f));
        float sum = 0f;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(() =>
        {
            foreach (System.Collections.Generic.KeyValuePair<string, ShaderParameterValue> pair in sut)
            {
                sum += pair.Value.X;
            }
        });

        result.BytesPerIteration.ShouldBe(0);
        sum.ShouldBeGreaterThan(0f);
    }
}
