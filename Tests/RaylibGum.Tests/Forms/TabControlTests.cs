using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Keys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// Raylib coverage for <see cref="TabControl"/>: the default visuals build, tabs pair with pages, and
/// the Right key steps the selection. The full behavior suite is MonoGameGum.Tests' TabControlTests.
/// </summary>
public class TabControlTests : BaseTestClass
{
    [Fact]
    public void AddTab_ShouldPairTabWithPageAndShowOnlySelectedPage()
    {
        TabControl tabControl = new TabControl();
        tabControl.AddToRoot();

        tabControl.AddTab("A", new Button());
        tabControl.AddTab("B", new Button());

        tabControl.TabItems.Count.ShouldBe(2);
        tabControl.Pages.Count.ShouldBe(2);
        tabControl.SelectedIndex.ShouldBe(-1);

        tabControl.SelectedIndex = 0;
        tabControl.Pages[0].Visual.Visible.ShouldBeTrue();
        tabControl.Pages[1].Visual.Visible.ShouldBeFalse();
    }

    [Fact]
    public void RightKey_ShouldSelectNextTab()
    {
        TabControl tabControl = new TabControl();
        tabControl.AddTab("A");
        tabControl.AddTab("B");
        tabControl.SelectedIndex = 0;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyTyped(Keys.Right)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        tabControl.OnFocusUpdate();

        tabControl.SelectedIndex.ShouldBe(1);
    }
}
