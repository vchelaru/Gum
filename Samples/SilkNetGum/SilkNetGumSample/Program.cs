using Silk.NET.OpenGLES;
using Silk.NET.SDL;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using System.Diagnostics;
using Gum;
using Gum.Forms.Input;
using GumSamples;

unsafe class Program
{
    enum RenderBackend
    {
        Dx11,
        DesktopGl,
        Vulkan,
        Gles,
        Metal
    }

    static readonly GumGallery gallery = new GumGallery();

    static GumService GumUI => GumService.Default;

    static void Main(string[] args)
    {
        // Vulkan by default uses the dedicated GPU, which is why Windows defaults to D3D11.
        RenderBackend renderBackend = RenderBackend.Dx11;

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
        {
            renderBackend = RenderBackend.Metal;
        }
        else if (OperatingSystem.IsLinux())
        {
            renderBackend = RenderBackend.Vulkan;
        }
        else if (OperatingSystem.IsAndroid())
        {
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
        Sdl sdl = SdlProvider.SDL.Value;

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

            // Context API/version/profile per backend.
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

            WindowOptions options = WindowOptions.Default;
            options.API = api;
            options.Size = new Vector2D<int>(1400, 900);
            options.Title = "SDL ANGLE Example";
            options.WindowState = WindowState.Normal;
            options.WindowBorder = WindowBorder.Resizable;
            options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8);
            options.PreferredDepthBufferBits = 24;
            options.PreferredStencilBufferBits = 8;
            options.VSync = false;
            // Lets a verification script run the sample with nobody watching: GUM_SAMPLE_HIDDEN=1.
            options.IsVisible = Environment.GetEnvironmentVariable("GUM_SAMPLE_HIDDEN") != "1";

            // SdlView.CoreInitialize applies PreferredBitDepth/DepthBufferBits/StencilBufferBits as
            // GLattr calls automatically, but never touches Doublebuffer.
            sdl.GLSetAttribute(GLattr.Doublebuffer, 1);

            // SdlView.CoreInitialize creates the SDL window BEFORE setting the context version and
            // profile. For ANGLE that is too late: its EGL surface binds its format at window-creation
            // time, so the context ends up non-functional (glGetString and friends silently return
            // null/empty, and the Skia GL interface fails) unless these are also set here (#3652).
            sdl.GLSetAttribute(GLattr.ContextMajorVersion, api.Version.MajorVersion);
            sdl.GLSetAttribute(GLattr.ContextMinorVersion, api.Version.MinorVersion);
            sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)(api.API == ContextAPI.OpenGLES ? GLprofile.ES : GLprofile.Core));

            IWindow window = Silk.NET.Windowing.Window.Create(options);
            window.Initialize();

            GL gl = GL.GetApi(window);

            GumUI.Initialize(window, "Content/GumProject/GumProject.gumx");

            // Issue #3998: Gum core ships no shader loader, so the app registers a resolver that turns a
            // ContainerRuntime.SourceShaderFile (.sksl path) into a compiled SKRuntimeEffect. With no
            // resolver registered, a SourceShaderFile fails like a missing file.
            SkiaGum.CustomSetPropertyOnRenderable.RenderTargetEffectResolver =
                SilkNetGum.Screens.RenderTargetShaderScreen.CompileEffectFromFile;

            // Adds .fx and .slang files to SourceShaderFile (ShadowDusk converts them to SkSL); the .sksl
            // resolver above keeps handling .sksl files.
            GumUI.UseShadowDusk();

            // Registers GumUI.Keyboard for Tab / Shift+Tab focus traversal between Forms controls.
            GumUI.UseKeyboardDefaults();
            GumUI.UseGamepadDefaults();

            gallery.Build();

            // GumService.Update wants total elapsed seconds since startup.
            Stopwatch totalTime = Stopwatch.StartNew();
            double previousTotalSeconds = 0;
            // options.VSync = false (above) leaves this loop otherwise uncapped, which let a feedback
            // bug in an earlier cut of the Zoom sample screen (issue #4330) compound far faster here
            // than on MonoGame's fixed ~60fps timestep. Capping to 60fps keeps per-frame-scaled logic
            // comparable across samples.
            const double targetFrameSeconds = 1.0 / 60.0;
            Stopwatch frameTimer = new Stopwatch();
            while (!window.IsClosing)
            {
                frameTimer.Restart();

                // Pumps SDL events into the input context so clicks and typing reach the Forms controls (#3652).
                window.DoEvents();

                if (GumUI.Keyboard.KeyPushed(Keys.Escape))
                {
                    break;
                }

                double gameTime = totalTime.Elapsed.TotalSeconds;
                GumUI.Update(gameTime);

                gallery.Tick(gameTime - previousTotalSeconds);
                previousTotalSeconds = gameTime;

                // Matches MonoGameGumInCode's GraphicsDevice.Clear(Color.CornflowerBlue) (RGB 100, 149, 237).
                gl.ClearColor(100f / 255f, 149f / 255f, 237f / 255f, 1.0f);
                gl.Clear((uint)GLEnum.ColorBufferBit);

                GumUI.Draw();

                window.GLContext!.SwapBuffers();

                double remainingSeconds = targetFrameSeconds - frameTimer.Elapsed.TotalSeconds;
                if (remainingSeconds > 0)
                {
                    System.Threading.Thread.Sleep((int)(remainingSeconds * 1000));
                }
            }

            window.Dispose();
        }
        finally
        {
            sdl.Quit();
        }
    }
}
