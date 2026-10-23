using Gum.Automation;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Automation;

/// <summary>
/// Each <see cref="GumDriver"/> capability against real Forms controls, driven through the scripted
/// cursor and keyboard only.
/// </summary>
public class GumDriverTests : BaseTestClass
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    private readonly GumDriver _driver;

    public GumDriverTests()
    {
        _driver = GumDriver.ForGumService(Gum.GumService.Default);
        _driver.Install();
    }

    public override void Dispose()
    {
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        base.Dispose();
    }

    [Fact]
    public void Install_ReplacesFormsCursorAndKeyboard()
    {
        FormsUtilities.Cursor.ShouldBeSameAs(_driver.Cursor);
        FormsUtilities.Keyboard.ShouldBeSameAs(_driver.Keyboard);
    }

    [Fact]
    public void Click_ByName_RaisesClickOnce()
    {
        Button button = new Button { Name = "Ok" };
        button.AddToRoot();
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        _driver.Click("Ok");

        clicks.ShouldBe(1);
    }

    [Fact]
    public void Click_SecondaryButton_DoesNotRaiseClick()
    {
        Button button = new Button { Name = "Ok" };
        button.AddToRoot();
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        _driver.Click("Ok", MouseButton.Secondary);

        clicks.ShouldBe(0);
    }

    [Fact]
    public void TypeAndPressKey_GoToTheFocusedTextBox()
    {
        TextBox textBox = new TextBox { Name = "Box" };
        textBox.AddToRoot();

        _driver.Click("Box");
        _driver.Type("abc");
        _driver.PressKey(GumKeys.Back);

        textBox.Text.ShouldBe("ab");
    }

    [Fact]
    public void Drag_FromNamedElement_PushesItAndRaisesDraggingAlongThePath()
    {
        Button handle = new Button { Name = "Handle", Width = 40, Height = 40 };
        handle.AddToRoot();
        Gum.GumService.Default.Root.UpdateLayout();
        List<int> dragXs = new();
        handle.Visual.Dragging += (_, _) => dragXs.Add(_driver.Cursor.X);

        _driver.Drag("Handle", toX: 300, toY: 20, moveFrames: 4);

        // Dragging fires on the push frame and on each of the four move frames.
        dragXs.Count.ShouldBe(5);
        dragXs[^1].ShouldBe(300);
        _driver.Cursor.VisualPushed.ShouldBeNull();
    }

    [Fact]
    public void Scroll_OverScrollViewer_MovesItsVerticalOffset()
    {
        ScrollViewer scrollViewer = new ScrollViewer { Name = "Scroller", Width = 200, Height = 100 };
        scrollViewer.AddToRoot();
        for (int i = 0; i < 20; i++)
        {
            scrollViewer.AddChild(new Button { Text = i.ToString() });
        }
        Gum.GumService.Default.Root.UpdateLayout();

        _driver.Scroll("Scroller", detents: -2);

        scrollViewer.VerticalScrollBarValue.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Read_PrefersTheFormsControlPropertyThenTheVisual()
    {
        CheckBox checkBox = new CheckBox { Name = "Agree", IsChecked = true };
        checkBox.AddToRoot();

        _driver.Read("Agree", "IsChecked").ShouldBe(true);
        _driver.Read("Agree", "Name").ShouldBe("Agree");
        Should.Throw<GumAutomationException>(() => _driver.Read("Agree", "NoSuchProperty"))
            .Message.ShouldContain("NoSuchProperty");
    }

    [Fact]
    public void Step_CountsFrames()
    {
        _driver.Step(4);

        _driver.FrameCount.ShouldBe(4);
    }

    [Fact]
    public void Screenshot_WithoutAWriter_ThrowsNamingTheProblem()
    {
        Should.Throw<GumAutomationException>(() => _driver.Screenshot("out.png"))
            .Message.ShouldContain("screenshot");
    }
}
