using System.Collections.Generic;
using System.Globalization;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

public class ShaderParameterTextTests
{
    private static ShaderParameterCollection Single(string name, ShaderParameterValue value)
    {
        ShaderParameterCollection collection = new();
        collection.Set(name, value);
        return collection;
    }

    [Theory]
    [InlineData(3f, "Thickness=3.0")]
    [InlineData(0.5f, "Thickness=0.5")]
    [InlineData(-1.25f, "Thickness=-1.25")]
    public void Format_Float_AlwaysHasADecimalPoint(float value, string expected)
    {
        string text = ShaderParameterText.Format(Single("Thickness", ShaderParameterValue.FromFloat(value)));

        text.ShouldBe(expected);
    }

    [Fact]
    public void Format_Int_HasNoDecimalPoint()
    {
        ShaderParameterText.Format(Single("Steps", ShaderParameterValue.FromInt(4))).ShouldBe("Steps=4");
    }

    [Fact]
    public void Format_Bool_IsTrueOrFalse()
    {
        ShaderParameterText.Format(Single("Glow", ShaderParameterValue.FromBool(true))).ShouldBe("Glow=true");
        ShaderParameterText.Format(Single("Glow", ShaderParameterValue.FromBool(false))).ShouldBe("Glow=false");
    }

    [Fact]
    public void Format_Vector_IsParenthesizedFloats()
    {
        string text = ShaderParameterText.Format(
            Single("OutlineColor", ShaderParameterValue.FromVector4(1f, 0.5f, 0f, 1f)));

        text.ShouldBe("OutlineColor=(1.0, 0.5, 0.0, 1.0)");
    }

    [Fact]
    public void Format_SeveralParameters_AreSortedByNameAndSeparatedBySemicolons()
    {
        ShaderParameterCollection collection = new();
        collection.Set("Thickness", ShaderParameterValue.FromFloat(2f));
        collection.Set("Glow", ShaderParameterValue.FromBool(true));

        ShaderParameterText.Format(collection).ShouldBe("Glow=true; Thickness=2.0");
    }

    [Fact]
    public void Format_Empty_IsAnEmptyString()
    {
        ShaderParameterText.Format(new ShaderParameterCollection()).ShouldBe("");
    }

    [Fact]
    public void Parse_RoundTripsEveryKind()
    {
        ShaderParameterCollection original = new();
        original.Set("F", ShaderParameterValue.FromFloat(1.5f));
        original.Set("I", ShaderParameterValue.FromInt(-3));
        original.Set("B", ShaderParameterValue.FromBool(true));
        original.Set("V2", ShaderParameterValue.FromVector2(1f, 2f));
        original.Set("V3", ShaderParameterValue.FromVector3(1f, 2f, 3f));
        original.Set("V4", ShaderParameterValue.FromVector4(0.1f, 0.2f, 0.3f, 0.4f));

        ShaderParameterCollection parsed = ShaderParameterText.Parse(ShaderParameterText.Format(original), null);

        parsed.Count.ShouldBe(6);
        parsed.TryGetValue("F", out ShaderParameterValue f).ShouldBeTrue();
        f.Kind.ShouldBe(ShaderParameterKind.Float);
        f.X.ShouldBe(1.5f);
        parsed.TryGetValue("I", out ShaderParameterValue i).ShouldBeTrue();
        i.Kind.ShouldBe(ShaderParameterKind.Int);
        i.X.ShouldBe(-3f);
        parsed.TryGetValue("B", out ShaderParameterValue b).ShouldBeTrue();
        b.Kind.ShouldBe(ShaderParameterKind.Bool);
        b.X.ShouldBe(1f);
        parsed.TryGetValue("V2", out ShaderParameterValue v2).ShouldBeTrue();
        v2.Count.ShouldBe(2);
        parsed.TryGetValue("V3", out ShaderParameterValue v3).ShouldBeTrue();
        v3.Z.ShouldBe(3f);
        parsed.TryGetValue("V4", out ShaderParameterValue v4).ShouldBeTrue();
        v4.Count.ShouldBe(4);
        v4.W.ShouldBe(0.4f);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrBlank_IsEmpty(string? text)
    {
        ShaderParameterText.Parse(text, null).Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_IgnoresWhitespaceAroundNamesAndValues()
    {
        ShaderParameterCollection parsed = ShaderParameterText.Parse("  Thickness = 2.0 ;Glow=  true", null);

        parsed.Count.ShouldBe(2);
        parsed.TryGetValue("Thickness", out ShaderParameterValue thickness).ShouldBeTrue();
        thickness.X.ShouldBe(2f);
    }

    [Fact]
    public void Parse_MalformedEntry_IsSkippedAndReported()
    {
        List<string> errors = new();

        ShaderParameterCollection parsed = ShaderParameterText.Parse("Thickness=abc; Glow=true; Broken", errors);

        parsed.Count.ShouldBe(1);
        parsed.TryGetValue("Glow", out _).ShouldBeTrue();
        errors.Count.ShouldBe(2);
        errors[0].ShouldContain("Thickness");
        errors[1].ShouldContain("Broken");
    }

    [Theory]
    [InlineData("A=(1.0)")]
    [InlineData("A=(1.0, 2.0, 3.0, 4.0, 5.0)")]
    [InlineData("A=(1.0, x)")]
    [InlineData("A=(1.0, 2.0")]
    public void Parse_BadVector_IsReported(string text)
    {
        List<string> errors = new();

        ShaderParameterText.Parse(text, errors).Count.ShouldBe(0);

        errors.Count.ShouldBe(1);
    }

    [Fact]
    public void FormatAndParse_UseTheInvariantCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            string text = ShaderParameterText.Format(Single("Thickness", ShaderParameterValue.FromFloat(1.5f)));
            ShaderParameterCollection parsed = ShaderParameterText.Parse("Thickness=1.5", null);

            text.ShouldBe("Thickness=1.5");
            parsed.TryGetValue("Thickness", out ShaderParameterValue value).ShouldBeTrue();
            value.X.ShouldBe(1.5f);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
