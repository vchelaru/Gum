using Gum.Collections;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
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
    public void Visual_HasEvents_ShouldBeTrue()
    {
        Panel sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void Panel_Width_ShouldDependOnChildren()
    {
        Panel panel = new();
        panel.Width.ShouldBe(0);
        panel.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
    }

    [Fact]
    public void Visual_Reassigned_ShouldStopReactingToOldVisualChildren()
    {
        InteractiveGue oldVisual = new(new InvisibleRenderable());
        Panel sut = new(oldVisual);
        InteractiveGue newVisual = new(new InvisibleRenderable());
        sut.Visual = newVisual;

        // Added without notification so only a stale handler on the old visual could pick it up.
        Button child = new();
        ((ObservableCollectionNoReset<GraphicalUiElement>)newVisual.Children!)
            .InsertWithoutNotification(0, child.Visual);

        oldVisual.Children!.Add(new InteractiveGue(new InvisibleRenderable()));

        sut.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void Visual_SetToNull_ShouldClearChildren()
    {
        Panel sut = new();
        Button child = new();
        sut.Visual.Children!.Add(child.Visual);
        sut.Children.Count.ShouldBe(1);

        sut.Visual = null!;

        sut.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void Visual_Reassigned_ShouldReflectNewVisualChildren()
    {
        Panel sut = new();
        sut.Visual.Children!.Add(new Button().Visual);
        InteractiveGue newVisual = new(new InvisibleRenderable());
        Button newChild = new();
        newVisual.Children!.Add(newChild.Visual);

        sut.Visual = newVisual;

        sut.Children.ShouldBe(new FrameworkElement[] { newChild });
    }
}
