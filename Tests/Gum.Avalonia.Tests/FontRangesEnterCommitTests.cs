using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Gum.Avalonia.Settings;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>Enter in a font range text box gives up the focus so the edit commits at once (Avalonia 12 has no FocusManager.ClearFocus).</summary>
public class FontRangesEnterCommitTests
{
    [AvaloniaFact]
    public void CommitOnEnter_Enter_TakesTheFocusOffTheBox()
    {
        TextBox box = new TextBox();
        Window window = new Window { Content = box };
        window.Show();
        box.Focus();
        Dispatcher.UIThread.RunJobs();
        box.IsFocused.ShouldBeTrue("setup: the box should hold the focus");
        typeof(FontRangesDisplay).GetMethod("CommitOnEnter", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { box });

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        box.IsFocused.ShouldBeFalse();
    }
}
