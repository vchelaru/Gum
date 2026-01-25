using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.Input;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class MenuItemTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        MenuItem menuItem = new ();
        menuItem.Visual.ShouldNotBeNull();
        (menuItem.Visual is Gum.Forms.DefaultVisuals.MenuItemVisual).ShouldBeTrue();
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        MenuItem sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void ShowSubItem_ShouldAcceptVisualTemplate_OfScrollViewerVisual()
    {
        MenuItem menuItem = new();
        for(int i = 0; i < 10; i++)
        {
            menuItem.Items.Add(i);
        }

        menuItem.ScrollViewerVisualTemplate = new Gum.Forms.VisualTemplate(() =>
        {
            var scrollViewer = new ScrollViewer();
            return scrollViewer.Visual;
        });

        // no exception should be thrown
        menuItem.IsSelected = true;
    }

    [Fact]
    public void ClickMenuItem_ShouldRaiseClickEvent()
    {
        Menu menu = new();
        menu.AddToRoot();
        menu.Name = "Menu";

        MenuItem topItem = new();
        menu.Items.Add(topItem);
        topItem.Header = "Top Item";
        topItem.Name = "Top Item";

        var wasTopClicked = false;
        topItem.Clicked += (not, used) =>
        {
            wasTopClicked = true;
        };
        var wasSubClicked = false;

        MenuItem subItem = new();
        subItem.Header = "Sub Item ";
        // make it big to force it:
        subItem.Name = "Sub Item";
        subItem.Width = 100;
        subItem.Height = 100;
        topItem.Items.Add(subItem);
        subItem.Clicked += (not, used) =>
        {
            wasSubClicked = true;
        };


        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.PrimaryClick).Returns(true);
        FormsUtilities.SetCursor(cursor.Object);
        cursor.SetupProperty(x => x.WindowOver);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.Setup(x => x.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(x => x.PrimaryPush).Returns(true);

        GumService.Default.Root.UpdateLayout();

        cursor.Setup(x => x.X).Returns((int)(topItem.Visual.AbsoluteLeft + 1));
        cursor.Setup(x => x.Y).Returns((int)(topItem.Visual.AbsoluteTop + 1));
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns((int)(topItem.Visual.AbsoluteLeft + 1));
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns((int)(topItem.Visual.AbsoluteTop + 1));

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        if(wasTopClicked == false)
        {
            var failureReason = CursorExtensions.GetEventFailureReason(cursor.Object, subItem);
        }

        wasTopClicked.ShouldBeTrue();

        cursor.Setup(x => x.X).Returns((int)(subItem.Visual.AbsoluteLeft + 1));
        cursor.Setup(x => x.Y).Returns((int)(subItem.Visual.AbsoluteTop + 1));
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns((int)(subItem.Visual.AbsoluteLeft + 1));
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns((int)(subItem.Visual.AbsoluteTop + 1));

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());


        wasSubClicked.ShouldBeTrue();

    }
}
