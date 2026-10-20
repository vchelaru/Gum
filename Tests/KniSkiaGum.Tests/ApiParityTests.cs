using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;
using XnaGraphicsDevice = Microsoft.Xna.Framework.Graphics.GraphicsDevice;

namespace KniSkiaGum.Tests;

/// <summary>
/// The KNI Skia <see cref="GumService"/> mirrors the KniGum/MonoGameGum shape: Initialize overloads
/// that take a game, a visuals version or a graphics device, and a typed <c>ContentLoader</c>. These
/// tests re-initialize the assembly-wide service, so each one puts it back.
/// </summary>
public class ApiParityTests : BaseTestClass
{
    private const int Width = 640;
    private const int Height = 480;

    private static void RestoreSharedService()
    {
        GumService.Default.Uninitialize();

        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.InitializeWithoutWindow(surface.Canvas, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
        GumService.Default.Cursor.ReadMouse = () => default;
        GumService.Default.Keyboard.ReadKeyboard = () => default;

        SkiaHostTestBase.CaptureRenderableBaseline();
        FrameworkElement.KeyboardsForUiControl.Clear();
    }

    [Fact]
    public void Initialize_WithVisualsVersionV3_Initializes()
    {
        try
        {
            GumService.Default.Uninitialize();

            GumService.Default.InitializeWithoutWindow(Width, Height, visualsVersion: DefaultVisualsVersion.V3);

            GumService.Default.IsInitialized.ShouldBeTrue();
            new Button().Visual.ShouldNotBeNull();
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Initialize_WithAnUnsupportedVisualsVersion_ThrowsNotSupportedAndStaysUninitialized()
    {
        try
        {
            GumService.Default.Uninitialize();

            Should.Throw<NotSupportedException>(() =>
                GumService.Default.InitializeWithoutWindow(Width, Height, visualsVersion: (DefaultVisualsVersion)99));

            GumService.Default.IsInitialized.ShouldBeFalse();
            Should.NotThrow(() => GumService.Default.InitializeWithoutWindow(Width, Height));
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Initialize_WithANullGraphicsDevice_ThrowsArgumentNull()
    {
        Should.Throw<ArgumentNullException>(() => GumService.Default.Initialize((XnaGraphicsDevice)null!));
    }

    [Fact]
    public void IGumServiceInitialize_ThrowsNotSupported_NamingTheGameOverload()
    {
        IGumService service = GumService.Default;

        NotSupportedException withoutProject = Should.Throw<NotSupportedException>(() => service.Initialize());
        NotSupportedException withProject = Should.Throw<NotSupportedException>(() => service.Initialize("project.gumx"));

        withoutProject.Message.ShouldContain("Game");
        withProject.Message.ShouldContain("Game");
    }

    [Fact]
    public void ContentLoader_IsTheSkiaContentLoader_AfterInitialize()
    {
        GumService.Default.ContentLoader.ShouldBeOfType<ContentLoader>();
    }
}
