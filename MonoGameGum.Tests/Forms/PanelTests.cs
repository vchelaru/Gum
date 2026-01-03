using Gum.Forms.Controls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class PanelTests : BaseTestClass
{
    [Fact]
    public void Panel_Width_ShouldDependOnChildren()
    {
        Panel panel = new();
        panel.Width.ShouldBe(0);
        panel.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
    }
}
