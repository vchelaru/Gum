using Silk.NET.OpenGLES;
using Silk.NET.SDL;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using Avalonia.Skia;
using SkiaSharp;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RenderingLibrary;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Gum;
using SilkNetGum.Screens;
using GumSamples.Screens;
using Gum.Managers;
using GumRuntime;

unsafe class Program
{
    #region Enums
    enum RenderBackend
    {
        Dx11,
        DesktopGl,
        Vulkan,
        Gles,
        Metal
    }

    #endregion

    #region Fields/Properties

    private static Sdl sdl;
    private static GL gl;
    private static IWindow window = null!;
    private static bool running = true;

    static GraphicalUiElement? currentGumxScreen;
    static FrameworkElement? currentCodeScreen;
    static int currentScreenIndex;
    static StackPanel navStrip = null!;
    static SKPaint sKPaint;
    static SKCanvas canvas;
    static SKPaint paintFromFile;

    static int windowWidth = 1400;
    static int windowHeight = 900;

    static GumService GumUI => GumService.Default;

    #endregion

    // Code-only screens appended after every screen from the gumx, allowing the
    // sample to exercise runtime features (like the unified NineSliceRuntime) that
    // are not yet authored as Gum screens.
    private static readonly Func<FrameworkElement>[] codeScreenFactories =
    {
        () => new SilkNetGum.Screens.NineSliceScreen(),
        () => new SilkNetGum.Screens.SpriteScreen(),
        () => new SilkNetGum.Screens.TextScreen(),
        () => new SilkNetGum.Screens.CirclesScreen(),
        () => new SilkNetGum.Screens.RectanglesScreen(),
        () => new SilkNetGum.Screens.ArcsScreen(),
        () => new SilkNetGum.Screens.PolygonsScreen(),
        () => new GumSamples.Screens.FormsScreen(),
    };

    // Parallel to codeScreenFactories, used as nav-strip button labels.
    private static readonly string[] codeScreenNames =
    {
        "NineSlice",
        "Sprite",
        "Text",
        "Circles",
        "Rectangles",
        "Arcs",
        "Polygons",
        "Forms",
    };

    private static void InitializeGum(SKCanvas canvas, IInputContext inputContext)
    {
        GumUI.Initialize(canvas, inputContext, "Content/GumProject/GumProject.gumx");

        // Registers GumUI.Keyboard for Tab / Shift+Tab focus traversal between Forms controls.
        GumUI.UseKeyboardDefaults();

        GraphicalUiElement.CanvasWidth = windowWidth;
        GraphicalUiElement.CanvasHeight = windowHeight;

        BuildNavStrip();

        LoadScreen(0);
    }

    // Mirrors MonoGameGumInCode's Game1.BuildNavStrip -- a horizontal strip of buttons, one per
    // screen, pinned to the top-left. Unlike MonoGameGumInCode's generic ShowScreen<T>, this sample
    // mixes two screen kinds (.gumx-authored GraphicalUiElement screens and code-only
    // FrameworkElement screens), so each button just calls LoadScreen(index) with its own captured
    // index instead of a generic factory.
    private static void BuildNavStrip()
    {
        navStrip = new StackPanel();
        navStrip.Orientation = Orientation.Horizontal;
        navStrip.Spacing = 4;
        navStrip.Visual.X = 4;
        navStrip.Visual.Y = 4;
        navStrip.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        navStrip.Width = 0;
        navStrip.Visual.WrapsChildren = true;
        navStrip.AddToRoot();

        var gumxScreens = ObjectFinder.Self.GumProjectSave!.Screens;

        for (int i = 0; i < gumxScreens.Count; i++)
        {
            int capturedIndex = i;
            AddNavButton(gumxScreens[i].Name, () => LoadScreen(capturedIndex));
        }

        for (int i = 0; i < codeScreenFactories.Length; i++)
        {
            int capturedIndex = gumxScreens.Count + i;
            AddNavButton(codeScreenNames[i], () => LoadScreen(capturedIndex));
        }
    }

