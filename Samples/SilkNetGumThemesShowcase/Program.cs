using Silk.NET.OpenGLES;
using Silk.NET.SDL;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using SkiaSharp;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gum;
using Gum.Wireframe;
using Gum.Themes.Bubblegum;
using Gum.Themes.DarkPro;
using Gum.Themes.Editor;
using Gum.Themes.ForestGlade;
using Gum.Themes.Hazard;
using MonoGameGumThemesShowcase.Screens;

namespace SilkNetGumThemesShowcase;

/// <summary>
/// Skia/SilkNet host for the Gum themes showcase. It drives the same <see cref="ShowcaseScreen"/>s
/// the MonoGame host (<c>MonoGameGumThemesShowcase</c>) and raylib host (<c>RaylibGumThemesShowcase</c>)
/// use -- the screen sources are source-shared across all three, so they render identically and can be
/// compared side by side.
///
/// Number keys swap the active theme (only themes with a Skia/SilkNet variant are listed); F1/F2 swap
/// the screen. Add a theme to <see cref="_themes"/> as each gains a <c>.SilkNet</c> variant (#3671).
/// </summary>
unsafe class Program
{
    private sealed class ThemeOption
    {
        public string Name { get; }
        public Action Apply { get; }
        public SKColor ClearColor { get; }

        public ThemeOption(string name, Action apply, SKColor clearColor)
        {
            Name = name;
            Apply = apply;
            ClearColor = clearColor;
        }
    }

    private static Sdl sdl = null!;
    private static GL gl = null!;
    private static IWindow window = null!;
    private static bool running = true;

    private static ThemeOption[] themes = null!;
    private static int currentThemeIndex;
    private static SKColor clearColor;
    private static ShowcaseScreen? currentScreen;
    private static Func<ShowcaseScreen> currentScreenFactory = () => new AllControlsScreen();

    private static int windowWidth = 1400;
    private static int windowHeight = 900;

    private static GumService GumUI => GumService.Default;

    private static void InitializeGum(SKCanvas canvas, IInputContext inputContext)
    {
        GumUI.Initialize(canvas, inputContext);
        GumUI.UseKeyboardDefaults();

        GraphicalUiElement.CanvasWidth = windowWidth;
        GraphicalUiElement.CanvasHeight = windowHeight;

        // Each theme's parameterless Apply() installs its visuals as the defaults. Editor has no
        // named background color, so a sensible dark surround is used, matching the MonoGame/raylib
        // showcases' own Editor clear color. The other themes read their own background token
        // directly -- already an SKColor via each Styling class's SKIA Color alias.
        themes = new[]
        {
            new ThemeOption("Editor", EditorTheme.Apply, new SKColor(40, 40, 40, 255)),
            new ThemeOption("Dark Pro", DarkProTheme.Apply, DarkProStyling.ActiveStyle.Colors.Background),
            new ThemeOption("Forest Glade", ForestGladeTheme.Apply, ForestGladeStyling.ActiveStyle.Colors.CanopyDeep),
            new ThemeOption("Bubblegum", BubblegumTheme.Apply, BubblegumStyling.ActiveStyle.Colors.Background),
            new ThemeOption("Hazard", HazardTheme.Apply, HazardStyling.ActiveStyle.Colors.Background),
        };

        ApplyTheme(0);
        RebuildScreen();

        WireKeyboard(inputContext);
    }

    private static void WireKeyboard(IInputContext inputContext)
    {
        if (inputContext.Keyboards.Count == 0)
        {
            return;
        }

        var keyboard = inputContext.Keyboards[0];
        keyboard.KeyDown += (_, key, _) =>
        {
            if (key == Key.Escape)
            {
                running = false;
                return;
            }

            if (key == Key.F1)
            {
                SwitchScreen(() => new AllControlsScreen());
                return;
            }

            if (key == Key.F2)
            {
                SwitchScreen(() => new ScreenshotScreen());
                return;
            }

            int themeIndex = key - Key.Number1;
            if (themeIndex >= 0 && themeIndex < themes.Length && themeIndex != currentThemeIndex)
            {
                ApplyTheme(themeIndex);
                RebuildScreen();
            }
        };
    }

