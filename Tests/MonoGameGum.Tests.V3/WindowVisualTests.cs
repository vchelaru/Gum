using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class WindowVisualTests
{
    [Fact]
    public void Constructor_ShouldCreateV3Visual()
    {
        Window window = new();
        window.Visual.ShouldNotBeNull();
        (window.Visual is WindowVisual).ShouldBeTrue();
    }

    [Fact]
    public void MakeSizedToChildren_ShouldSetCorrectProperties()
    {
        Window window = new();
        WindowVisual windowVisual = (WindowVisual)window.Visual;

        windowVisual.MakeSizedToChildren(1, 2, 3, 4);

        ContainerRuntime child = new();
        window.AddChild(child);
        child.Width = 100;
        child.Height = 150;
        child.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        child.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        window.Visual.GetAbsoluteWidth().ShouldBe(100 + 1 + 3);
        window.Visual.GetAbsoluteHeight().ShouldBe(150 + 2 + 4);
    }

    [Fact]
    public void Window_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        Window sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
