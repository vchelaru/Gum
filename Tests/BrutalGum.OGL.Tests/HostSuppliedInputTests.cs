using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using RenderingLibrary;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;
using System.Linq;
using System.Reflection;
using GumKeys = Gum.Forms.Input.Keys;

namespace BrutalGum.Tests;

/// <summary>
/// A host that is not GLFW (SDL3, a custom window) feeds Gum through the public <see cref="IBrutalInput"/>
/// and nothing else. These tests initialize the service over such a host and drive real Forms controls
/// through frames, proving the seam end to end without a window.
/// </summary>
public class HostSuppliedInputTests : BaseTestClass
{
    private readonly FakeBrutalInput _host = new();
    private double _time = 100;

    public HostSuppliedInputTests()
    {
        GumService.Default.Uninitialize();
        InitializeOver(_host);
    }

    public override void Dispose()
    {
        base.Dispose();
        GumService.Default.Uninitialize();
        InitializeOver(TestAssemblyInitialize.Input);
        SkiaHostTestBase.CaptureRenderableBaseline();
        FrameworkElement.KeyboardsForUiControl.Clear();
    }

    private static void InitializeOver(IBrutalInput input)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.Initialize(surface.Canvas, input, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
    }

    private void Frame() => GumService.Default.Update(_time += 1);

    private static Button AddButton()
    {
        Button button = new();
        button.AddToRoot();
        button.X = 10;
        button.Y = 10;
        button.Width = 100;
        button.Height = 40;
        return button;
    }

    [Fact]
    public void Update_HostPressesAndReleasesOverButton_FiresClick()
    {
        Button button = AddButton();
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        _host.CursorX = 60;
        _host.CursorY = 30;
        Frame();

        _host.HeldButtons.Add(MouseButton.Left);
        Frame();
        _host.HeldButtons.Remove(MouseButton.Left);
        Frame();

        clicks.ShouldBe(1);
    }

    [Theory]
    [InlineData(30, 15, 1)]
    [InlineData(150, 100, 0)]
    public void Update_HighDpiHost_ScalesWindowCursorToFramebufferPixels(double windowX, double windowY, int expectedClicks)
    {
        // The window is half the framebuffer, so the host's cursor must be doubled to land on Gum's canvas.
        _host.WindowWidth = 400;
        _host.WindowHeight = 300;
        _host.FramebufferWidth = 800;
        _host.FramebufferHeight = 600;
        Button button = AddButton();
        button.X = 50;
        button.Y = 20;
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        _host.CursorX = windowX;
        _host.CursorY = windowY;
        Frame();

        _host.HeldButtons.Add(MouseButton.Left);
        Frame();
        _host.HeldButtons.Remove(MouseButton.Left);
        Frame();

        clicks.ShouldBe(expectedClicks);
    }

    [Fact]
    public void Update_HostPressesAwayFromButton_DoesNotClick()
    {
        Button button = AddButton();
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        _host.CursorX = 600;
        _host.CursorY = 400;
        Frame();

        _host.HeldButtons.Add(MouseButton.Left);
        Frame();
        _host.HeldButtons.Remove(MouseButton.Left);
        Frame();

        clicks.ShouldBe(0);
    }

    [Fact]
    public void Update_HostTypesCharacters_AppendsToFocusedTextBox()
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.IsFocused = true;

        _host.RaiseChar('h');
        _host.RaiseChar('i');
        Frame();

        textBox.Text.ShouldBe("hi");
    }

    [Fact]
    public void Update_HostHoldsBackspace_RemovesTypedCharacter()
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.Text = "ab";
        textBox.CaretIndex = 2;
        textBox.IsFocused = true;

        _host.HeldKeys.Add(GumKeys.Back);
        Frame();

        textBox.Text.ShouldBe("a");
    }

    [Fact]
    public void Update_HostScrolls_RaisesCursorZVelocity()
    {
        Frame();

        _host.RaiseScroll(2);
        Frame();

        IGumService.Default!.Cursor!.ZVelocity.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Update_HostPastesWithCtrlV_InsertsHostClipboardText()
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.IsFocused = true;
        _host.ClipboardText = "from the host";
        _host.HeldKeys.Add(GumKeys.LeftControl);
        Frame();

        _host.HeldKeys.Add(GumKeys.V);
        Frame();

        textBox.Text.ShouldBe("from the host");
    }

    [Fact]
    public void Update_TextBoxCopies_WritesHostClipboard()
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.Text = "copy me";
        textBox.IsFocused = true;
        textBox.SelectAll();
        _host.HeldKeys.Add(GumKeys.LeftControl);
        Frame();

        _host.HeldKeys.Add(GumKeys.C);
        Frame();

        _host.ClipboardText.ShouldBe("copy me");
    }

    [Fact]
    public void PublicSurface_OfInputSeam_ExposesNoBrutalTypes()
    {
        typeof(IBrutalInput).IsPublic.ShouldBeTrue();

        IEnumerable<Type> exposed = typeof(IBrutalInput)
            .GetMembers()
            .SelectMany(member => member switch
            {
                PropertyInfo property => new[] { property.PropertyType },
                MethodInfo method => method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType),
                EventInfo e => new[] { e.EventHandlerType! },
                _ => Array.Empty<Type>(),
            })
            .SelectMany(type => type.IsGenericType ? type.GetGenericArguments().Append(type) : new[] { type });

        exposed.Where(type => type.Namespace?.StartsWith("Brutal") == true).ShouldBeEmpty();
    }
}
