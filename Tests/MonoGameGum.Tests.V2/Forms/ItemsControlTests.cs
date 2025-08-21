using Gum.Forms.DefaultVisuals;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class ItemsControlTests : BaseTestClass
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var itemsControl = new Gum.Forms.Controls.ItemsControl();
        itemsControl.Visual.ShouldNotBeNull();
        (itemsControl.Visual is Gum.Forms.DefaultVisuals.ItemsControlVisual).ShouldBeTrue();
    }

    [Fact]
    public void ItemsControlVisual_ShouldCreateItemsControlForms()
    {
        var visual = new ItemsControlVisual();
        visual.FormsControl.ShouldNotBeNull();
    }


}