    private static void ApplyTheme(int index)
    {
        currentThemeIndex = index;
        ThemeOption theme = themes[index];
        theme.Apply();
        clearColor = theme.ClearColor;
        window.Title = $"Gum Theme Showcase (Skia/SilkNet) - {index + 1}. {theme.Name}  (1-{themes.Length} swap theme, F1/F2 swap screen)";
    }

    private static void RebuildScreen() => SwitchScreen(currentScreenFactory);

    private static void SwitchScreen(Func<ShowcaseScreen> factory)
    {
        currentScreenFactory = factory;
        currentScreen?.Destroy();
        currentScreen = factory();
        currentScreen.Build();
    }

    private static void Draw()
    {
        GumUI.Draw();
    }

    static unsafe void Main(string[] args)
    {
        // ANGLE via D3D11, matching SilkNetGumSample's Windows default.
        Environment.SetEnvironmentVariable("ANGLE_DEFAULT_PLATFORM", "d3d11");

        sdl = Silk.NET.SDL.SdlProvider.SDL.Value;

        SKSurface? surface = null;
        GRBackendRenderTarget? renderTarget = null;
        SKCanvas canvas = null!;

        try
        {
            sdl.SetHint("SDL_OPENGL_ES_DRIVER", "1");
            sdl.SetHint("SDL_HINT_OPENGL_ES_DRIVER", "angle");

            var api = new GraphicsAPI(
                ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 0));

            SdlWindowing.Use();

            var options = WindowOptions.Default;
            options.API = api;
            options.Size = new Vector2D<int>(windowWidth, windowHeight);
            options.Title = "Gum Theme Showcase (Skia/SilkNet)";
            options.WindowState = WindowState.Normal;
            options.WindowBorder = WindowBorder.Resizable;
            options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8);
            options.PreferredDepthBufferBits = 24;
            options.PreferredStencilBufferBits = 8;
            options.VSync = false;

            sdl.GLSetAttribute(GLattr.Doublebuffer, 1);
            sdl.GLSetAttribute(GLattr.ContextMajorVersion, api.Version.MajorVersion);
            sdl.GLSetAttribute(GLattr.ContextMinorVersion, api.Version.MinorVersion);
            sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)GLprofile.ES);

            window = Silk.NET.Windowing.Window.Create(options);
            window.Initialize();

            gl = GL.GetApi(window);

            using var grGlInterface = GRGlInterface.Create(name => (nint)sdl.GLGetProcAddress(name));
            grGlInterface.Validate();
            using var grContext = GRContext.CreateGl(grGlInterface);

            void RecreateSurface(int width, int height)
            {
                surface?.Dispose();
                renderTarget?.Dispose();

                renderTarget = new GRBackendRenderTarget(width, height, 0, 8, new GRGlFramebufferInfo(0, 0x8058));
                surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
                canvas = surface.Canvas;

                if (GumUI.IsInitialized)
                {
                    GumUI.SystemManagers.Canvas = canvas;
                }
            }

            RecreateSurface(windowWidth, windowHeight);

            var inputContext = window.CreateInput();

            InitializeGum(canvas, inputContext);

            gl.Viewport(0, 0, (uint)windowWidth, (uint)windowHeight);

            Vector2D<int>? pendingResize = null;
            window.Closing += () => running = false;
            window.Resize += newSize => pendingResize = newSize;

            Stopwatch totalTime = new Stopwatch();
            totalTime.Start();

            while (running && !window.IsClosing)
            {
                window.DoEvents();

                if (pendingResize.HasValue)
                {
                    var newSize = pendingResize.Value;
                    pendingResize = null;

                    gl.Viewport(0, 0, (uint)newSize.X, (uint)newSize.Y);
                    RecreateSurface(newSize.X, newSize.Y);
                    GumUI.HandleResize(newSize.X, newSize.Y);
                }

                GumUI.Update(totalTime.Elapsed.TotalSeconds);

                gl.ClearColor(clearColor.Red / 255f, clearColor.Green / 255f, clearColor.Blue / 255f, 1.0f);
                gl.Clear((uint)GLEnum.ColorBufferBit);

                grContext.ResetContext();

                Draw();
                canvas.Flush();

                window.GLContext!.SwapBuffers();
            }
        }
        finally
        {
            canvas?.Dispose();
            surface?.Dispose();
            renderTarget?.Dispose();
            window?.Dispose();
            sdl?.Quit();
        }
    }
}
