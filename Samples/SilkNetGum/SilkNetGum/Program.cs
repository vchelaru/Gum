using Silk.NET.OpenGLES;
using Silk.NET.SDL;
using Silk.NET.Input;
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
    private static Window* window;
    private static void* glContext;
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

    private static string GetSdlError()
    {
        byte* error = sdl.GetError();
        return Marshal.PtrToStringUTF8((IntPtr)error) ?? "Unknown error";
    }
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




        // Initialize SDL
        sdl = Sdl.GetApi();
        if (sdl.Init(Sdl.InitVideo | Sdl.InitEvents) < 0)
        {
            Console.WriteLine($"SDL initialization failed: {GetSdlError()}");
            return;
        }

        try
        {
            // Set OpenGL ES attributes BEFORE loading libraries
            sdl.GLSetAttribute(GLattr.RedSize, 8);
            sdl.GLSetAttribute(GLattr.GreenSize, 8);
            sdl.GLSetAttribute(GLattr.BlueSize, 8);
            sdl.GLSetAttribute(GLattr.AlphaSize, 8);
            sdl.GLSetAttribute(GLattr.DepthSize, 24);
            sdl.GLSetAttribute(GLattr.StencilSize, 8);
            sdl.GLSetAttribute(GLattr.Doublebuffer, 1);



            if (renderBackend == RenderBackend.DesktopGl)
            {
                sdl.GLSetAttribute(GLattr.ContextMajorVersion, 3);
                sdl.GLSetAttribute(GLattr.ContextMinorVersion, 3); // to support dx11 as well
                sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)GLprofile.Core);
            }
            else if (renderBackend == RenderBackend.Dx11)
            {
                sdl.GLSetAttribute(GLattr.ContextMajorVersion, 3);
                sdl.GLSetAttribute(GLattr.ContextMinorVersion, 0); // to support dx11 as well
                sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)GLprofile.ES);
            }
            else // vulkan and opengl es 3.2
            {
                sdl.GLSetAttribute(GLattr.ContextMajorVersion, 3);
                sdl.GLSetAttribute(GLattr.ContextMinorVersion, 2); // to support dx11 as well
                sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)GLprofile.ES);
            }




            if (renderBackend != RenderBackend.Gles)
            {
                sdl.SetHint("SDL_OPENGL_ES_DRIVER", "1");
                sdl.SetHint("SDL_HINT_OPENGL_ES_DRIVER", "angle");
            }


            // Create window with appropriate flags
            uint windowFlags = (uint)(WindowFlags.Opengl | WindowFlags.Shown | WindowFlags.Resizable);



            Console.WriteLine("Creating window...");
            window = sdl.CreateWindow(
                "SDL ANGLE Example",
                Sdl.WindowposCentered,
                Sdl.WindowposCentered,
                windowWidth, windowHeight,
                windowFlags
            );

            if (window == null)
            {
                Console.WriteLine($"Window creation failed: {GetSdlError()}");
                throw new Exception("Window creation failed");
            }


            Console.WriteLine("Creating GL context...");
            glContext = sdl.GLCreateContext(window);
            if (glContext == null)
            {
                throw new Exception($"OpenGL context creation failed: {GetSdlError()}");
            }

            Console.WriteLine("Making context current...");
            if (sdl.GLMakeCurrent(window, glContext) < 0)
            {
                throw new Exception($"Failed to make context current: {GetSdlError()}");
            }

            // Enable vsync
            sdl.GLSetSwapInterval(0);

            Console.WriteLine("Getting GL API...");
            gl = GL.GetApi(loadFunction);




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




            using var grGlInterface = GRGlInterface.Create(loadFunction);
            grGlInterface.Validate();
            using var grContext = GRContext.CreateGl(grGlInterface);
            var renderTarget = new GRBackendRenderTarget(windowWidth, windowHeight, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
            using var surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
            canvas = surface.Canvas;

            // Wrap the raw SDL window handle (created above via Silk.NET.SDL P/Invoke) in an IView so
            // Silk.NET.Input.Sdl can build a real IInputContext from it. This does NOT hand window or
            // GL-context ownership to Silk.NET.Windowing -- CreateFrom only wraps the existing handle
            // for input purposes; the ANGLE/GLES/D3D11 setup above is untouched (#3652).
            SdlWindowing.RegisterPlatform();
            var view = SdlWindowing.CreateFrom(window);
            var inputContext = view.CreateInput();

            InitializeGum(canvas, inputContext);

            gl.Viewport(0, 0, 600, 600);

            Event ev = new Event();
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
            while (running)
            {

                if (sw.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(frames);
                    sw.Restart();

                    frames = 0;
                }
                frames++;
                while (sdl.PollEvent(&ev) != 0)
                {

                    switch (ev.Type)
                    {

                        case (uint)EventType.Dropfile:
                            Console.WriteLine(Marshal.PtrToStringUTF8((IntPtr)ev.Drop.File));
                            break;
                        case (uint)EventType.Fingermotion:
                            ev.Tfinger.X = 0;
                            break;
                        case (uint)EventType.Quit:
                            running = false;
                            break;
                        case (uint)EventType.Keydown:
                            if (ev.Key.Keysym.Sym == (int)KeyCode.KEscape)
                                running = false;
                            else if (ev.Key.Keysym.Sym == (int)KeyCode.KLeft)
                                LoadScreen(currentScreenIndex - 1);
                            else if (ev.Key.Keysym.Sym == (int)KeyCode.KRight)
                                LoadScreen(currentScreenIndex + 1);
                            break;
                        case (uint)EventType.Windowevent:
                            if (ev.Window.Event == (byte)WindowEventID.Resized)
                            {
                                gl.Viewport(0, 0, (uint)ev.Window.Data1, (uint)ev.Window.Data2);
                                GumUI.HandleResize(ev.Window.Data1, ev.Window.Data2);
                            }
                            break;
                    }
                }


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
                sdl.GLSwapWindow(window);
            }
        }
        finally
        {
            paintFromFile.Dispose();
            canvas.Dispose();
            // Cleanup
            if (glContext != null)
                sdl.GLDeleteContext(glContext);
            if (window != null)
                sdl.DestroyWindow(window);
            sdl.Quit();
        }
    }

    private static nint loadFunction(string name)
    {
        return (nint)sdl.GLGetProcAddress(name);
    }

    #endregion
}