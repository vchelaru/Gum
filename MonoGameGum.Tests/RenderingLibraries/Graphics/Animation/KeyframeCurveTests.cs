using System;
using FlatRedBall.Glue.StateInterpolation;
using Gum.Graphics.Animation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics.Animation;

// Ported from FlatRedBall2's KeyframeCurveTests (#1478): the file stores curve names as strings, and
// an omitted or unknown name means snap (type) or Out (easing).
public class KeyframeCurveTests
{
    [Fact]
    public void Default_IsSnapWithOutEasing()
    {
        KeyframeCurve curve = default;

        curve.IsSnap.ShouldBeTrue();
        curve.Type.ShouldBe(InterpolationType.Instant);
        curve.Easing.ShouldBe(Easing.Out);
    }

    [Fact]
    public void Parse_NullType_IsSnap()
    {
        KeyframeCurve.Parse(null, "In").IsSnap.ShouldBeTrue();
    }

    [Fact]
    public void Parse_OmittedEasing_IsOut()
    {
        KeyframeCurve.Parse("Quadratic", null).Easing.ShouldBe(Easing.Out);
    }

    [Fact]
    public void Parse_NamesAreCaseInsensitive()
    {
        KeyframeCurve curve = KeyframeCurve.Parse("quadratic", "inout");

        curve.Type.ShouldBe(InterpolationType.Quadratic);
        curve.Easing.ShouldBe(Easing.InOut);
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("7")]
    public void Parse_UnknownType_IsSnap(string type)
    {
        KeyframeCurve.Parse(type, "In").IsSnap.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("9")]
    public void Parse_UnknownEasing_IsOut(string easing)
    {
        KeyframeCurve.Parse("Cubic", easing).Easing.ShouldBe(Easing.Out);
    }

    [Fact]
    public void Parse_AllLibraryCurves_AreRepresentable()
    {
        int count = 0;
        foreach (InterpolationType type in Enum.GetValues<InterpolationType>())
        {
            if (type == InterpolationType.Instant)
            {
                continue;
            }
            foreach (Easing easing in Enum.GetValues<Easing>())
            {
                KeyframeCurve curve = KeyframeCurve.Parse(type.ToString(), easing.ToString());
                curve.IsSnap.ShouldBeFalse();
                curve.Type.ShouldBe(type);
                curve.Easing.ShouldBe(easing);
                count++;
            }
        }
        count.ShouldBe(33);
    }

    [Fact]
    public void Evaluate_Snap_HoldsZeroUntilSegmentEnd()
    {
        KeyframeCurve.Snap.Evaluate(0f).ShouldBe(0f);
        KeyframeCurve.Snap.Evaluate(0.999f).ShouldBe(0f);
        KeyframeCurve.Snap.Evaluate(1f).ShouldBe(1f);
    }

    [Theory]
    [InlineData("Back")]
    [InlineData("Bounce")]
    [InlineData("Elastic")]
    [InlineData("Cubic")]
    public void Evaluate_EndpointsAreExact(string type)
    {
        foreach (string easing in new[] { "In", "Out", "InOut" })
        {
            KeyframeCurve curve = KeyframeCurve.Parse(type, easing);
            curve.Evaluate(0f).ShouldBe(0f);
            curve.Evaluate(1f).ShouldBe(1f);
        }
    }

    [Fact]
    public void Evaluate_QuadraticMatchesLibrary()
    {
        KeyframeCurve.Parse("Quadratic", "In").Evaluate(0.5f).ShouldBe(0.25f, 0.0001f);
        KeyframeCurve.Parse("Quadratic", "Out").Evaluate(0.5f).ShouldBe(0.75f, 0.0001f);
    }
}
