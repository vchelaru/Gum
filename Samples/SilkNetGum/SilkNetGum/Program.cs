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
    static TextRuntime instructionsText;
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
        () => new SilkNetGum.Screens.FormsScreen(),
    };

    private static void InitializeGum(SKCanvas canvas, IInputContext inputContext)
    {
        GumUI.Initialize(canvas, inputContext, "Content/GumProject/GumProject.gumx");

        // Registers GumUI.Keyboard for Tab / Shift+Tab focus traversal between Forms controls.
        GumUI.UseKeyboardDefaults();

        LoadScreen(0);

        instructionsText = new TextRuntime();
        instructionsText.Text = "Press the left or right arrows to toggle between the screens";
        instructionsText.X = 0;
        instructionsText.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        instructionsText.XOrigin = HorizontalAlignment.Center;
        instructionsText.Y = -10;
        instructionsText.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        instructionsText.YOrigin = VerticalAlignment.Bottom;
        instructionsText.AddToRoot();

        GraphicalUiElement.CanvasWidth = windowWidth;
        GraphicalUiElement.CanvasHeight = windowHeight;
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
            currentGumxScreen.Width = GraphicalUiElement.CanvasWidth;
            currentGumxScreen.Height = GraphicalUiElement.CanvasHeight;
        }
        else
        {
            // Code screens are FrameworkElement and Dock(Fill) themselves, so their visual
            // fills the canvas without an explicit size assignment here.
            currentCodeScreen = codeScreenFactories[currentScreenIndex - gumxScreens.Count]();
            currentCodeScreen.AddToRoot();
        }
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

            // Deliberately NOT window.GLContext.GetProcAddress/TryGetProcAddress here: both funnel
            // through SdlContext's SDL_ClearError/SDL_GetError check, which treats ANY SDL error
            // string set during the lookup as failure -- even a stale/benign one unrelated to the
            // proc actually being missing -- so real, resolvable functions were coming back null
            // and GRGlInterface.Create() failed outright, NREing on the next line's Validate()
            // (#3652). Call the raw SDL proc-address lookup directly instead, exactly like the
            // original (working) sdl.GLGetProcAddress-based loadFunction did -- it only treats a
            // null pointer as "missing", which is all GRGlInterface.Create actually needs.
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
            };

            if (inputContext.Keyboards.Count > 0)
            {
                var keyboard = inputContext.Keyboards[0];
                keyboard.KeyDown += (_, key, _) =>
                {
                    if (key == Key.Escape)
                        running = false;
                    else if (key == Key.Left)
                        LoadScreen(currentScreenIndex - 1);
                    else if (key == Key.Right)
                        LoadScreen(currentScreenIndex + 1);
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



                // Render
                gl.ClearColor(0.2f, 0.3f, 0.8f, 1.0f);
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
            paintFromFile.Dispose();
            canvas.Dispose();
            window?.Dispose();
            sdl.Quit();
        }
    }

    #endregion
}