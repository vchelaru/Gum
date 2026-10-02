using Gum.DataTypes;
using Gum.DataTypes.Variables;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

public class VariableSaveColorOperationTests
{
    [Fact]
    public void FixEnumerations_ShouldPromoteColorOperationInt_ToEnum()
    {
        // A loaded .gumx holds enum variables as their underlying int; the runtime's own coercion
        // (no tool hook) must turn ColorOperation back into the enum.
        VariableSave variable = new VariableSave
        {
            Name = "Instance.ColorOperation",
            Type = "ColorOperation",
            Value = (int)ColorOperation.Add,
        };

        bool changed = variable.FixEnumerations();

        changed.ShouldBeTrue();
        variable.Value.ShouldBe(ColorOperation.Add);
    }

    [Fact]
    public void ConvertEnumerationValuesToInts_ShouldDemoteColorOperation_ToInt()
    {
        VariableSave variable = new VariableSave
        {
            Name = "Instance.ColorOperation",
            Type = "ColorOperation",
            Value = ColorOperation.ColorTextureAlpha,
        };

        variable.ConvertEnumerationValuesToInts();

        variable.Value.ShouldBe((int)ColorOperation.ColorTextureAlpha);
    }
}
