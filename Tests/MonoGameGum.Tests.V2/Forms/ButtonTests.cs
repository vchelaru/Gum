using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Microsoft.Xna.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class ButtonTests : BaseTestClass
{
    public ButtonTests () : base()
    {
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        Button sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        Button button = new();
        button.Visual.ShouldNotBeNull();
        (button.Visual is ButtonVisual).ShouldBeTrue();
    }

    [Fact]
    public void Cursor_VisualOver_ShouldReturnButtonVisual()
    {
        Button button = new();
        button.AddToRoot();

        GumService.Default.Update(new GameTime());

        var cursor = FrameworkElement.MainCursor;

        cursor.VisualOver.ShouldBe(button.Visual);
    }
}
