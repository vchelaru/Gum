using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

public class ShaderParameterHistoryTests
{
    [Fact]
    public void GetRestoreValue_WithABaseline_ReturnsTheBaselineNotZero()
    {
        ShaderParameterHistory sut = new();
        sut.SetBaseline("Thickness", ShaderParameterValue.FromFloat(1f));

        ShaderParameterValue restored = sut.GetRestoreValue("Thickness", ShaderParameterValue.FromFloat(3f));

        restored.X.ShouldBe(1f);
    }

    [Fact]
    public void GetRestoreValue_WithoutABaseline_ReturnsZeroOfTheLastWrittenShape()
    {
        ShaderParameterHistory sut = new();

        ShaderParameterValue restored = sut.GetRestoreValue("Offset", ShaderParameterValue.FromVector2(4f, 5f));

        restored.Count.ShouldBe(2);
        restored.X.ShouldBe(0f);
        restored.Y.ShouldBe(0f);
    }

    [Fact]
    public void SetBaseline_CalledAgain_KeepsTheFirstValue()
    {
        ShaderParameterHistory sut = new();
        sut.SetBaseline("Thickness", ShaderParameterValue.FromFloat(1f));

        sut.SetBaseline("Thickness", ShaderParameterValue.FromFloat(3f));

        sut.GetRestoreValue("Thickness", ShaderParameterValue.FromFloat(3f)).X.ShouldBe(1f);
    }

    [Fact]
    public void HasBaseline_ReflectsWhetherOneWasStored()
    {
        ShaderParameterHistory sut = new();

        sut.HasBaseline("Thickness").ShouldBeFalse();
        sut.SetBaseline("Thickness", ShaderParameterValue.FromFloat(1f));
        sut.HasBaseline("Thickness").ShouldBeTrue();
    }

    [Fact]
    public void Baseline_SurvivesTakeStale()
    {
        ShaderParameterHistory sut = new();
        sut.SetBaseline("Thickness", ShaderParameterValue.FromFloat(1f));
        sut.Record("Thickness", ShaderParameterValue.FromFloat(3f));

        sut.TakeStale(new ShaderParameterCollection()).Count.ShouldBe(1);

        sut.HasBaseline("Thickness").ShouldBeTrue();
        sut.GetRestoreValue("Thickness", ShaderParameterValue.FromFloat(3f)).X.ShouldBe(1f);
    }

    [Fact]
    public void ObserveLive_TheFirstTime_StoresTheValueAsTheBaseline()
    {
        ShaderParameterHistory sut = new();

        sut.ObserveLive("Level", ShaderParameterValue.FromFloat(0.5f));

        sut.GetRestoreValue("Level", ShaderParameterValue.FromFloat(1f)).X.ShouldBe(0.5f);
    }

    [Fact]
    public void ObserveLive_WhenTheValueIsTheOneGumLeft_KeepsTheBaseline()
    {
        ShaderParameterHistory sut = new();
        sut.ObserveLive("Level", ShaderParameterValue.FromFloat(0.5f));
        sut.RecordHeld("Level", ShaderParameterValue.FromFloat(1f));

        sut.ObserveLive("Level", ShaderParameterValue.FromFloat(1f));

        sut.GetRestoreValue("Level", ShaderParameterValue.FromFloat(1f)).X.ShouldBe(0.5f);
    }

    [Fact]
    public void ObserveLive_WhenTheValueDiffersFromTheOneGumLeft_ReplacesTheBaseline()
    {
        ShaderParameterHistory sut = new();
        sut.ObserveLive("Level", ShaderParameterValue.FromFloat(0.5f));
        sut.RecordHeld("Level", ShaderParameterValue.FromFloat(1f));

        sut.ObserveLive("Level", ShaderParameterValue.FromFloat(0.9f));

        sut.GetRestoreValue("Level", ShaderParameterValue.FromFloat(1f)).X.ShouldBe(0.9f);
    }

    [Fact]
    public void ObserveLive_WhenGumHasNotRecordedAWrite_KeepsTheBaseline()
    {
        ShaderParameterHistory sut = new();
        sut.SetBaseline("Level", ShaderParameterValue.FromFloat(0.5f));

        sut.ObserveLive("Level", ShaderParameterValue.FromFloat(0.9f));

        sut.GetRestoreValue("Level", ShaderParameterValue.FromFloat(1f)).X.ShouldBe(0.5f);
    }

    [Fact]
    public void ObserveLive_WhenOnlyTheComponentCountDiffers_ReplacesTheBaseline()
    {
        ShaderParameterHistory sut = new();
        sut.ObserveLive("Vec", ShaderParameterValue.FromVector2(0f, 0f));
        sut.RecordHeld("Vec", ShaderParameterValue.FromVector2(1f, 0f));

        sut.ObserveLive("Vec", ShaderParameterValue.FromVector2(1f, 0.5f));

        sut.GetRestoreValue("Vec", ShaderParameterValue.FromVector2(1f, 0f)).Y.ShouldBe(0.5f);
    }
}
