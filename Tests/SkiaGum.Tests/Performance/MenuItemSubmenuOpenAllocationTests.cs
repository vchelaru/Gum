using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// Managed allocation of opening and closing a <see cref="MenuItem"/> submenu on the Skia runtime (#7864).
/// A menu bar is swept several times a second, so each open must reuse the popup and rows it built the
/// time before. Each cycle opens the submenu, runs one frame, closes it and runs one frame.
/// </summary>
public class MenuItemSubmenuOpenAllocationTests : BaseTestClass
{
    // Open + frame + close + frame. Rebuilding the popup each open cost 274 KB (supplied rows) and 454 KB (generated rows).
    private const int CycleLimitBytes = 30000;

    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost();
    private readonly FrameCursor _cursor = new FrameCursor();
    private readonly FrameKeyboard _keyboard = new FrameKeyboard();
    private readonly ICursor _previousCursor = FrameworkElement.MainCursor;
    private readonly IInputReceiverKeyboard _previousKeyboard = FrameworkElement.MainKeyboard;
    private double _inputSeconds;

    public MenuItemSubmenuOpenAllocationTests(ITestOutputHelper output)
    {
        _output = output;
        // Mocks allocate on every member access; production always has a real cursor.
        FrameworkElement.MainCursor = _cursor;
        FrameworkElement.MainKeyboard = _keyboard;
        FrameworkElement.KeyboardsForUiControl.Add(_keyboard);
    }

    public override void Dispose()
    {
        InteractiveGue.CurrentInputReceiver = null;
        FrameworkElement.KeyboardsForUiControl.Remove(_keyboard);
        FrameworkElement.MainCursor = _previousCursor;
        FrameworkElement.MainKeyboard = _previousKeyboard;
        GumService.Default.PopupRoot.Children!.Clear();
        _keyboard.Dispose();
        _host.Dispose();
        base.Dispose();
    }

    private void RunFrame()
    {
        _inputSeconds += 1.0 / 60.0;
        FormsUtilities.Update(_inputSeconds, GumService.Default.Root);
        _host.Update();
        _host.Draw();
    }

    // A menu bar with one top-level item whose submenu holds five rows: MenuItems supplied in Items,
    // or strings the item turns into rows itself.
    private MenuItem CreateMenu(bool generatedRows)
    {
        Menu menu = new Menu { X = 10, Y = 10 };
        menu.AddToRoot();

        MenuItem file = new MenuItem { Header = "File" };
        menu.Items!.Add(file);
        for (int i = 0; i < 5; i++)
        {
            if (generatedRows)
            {
                file.Items!.Add("Item " + i);
            }
            else
            {
                file.Items!.Add(new MenuItem { Header = "Item " + i });
            }
        }
        RunFrame();
        return file;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpenAndCloseSubmenu_FiveRows_AllocatesLittleAfterFirstOpen(bool generatedRows)
    {
        MenuItem file = CreateMenu(generatedRows);
        bool everOpen = false;
        long openBytes = 0;
        long closeBytes = 0;
        int cycles = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                long start = GC.GetAllocatedBytesForCurrentThread();
                file.IsSelected = true;
                RunFrame();
                long afterOpen = GC.GetAllocatedBytesForCurrentThread();
                everOpen |= file.IsPopupVisible;
                file.IsSelected = false;
                RunFrame();
                long afterClose = GC.GetAllocatedBytesForCurrentThread();
                openBytes += afterOpen - start;
                closeBytes += afterClose - afterOpen;
                cycles++;
            },
            attempts: 1,
            warmupIterations: 20,
            measuredIterations: 100);

        _output.WriteLine($"generatedRows={generatedRows}: cycle {result.BytesPerIteration:N0} bytes; " +
            $"open+frame {openBytes / cycles:N0}, close+frame {closeBytes / cycles:N0} (all cycles incl. warmup)");

        everOpen.ShouldBeTrue();
        file.IsPopupVisible.ShouldBeFalse();
        // The submenu opened and closed every cycle; reusing its popup and rows leaves only state changes to allocate.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(CycleLimitBytes);
    }
}