    private static void AddNavButton(string text, Action onClick)
    {
        var button = new Gum.Forms.Controls.Button();
        button.Text = text;
        button.Click += (_, _) => onClick();
        navStrip.AddChild(button);
    }

    private static void LoadScreen(int index)
    {
        var gumxScreens = ObjectFinder.Self.GumProjectSave!.Screens;
        int total = gumxScreens.Count + codeScreenFactories.Length;
        if (total == 0) return;

        currentScreenIndex = ((index % total) + total) % total;

        // Attach via AddToRoot (children of GumService.Default.Root) instead of
        // AddToManagers — that way GumService.Default.Update walks the screen and
        // its descendants, which is what advances AnimationChain playback on the
        // SpriteScreen's animated bear row.
        currentGumxScreen?.RemoveFromRoot();
        currentGumxScreen = null;
        currentCodeScreen?.RemoveFromRoot();
        currentCodeScreen = null;

        if (currentScreenIndex < gumxScreens.Count)
        {
            currentGumxScreen = gumxScreens[currentScreenIndex].ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
            currentGumxScreen.AddToRoot();
            currentGumxScreen.YOrigin = VerticalAlignment.Top;
            currentGumxScreen.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            ResizeCurrentGumxScreen();
        }
        else
        {
            // Code screens are FrameworkElement and Dock(Fill) themselves, so their visual
            // fills the canvas without an explicit size assignment here -- only the top-offset
            // and height-shrink need to be applied on top of that.
            currentCodeScreen = codeScreenFactories[currentScreenIndex - gumxScreens.Count]();
            currentCodeScreen.Visual.YOrigin = VerticalAlignment.Top;
            currentCodeScreen.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            currentCodeScreen.Visual.Y = navStrip.Visual.GetAbsoluteHeight();
            currentCodeScreen.Visual.Height = -navStrip.Visual.GetAbsoluteHeight();
            currentCodeScreen.AddToRoot();
        }
    }

    // The loaded .gumx screen's Width/Height are plain pixel values (not RelativeToParent), so they
    // don't track GraphicalUiElement.CanvasWidth/Height automatically -- unlike code screens, which
    // Dock(Fill) themselves via relative units. Without re-applying this on every resize (not just
    // the initial LoadScreen), the screen keeps its original size while the root re-lays-out against
    // the new canvas size, shifting canvas-edge-anchored elements (e.g. bottom-docked) out of place
    // (#3657).
    private static void ResizeCurrentGumxScreen()
    {
        if (currentGumxScreen == null) return;

        float navStripHeight = navStrip.Visual.GetAbsoluteHeight();
        currentGumxScreen.Width = GraphicalUiElement.CanvasWidth;
        currentGumxScreen.Height = GraphicalUiElement.CanvasHeight - navStripHeight;
        currentGumxScreen.Y = navStripHeight;
    }

    private static void Draw()
    {

        GumUI.Draw();

        canvas.Flush();
    }

    #region General Setup/Functions

    static unsafe void Main(string[] args)
    {

        //vulkan by default uses dedicated gpu, thus why the default on windows
        RenderBackend renderBackend = RenderBackend.Dx11;

        if (OperatingSystem.IsMacOS())
        {
            Debug.WriteLine("Running on macOS...");
            renderBackend = RenderBackend.Metal;
        }

        if (OperatingSystem.IsIOS())
        {
            Debug.WriteLine("Running on ios...");
            renderBackend = RenderBackend.Metal;
        }

        if (OperatingSystem.IsLinux())
        {
            Console.WriteLine("Running on linux...");
            renderBackend = RenderBackend.Vulkan;
        }

        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("Running on windows...");
            renderBackend = RenderBackend.Dx11;
        }

        if (OperatingSystem.IsAndroid())
        {
            Debug.WriteLine("Running on android...");
            renderBackend = RenderBackend.Gles;
        }


