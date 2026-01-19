using Silk.NET.OpenGLES;
using Silk.NET.SDL;
using Avalonia.Skia;
using SkiaSharp;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RenderingLibrary;
using Gum.Wireframe;
using SkiaGum.GueDeriving;
using RenderingLibrary.Graphics;
using SkiaGum;
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

    static GraphicalUiElement Root;
    static SKPaint sKPaint;
    static SKCanvas canvas;
    static SKPaint paintFromFile;

    #endregion

    private static void InitializeGum(SKCanvas canvas)
    {
        GumService.Default.Initialize(canvas, "Content/GumProject/GumProject.gumx");

        //Root = new CodeOnlyScreen();
        //Root.AddToManagers();

        var screen = ObjectFinder.Self.GumProjectSave!.Screens.First();
        Root = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
    }

    private static void Draw()
    {
        GumService.Default.Draw();

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
                800, 600,
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
            var renderTarget = new GRBackendRenderTarget(800, 600, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
            using var surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
            canvas = surface.Canvas;

            InitializeGum(canvas);

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
                            break;
                        case (uint)EventType.Windowevent:
                            if (ev.Window.Event == (byte)WindowEventID.Resized)
                            {
                                gl.Viewport(0, 0, (uint)ev.Window.Data1, (uint)ev.Window.Data2);
                            }
                            break;
                    }
                }




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