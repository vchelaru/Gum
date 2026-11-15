using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FluentIcons.Avalonia;
using FluentIcons.Common;
using Gum.Avalonia.Diagnostics;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// An unattended screenshot must wait until icons are drawn (#8091): a FluentIcon fills in its glyph
/// from a Loaded handler that runs after the layout pass that created it.
/// </summary>
public class UiSettleTests
{
    [AvaloniaFact]
    public async Task SettleAsync_AfterAnIconIsAddedToAShownWindow_TheCaptureContainsTheGlyph()
    {
        StackPanel host = new StackPanel { Background = Brushes.White };
        Window window = new Window
        {
            Width = 200,
            Height = 120,
            Content = host,
            RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Light,
        };
        window.Show();
        await UiSettle.SettleAsync(window);

        // The tree's expander arrows and the States icon are created like this: after the window is up.
        host.Children.Add(new FluentIcon { Icon = Icon.Add, FontSize = 48, Width = 48, Height = 48, Foreground = Brushes.Black });
        await UiSettle.SettleAsync(window);

        DarkPixelCount(window).ShouldBeGreaterThan(20, "the glyph must be drawn once the UI has settled");
    }

    [AvaloniaFact]
    public async Task SettleAsync_WithNothingPending_Returns()
    {
        Window window = new Window { Width = 100, Height = 100, Content = new Border() };
        window.Show();

        await UiSettle.SettleAsync(window).WaitAsync(TimeSpan.FromSeconds(10));
    }

    private static int DarkPixelCount(Window window)
    {
        PixelSize size = new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height);
        using RenderTargetBitmap bitmap = new RenderTargetBitmap(size);
        bitmap.Render(window);
        using MemoryStream stream = new MemoryStream();
        bitmap.Save(stream);
        stream.Position = 0;
        using WriteableBitmap decoded = WriteableBitmap.Decode(stream);
        using global::Avalonia.Platform.ILockedFramebuffer buffer = decoded.Lock();
        byte[] bytes = new byte[buffer.RowBytes * buffer.Size.Height];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);
        int dark = 0;
        for (int y = 0; y < buffer.Size.Height; y++)
        {
            for (int x = 0; x < buffer.Size.Width; x++)
            {
                int offset = y * buffer.RowBytes + x * 4;
                if (bytes[offset] < 100 && bytes[offset + 1] < 100 && bytes[offset + 2] < 100)
                {
                    dark++;
                }
            }
        }
        return dark;
    }
}