        if (renderBackend == RenderBackend.DesktopGl)
            Environment.SetEnvironmentVariable("ANGLE_DEFAULT_PLATFORM", "gl");
        if (renderBackend == RenderBackend.Dx11)
            Environment.SetEnvironmentVariable("ANGLE_DEFAULT_PLATFORM", "d3d11");
        if (renderBackend == RenderBackend.Vulkan)
            Environment.SetEnvironmentVariable("ANGLE_DEFAULT_PLATFORM", "vulkan");
        if (renderBackend == RenderBackend.Metal)
            Environment.SetEnvironmentVariable("ANGLE_DEFAULT_PLATFORM", "metal");




        // sdl is grabbed via SdlProvider so that the SDL_SetHint calls below apply to the same
        // Sdl instance that Silk.NET.Windowing.Sdl will reuse internally when it creates the
        // window (SdlView pulls Sdl from SdlProvider.SDL too). Accessing SdlProvider.SDL.Value
        // triggers SDL_Init.
        sdl = Silk.NET.SDL.SdlProvider.SDL.Value;

        try
        {
            // ANGLE requires these hints to be set before the GL context is created (below, via
            // window.Initialize()). Hints are global SDL state, so setting them here -- ahead of
            // handing window/context creation to Silk.NET.Windowing -- still works.
            if (renderBackend != RenderBackend.Gles)
            {
                sdl.SetHint("SDL_OPENGL_ES_DRIVER", "1");
                sdl.SetHint("SDL_HINT_OPENGL_ES_DRIVER", "angle");
            }

            // Context API/version/profile per backend, mirroring the GLattr calls this used to
            // make by hand before window creation.
            GraphicsAPI api = renderBackend switch
            {
                RenderBackend.DesktopGl => new GraphicsAPI(
                    ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3)),
                RenderBackend.Dx11 => new GraphicsAPI(
                    ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 0)),
                // vulkan and opengl es 3.2
                _ => new GraphicsAPI(
                    ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 2)),
            };

            // Silk.NET.Windowing.Sdl must create and own the window (going through the normal
            // Initialize() path) for Silk.NET.Input.Sdl to ever receive SDL events -- wrapping an
            // externally-created window via SdlWindowing.CreateFrom skips RegisterCallbacks(),
            // which is what subscribes the view to the platform's event pump, so input (clicks,
            // typing) never arrives no matter what's polled afterward (#3652).
            SdlWindowing.Use();

            var options = WindowOptions.Default;
            options.API = api;
            options.Size = new Vector2D<int>(windowWidth, windowHeight);
            options.Title = "SDL ANGLE Example";
            options.WindowState = WindowState.Normal;
            options.WindowBorder = WindowBorder.Resizable;
            options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8);
            options.PreferredDepthBufferBits = 24;
            options.PreferredStencilBufferBits = 8;
            options.VSync = false;

            // SdlView.CoreInitialize applies PreferredBitDepth/DepthBufferBits/StencilBufferBits as
            // GLattr calls automatically, but never touches Doublebuffer -- the original manual
            // sdl.GLCreateContext setup explicitly set this before context creation. Setting it here
            // (attributes are global SDL state, applied before Window.Create below) restores parity.
            sdl.GLSetAttribute(GLattr.Doublebuffer, 1);

            // SdlView.CoreInitialize creates the SDL window (Sdl.CreateWindow) BEFORE setting
            // ContextMajorVersion/MinorVersion/ContextProfileMask -- those are only set later,
            // immediately before Sdl.GLCreateContext. For ANGLE, that's too late: its EGL surface
            // binds its format at window-creation time, so the context ends up non-functional
            // (glGetString and friends silently return null/empty, and GRGlInterface.Create fails)
            // unless these are also set here, before Window.Create (#3652).
            sdl.GLSetAttribute(GLattr.ContextMajorVersion, api.Version.MajorVersion);
            sdl.GLSetAttribute(GLattr.ContextMinorVersion, api.Version.MinorVersion);
            sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)(api.API == ContextAPI.OpenGLES ? GLprofile.ES : GLprofile.Core));

            Console.WriteLine("Creating window...");
            window = Silk.NET.Windowing.Window.Create(options);
            window.Initialize();

            Console.WriteLine("Getting GL API...");
            gl = GL.GetApi(window);

            gl.Enable(EnableCap.DebugOutput);
            gl.Enable(EnableCap.DebugOutputSynchronous);

            // Set up debug callback
            gl.DebugMessageCallback((source, type, id, severity, length, message, param) =>
            {
                string messageString = Marshal.PtrToStringAnsi(message, length);
                //Console.WriteLine($"GL Debug: {severity}: {messageString}");
            }, in IntPtr.Zero);

            // Print renderer info
            unsafe
            {
                byte* renderer = (byte*)gl.GetString(GLEnum.Renderer);
                byte* version = (byte*)gl.GetString(GLEnum.Version);
                Console.WriteLine($"Renderer: {Marshal.PtrToStringUTF8((IntPtr)renderer)}");
                Console.WriteLine($"Version: {Marshal.PtrToStringUTF8((IntPtr)version)}");
            }

            using var grGlInterface = GRGlInterface.Create(name => (nint)sdl.GLGetProcAddress(name));
            grGlInterface.Validate();
            using var grContext = GRContext.CreateGl(grGlInterface);
            var renderTarget = new GRBackendRenderTarget(windowWidth, windowHeight, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
            using var surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
            canvas = surface.Canvas;

            // Now that Silk.NET.Windowing.Sdl created and initialized the window itself,
            // CreateInput builds a real IInputContext whose events are actually pumped (#3652).
            var inputContext = window.CreateInput();

            InitializeGum(canvas, inputContext);

            gl.Viewport(0, 0, 600, 600);

            window.Closing += () => running = false;
            window.Resize += newSize =>
            {
                gl.Viewport(0, 0, (uint)newSize.X, (uint)newSize.Y);
                GumUI.HandleResize(newSize.X, newSize.Y);
                ResizeCurrentGumxScreen();
            };

            if (inputContext.Keyboards.Count > 0)
            {
                var keyboard = inputContext.Keyboards[0];
                keyboard.KeyDown += (_, key, _) =>
                {
                    if (key == Key.Escape)
                        running = false;
                };
            }

            // Main loop

            sKPaint = new SKPaint()
            {
                IsAntialias = true,
                Color = SKColors.DarkRed,
            };

            int frames = 0;

            using var fontFromFile = SKTypeface.FromFile("Super Morning.ttf");
            paintFromFile = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                TextSize = 32,
                Typeface = fontFromFile
            };
            SKPath batchPath = new SKPath();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            // GumService.Update wants total elapsed seconds since startup (on
            // non-XNALIKE backends the engine computes the per-frame delta as
            // `currentTotal - previousTotal`). `sw` above is reset every second
            // for FPS reporting, so we keep a separate monotonic clock here.
            Stopwatch totalTime = new Stopwatch();
            totalTime.Start();
            while (running && !window.IsClosing)
            {

                if (sw.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(frames);
                    sw.Restart();

                    frames = 0;
                }
                frames++;

                // Pumps SDL events into the window's event list, invokes ProcessEvents (which
                // drives the input context -- mouse/keyboard state), then clears the list. This
                // is what makes clicks/typing actually reach the Forms controls (#3652).
                window.DoEvents();

                // Per-frame Update drives AnimateSelf (and any other Forms
                // input/activity pumps). Without this the .achx animation row
                // on SpriteScreen shows the first frame and never advances.
                GumUI.Update(totalTime.Elapsed.TotalSeconds);



                // Render. Matches MonoGameGumInCode's GraphicsDevice.Clear(Color.CornflowerBlue)
                // (RGB 100, 149, 237) so the two samples are visually comparable.
                gl.ClearColor(100f / 255f, 149f / 255f, 237f / 255f, 1.0f);
                gl.Clear((uint)GLEnum.ColorBufferBit);


                grContext.ResetContext();
                // canvas.Clear(SKColors.Cyan);
                //_renderer.Render(canvas);


                Draw();

                // Swap buffers
                window.GLContext!.SwapBuffers();
            }
        }
        finally
        {
            paintFromFile?.Dispose();
            canvas?.Dispose();
            window?.Dispose();
            sdl?.Quit();
        }
    }

    #endregion
}