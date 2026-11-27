using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGameGum.Tests.Input;
using MonoGameGum.TestsCommon;
using Shouldly;
using System;
using Xunit;
using Xunit.Abstractions;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// A frame of <see cref="Gum.GumService.Update(GameTime)"/> in a common idle input state allocates nothing (#8472):
/// a focused control, a hovered control, a held press, an open ComboBox drop-down, keyboard navigation.
/// Real cursor and keyboard subclasses feed fixed input, since a Moq proxy allocates on every read.
/// </summary>
public class InputUpdateAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    private readonly FixedCursor _cursor = new FixedCursor();
    private readonly FixedKeyboard _keyboard = new FixedKeyboard { KeyboardStateProcessor = new FixedKeyboardState() };
    private readonly GameTime _gameTime = new GameTime();

    public InputUpdateAllocationTests(ITestOutputHelper output)
    {
        _output = output;
        FormsUtilities.SetCursor(_cursor);
        FormsUtilities.SetKeyboard(_keyboard);
        FrameworkElement.MainCursor = _cursor;
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

    private sealed class FixedCursor : Cursor
    {
        public FixedCursor() : base((GameWindow?)null) { }

        public MouseState Next { get; set; }

        protected override MouseState GetMouseState() => Next;

        protected override TouchCollection GetTouchCollection() => default;
    }

    private sealed class FixedKeyboardState : KeyboardStateProcessor
    {
        protected override KeyboardState GetKeyboardState() => default;
    }

    private sealed class FixedKeyboard : Gum.Input.Keyboard
    {
        protected override string GetTypedText() => "";
    }

    private void Frame()
    {
        TimeSpan step = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        _gameTime.TotalGameTime += step;
        _gameTime.ElapsedGameTime = step;
        Gum.GumService.Default.Update(_gameTime);
    }

    private void PlaceCursor(GraphicalUiElement over, bool leftDown = false)
    {
        int x = (int)(over.AbsoluteLeft + over.AbsoluteWidth / 2);
        int y = (int)(over.AbsoluteTop + over.AbsoluteHeight / 2);
        _cursor.Next = new MouseState(x, y, 0, leftDown ? ButtonState.Pressed : ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
    }

    private void PlaceCursorAway()
    {
        _cursor.Next = new MouseState(790, 590, 0, ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
    }

    private long MeasureIdleFrames(string state)
    {
        AllocationResult result = AllocationMeasurer.Measure(Frame, warmupIterations: 60, measuredIterations: 300);
        _output.WriteLine($"{state}: {result.TotalBytes} bytes over {result.Iterations} frames");
        return result.TotalBytes;
    }

    [Fact]
    public void Update_FocusedButton_AllocatesNothing()
    {
        Button button = new Button();
        button.AddToRoot();
        PlaceCursorAway();
        button.IsFocused = true;

        MeasureIdleFrames("focused Button").ShouldBe(0);
        InteractiveGue.CurrentInputReceiver.ShouldBe(button);
    }

    [Fact]
    public void Update_FocusedTextBoxWithCaret_AllocatesNothing()
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        textBox.Text = "hello";
        PlaceCursorAway();
        textBox.IsFocused = true;

        MeasureIdleFrames("focused TextBox").ShouldBe(0);
        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);
    }

    [Fact]
    public void Update_FocusedListBox_AllocatesNothing()
    {
        ListBox listBox = new ListBox();
        listBox.AddToRoot();
        for (int i = 0; i < 5; i++)
        {
            listBox.Items.Add("Item " + i);
        }
        listBox.SelectedIndex = 0;
        PlaceCursorAway();
        listBox.IsFocused = true;

        MeasureIdleFrames("focused ListBox").ShouldBe(0);
        InteractiveGue.CurrentInputReceiver.ShouldBe(listBox);
    }

    [Fact]
    public void Update_CursorHoveringButton_AllocatesNothing()
    {
        Button button = new Button();
        button.AddToRoot();
        PlaceCursor(button.Visual);

        MeasureIdleFrames("hovered Button").ShouldBe(0);
        _cursor.VisualOver.ShouldBe(button.Visual);
    }

    [Fact]
    public void Update_CursorMovingOverButton_AllocatesNothing()
    {
        Button button = new Button();
        button.AddToRoot();
        int rollOverBubblingCount = 0;
        button.Visual.RollOverBubbling += (_, _) => rollOverBubblingCount++;
        PlaceCursor(button.Visual);
        MouseState center = _cursor.Next;
        bool right = false;

        AllocationResult result = AllocationMeasurer.Measure(() =>
        {
            right = !right;
            _cursor.Next = new MouseState(center.X + (right ? 1 : 0), center.Y, 0, ButtonState.Released,
                ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            Frame();
        }, warmupIterations: 60, measuredIterations: 300);
        _output.WriteLine($"cursor moving over Button: {result.TotalBytes} bytes over {result.Iterations} frames");

        result.TotalBytes.ShouldBe(0);
        rollOverBubblingCount.ShouldBeGreaterThanOrEqualTo(300);
    }

    [Fact]
    public void Update_FocusedTextBoxAndHoveredButtonWithToolTip_AllocatesNothing()
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        Button button = new Button();
        button.AddToRoot();
        button.Y = 100;
        button.ToolTip = "tip";
        button.Visual.UpdateLayout();
        PlaceCursor(button.Visual);
        textBox.IsFocused = true;
        // Long enough that the measured frames see one tooltip staying open, not its timed close and reopen.
        ToolTipService.ShowDuration = TimeSpan.FromHours(1);
        try
        {
            MeasureIdleFrames("focused TextBox + hovered Button with open tooltip").ShouldBe(0);
            _cursor.VisualOver.ShouldBe(button.Visual);
            FrameworkElement.PopupRoot.Children.Count.ShouldBe(1);
        }
        finally
        {
            ToolTipService.ResetForTesting();
            ToolTipService.ShowDuration = TimeSpan.FromMilliseconds(5000);
        }
    }

    [Fact]
    public void Update_FocusedButtonWithHotReloadEnabled_AllocatesNothing()
    {
        string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GumHotReloadAlloc_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        Gum.GumService service = Gum.GumService.Default;
        try
        {
            service.EnableHotReload(System.IO.Path.Combine(folder, "Project.gumx"));
            Button button = new Button();
            button.AddToRoot();
            PlaceCursorAway();
            button.IsFocused = true;

            MeasureIdleFrames("focused Button, hot reload enabled").ShouldBe(0);
        }
        finally
        {
            service._hotReloadManager?.Stop();
            service._hotReloadManager = null;
            System.IO.Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Update_HeldPressOnButton_AllocatesNothing()
    {
        Button button = new Button();
        button.AddToRoot();
        PlaceCursor(button.Visual, leftDown: true);

        MeasureIdleFrames("held press on Button").ShouldBe(0);
        _cursor.VisualPushed.ShouldBe(button.Visual);
    }

    [Fact]
    public void Update_OpenComboBoxDropDown_AllocatesNothing()
    {
        ComboBox comboBox = new ComboBox();
        comboBox.AddToRoot();
        for (int i = 0; i < 5; i++)
        {
            comboBox.Items.Add("Item " + i);
        }
        PlaceCursorAway();
        Frame();
        comboBox.IsDropDownOpen = true;
        Frame();
        GraphicalUiElement firstItem = (GraphicalUiElement)comboBox.ListBox.ListBoxItems[0].Visual;
        PlaceCursor(firstItem);

        MeasureIdleFrames("open ComboBox drop-down").ShouldBe(0);
        comboBox.IsDropDownOpen.ShouldBeTrue();
        _cursor.VisualOver.ShouldBe(firstItem);
    }

    [Fact]
    public void Update_KeyboardNavigationIdleOnFocusedButton_AllocatesNothing()
    {
        FrameworkElement.KeyboardsForUiControl.Add(_keyboard);
        Button first = new Button();
        first.AddToRoot();
        Button second = new Button();
        second.AddToRoot();
        second.Y = 50;
        PlaceCursorAway();
        first.IsFocused = true;

        MeasureIdleFrames("keyboard navigation idle").ShouldBe(0);
        InteractiveGue.CurrentInputReceiver.ShouldBe(first);
    }

    [Fact]
    public void Update_GamepadNavigationIdleOnFocusedButton_AllocatesNothing()
    {
        Gum.Input.GamePad pad = new Gum.Input.GamePad();
        pad.Activity(new GamePadState(Vector2.Zero, Vector2.Zero, 0, 0, (Buttons)0), 0);
        FrameworkElement.GamePadsForUiControl.Add(pad);
        Button first = new Button();
        first.AddToRoot();
        Button second = new Button();
        second.AddToRoot();
        second.Y = 50;
        PlaceCursorAway();
        first.IsFocused = true;

        MeasureIdleFrames("gamepad navigation idle").ShouldBe(0);
        InteractiveGue.CurrentInputReceiver.ShouldBe(first);
    }
}
