using Gum.Plugins.InternalPlugins.VariableGrid;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.VariableGrid;

public class ShaderParameterRowsTests
{
    private string _text = "";

    private List<SyntheticVariableRow> Build(params ShaderParameterInfo[] infos) =>
        ShaderParameterRows.Build(infos, () => _text, text => _text = text);

    private static ShaderParameterInfo FloatInfo(string name, float defaultValue = 0f) =>
        new ShaderParameterInfo(name, ShaderParameterKind.Float, 1, ShaderParameterValue.FromFloat(defaultValue));

    private static ShaderParameterInfo VectorInfo(string name, int count)
    {
        ShaderParameterValue value = count == 2 ? ShaderParameterValue.FromVector2(0.5f, 0.25f)
            : count == 3 ? ShaderParameterValue.FromVector3(0.5f, 0.25f, 0.125f)
            : ShaderParameterValue.FromVector4(0.5f, 0.25f, 0.125f, 1f);
        return new ShaderParameterInfo(name, ShaderParameterKind.Float, count, value);
    }

    [Fact]
    public void Build_TexelSize_IsHiddenBecauseGumFillsItIn()
    {
        List<SyntheticVariableRow> rows = Build(FloatInfo("Thickness"), VectorInfo("TexelSize", 2), FloatInfo("PixelSize"));

        rows.Select(row => row.Name).ShouldBe(new[] { "Thickness", "PixelSize" });
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void Set_StoresAnExplicitValue_EvenWhenItEqualsTheDisplayedDefault(float value)
    {
        List<SyntheticVariableRow> rows = Build(FloatInfo("Thickness", defaultValue: 0f));

        rows[0].Set(value);

        _text.ShouldContain("Thickness=");
        rows[0].Get().ShouldBe(value);
    }

    [Fact]
    public void Build_ScalarFloat_IsOneRowNamedAfterTheParameter()
    {
        List<SyntheticVariableRow> rows = Build(FloatInfo("Thickness"));

        rows.Count.ShouldBe(1);
        rows[0].Name.ShouldBe("Thickness");
        rows[0].ValueType.ShouldBe(typeof(float));
    }

    [Fact]
    public void Get_ShowsTheShaderDefault_UntilAValueIsStored()
    {
        List<SyntheticVariableRow> rows = Build(FloatInfo("Thickness", defaultValue: 2.5f));

        rows[0].Get().ShouldBe(2.5f);

        _text = "Thickness=4.0";
        rows[0].Get().ShouldBe(4f);
    }

    [Fact]
    public void Set_WritesTheValueIntoTheSavedText()
    {
        List<SyntheticVariableRow> rows = Build(FloatInfo("Thickness"));

        rows[0].Set!(3.0);

        _text.ShouldBe("Thickness=3.0");
    }

    [Fact]
    public void Set_KeepsEntriesTheShaderDoesNotDeclare()
    {
        _text = "Old=1.0; Thickness=1.0";
        List<SyntheticVariableRow> rows = Build(FloatInfo("Thickness"));

        rows[0].Set!(2f);

        _text.ShouldBe("Old=1.0; Thickness=2.0");
    }

    [Fact]
    public void Build_Int_UsesAnIntRowAndWritesAnIntWithNoDecimalPoint()
    {
        List<SyntheticVariableRow> rows = Build(
            new ShaderParameterInfo("Steps", ShaderParameterKind.Int, 1, ShaderParameterValue.FromInt(4)));

        rows[0].ValueType.ShouldBe(typeof(int));
        rows[0].Get().ShouldBe(4);

        rows[0].Set!(6);
        _text.ShouldBe("Steps=6");
    }

    [Fact]
    public void Build_Bool_UsesABoolRow()
    {
        List<SyntheticVariableRow> rows = Build(
            new ShaderParameterInfo("Glow", ShaderParameterKind.Bool, 1, ShaderParameterValue.FromBool(false)));

        rows[0].ValueType.ShouldBe(typeof(bool));
        rows[0].Get().ShouldBe(false);

        rows[0].Set!(true);
        _text.ShouldBe("Glow=true");
    }

    [Theory]
    [InlineData(2, new[] { "Tint X", "Tint Y" })]
    [InlineData(3, new[] { "Tint X", "Tint Y", "Tint Z" })]
    [InlineData(4, new[] { "Tint X", "Tint Y", "Tint Z", "Tint W" })]
    public void Build_Vector_HasOneFloatRowPerComponent(int count, string[] expectedNames)
    {
        List<SyntheticVariableRow> rows = Build(VectorInfo("Tint", count));

        rows.Select(row => row.Name).ToArray().ShouldBe(expectedNames);
        rows.All(row => row.ValueType == typeof(float)).ShouldBeTrue();
    }

    [Fact]
    public void Vector_Get_ShowsEachComponentOfTheDefaultThenTheStoredValue()
    {
        List<SyntheticVariableRow> rows = Build(VectorInfo("Color", 4));

        rows[1].Get().ShouldBe(0.25f);

        _text = "Color=(1.0, 0.0, 0.5, 1.0)";
        rows[2].Get().ShouldBe(0.5f);
    }

    [Fact]
    public void Vector_Set_ChangesOnlyThatComponent()
    {
        _text = "Color=(1.0, 0.0, 0.5, 1.0)";
        List<SyntheticVariableRow> rows = Build(VectorInfo("Color", 4));

        rows[1].Set!(0.75f);

        _text.ShouldBe("Color=(1.0, 0.75, 0.5, 1.0)");
    }

    [Fact]
    public void Vector_Set_StartsFromTheShaderDefaultWhenNothingIsStored()
    {
        List<SyntheticVariableRow> rows = Build(VectorInfo("Size", 2));

        rows[0].Set!(2f);

        _text.ShouldBe("Size=(2.0, 0.25)");
    }

    [Fact]
    public void Set_IgnoresAValueItCannotConvert()
    {
        List<SyntheticVariableRow> rows = Build(FloatInfo("Thickness"));

        rows[0].Set!("not a number");

        _text.ShouldBe("");
    }

    private List<SyntheticVariableRow> BuildWithSourceDefaults(
        Dictionary<string, ShaderParameterValue> sourceDefaults, params ShaderParameterInfo[] infos) =>
        ShaderParameterRows.Build(infos, () => _text, text => _text = text, sourceDefaults);

    [Fact]
    public void Get_ShowsTheDefaultDeclaredInTheSource_NotTheCompiledOne()
    {
        // ShadowDusk drops "float Thickness = 1.0;", so the compiled effect reports 0.
        List<SyntheticVariableRow> rows = BuildWithSourceDefaults(
            new Dictionary<string, ShaderParameterValue> { ["Thickness"] = ShaderParameterValue.FromFloat(1f) },
            FloatInfo("Thickness", defaultValue: 0f));

        rows[0].Get().ShouldBe(1f);
    }

    [Fact]
    public void Get_ShowsAnExplicitZero_WhenTheSourceDefaultIsOne()
    {
        _text = "Thickness=0.0";
        List<SyntheticVariableRow> rows = BuildWithSourceDefaults(
            new Dictionary<string, ShaderParameterValue> { ["Thickness"] = ShaderParameterValue.FromFloat(1f) },
            FloatInfo("Thickness"));

        rows[0].Get().ShouldBe(0f);
    }

    [Fact]
    public void Set_StoresZero_WhenTheSourceDefaultIsOne()
    {
        List<SyntheticVariableRow> rows = BuildWithSourceDefaults(
            new Dictionary<string, ShaderParameterValue> { ["Thickness"] = ShaderParameterValue.FromFloat(1f) },
            FloatInfo("Thickness"));

        rows[0].Set!(0f);

        _text.ShouldBe("Thickness=0.0");
        rows[0].Get().ShouldBe(0f);
    }

    [Fact]
    public void Get_KeepsTheCompiledDefault_ForAParameterTheSourceDoesNotDefault()
    {
        List<SyntheticVariableRow> rows = BuildWithSourceDefaults(
            new Dictionary<string, ShaderParameterValue> { ["Other"] = ShaderParameterValue.FromFloat(9f) },
            FloatInfo("Thickness", defaultValue: 2.5f));

        rows[0].Get().ShouldBe(2.5f);
    }

    [Fact]
    public void Get_ShowsTheSourceDefaultOfEveryVectorComponent()
    {
        List<SyntheticVariableRow> rows = BuildWithSourceDefaults(
            new Dictionary<string, ShaderParameterValue> { ["Tint"] = ShaderParameterValue.FromVector4(1f, 0.5f, 0.25f, 1f) },
            new ShaderParameterInfo("Tint", ShaderParameterKind.Float, 4, ShaderParameterValue.FromVector4(0f, 0f, 0f, 0f)));

        rows.Select(row => row.Get()).ToArray().ShouldBe(new object[] { 1f, 0.5f, 0.25f, 1f });
    }

    [Fact]
    public void Vector_Set_StartsFromTheSourceDefaultWhenNothingIsStored()
    {
        List<SyntheticVariableRow> rows = BuildWithSourceDefaults(
            new Dictionary<string, ShaderParameterValue> { ["Size"] = ShaderParameterValue.FromVector2(3f, 4f) },
            new ShaderParameterInfo("Size", ShaderParameterKind.Float, 2, ShaderParameterValue.FromVector2(0f, 0f)));

        rows[0].Set!(2f);

        _text.ShouldBe("Size=(2.0, 4.0)");
    }

    [Fact]
    public void Get_ShowsTheSourceDefaultOfAnIntAndABool()
    {
        List<SyntheticVariableRow> rows = BuildWithSourceDefaults(
            new Dictionary<string, ShaderParameterValue>
            {
                ["Steps"] = ShaderParameterValue.FromInt(4),
                ["Glow"] = ShaderParameterValue.FromBool(true),
            },
            new ShaderParameterInfo("Steps", ShaderParameterKind.Int, 1, ShaderParameterValue.FromInt(0)),
            new ShaderParameterInfo("Glow", ShaderParameterKind.Bool, 1, ShaderParameterValue.FromBool(false)));

        rows[0].Get().ShouldBe(4);
        rows[1].Get().ShouldBe(true);
    }

    [Fact]
    public void Get_IgnoresASourceDefaultOfAnotherKindOrSize()
    {
        List<SyntheticVariableRow> rows = BuildWithSourceDefaults(
            new Dictionary<string, ShaderParameterValue>
            {
                ["Steps"] = ShaderParameterValue.FromFloat(4f),
                ["Tint"] = ShaderParameterValue.FromVector2(1f, 1f),
            },
            new ShaderParameterInfo("Steps", ShaderParameterKind.Int, 1, ShaderParameterValue.FromInt(2)),
            new ShaderParameterInfo("Tint", ShaderParameterKind.Float, 4, ShaderParameterValue.FromVector4(0.5f, 0.5f, 0.5f, 0.5f)));

        rows[0].Get().ShouldBe(2);
        rows[1].Get().ShouldBe(0.5f);
    }
}
