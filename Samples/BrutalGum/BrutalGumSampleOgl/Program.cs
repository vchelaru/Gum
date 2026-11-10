// A gallery of Gum screens (the same ones SilkNetGumSample shows) drawn by Gum.Brutal.OGL on OpenGL 3.3.
// Gum owns Skia and the render target; the game only creates the window and calls Initialize, Update, Draw.
//
//   BrutalGumSampleOgl                                   opens a resizable window
//   BrutalGumSampleOgl --hidden --exit-after 5           same, invisible, closes after 5 seconds
//   add --capture <png>      saves the window's pixels (glReadPixels on the default framebuffer) before exiting
//   add --resize 900x500     resizes the window once, halfway through the run
using Brutal.GlfwApi;
using Brutal.Numerics;
using Gum;
using GumSamples;
using System.Diagnostics;
using System.Text;

SampleArgs sample = new SampleArgs(args);

// The Gum project loads relative to the working directory, so run from the build output whatever the caller's folder is.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

Glfw.Init();
try
{
    Glfw.WindowHint(GlfwWindowHint.ContextVersionMajor, 3);
    Glfw.WindowHint(GlfwWindowHint.ContextVersionMinor, 3);
    Glfw.WindowHint(GlfwWindowHint.OpenGlProfile, 0x32001 /* GLFW_OPENGL_CORE_PROFILE */);
    SampleSupport.ApplyWindowHints(sample);
    using GlfwWindow window = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = "Gum.Brutal.OGL sample", Size = new int2(1400, 900) });
    SampleSupport.PlaceWindow(window, sample);
    window.MakeContextCurrent();
    FrameTiming timing = new FrameTiming(args, GetProc);
    Glfw.SwapInterval(timing.SwapInterval);

    GumService GumUI = GumService.Default;

    GumUI.Initialize(window, "Content/GumProject/GumProject.gumx");
    SkiaGum.CustomSetPropertyOnRenderable.RenderTargetEffectResolver =
        SilkNetGum.Screens.RenderTargetShaderScreen.CompileEffectFromFile;
    GumUI.UseShadowDusk();
    GumUI.UseKeyboardDefaults();
    GumUI.UseGamepadDefaults();

    // Demo the auto-fit helpers. Flip via the Zoom/Expand radios in the nav strip.
    GumUI.EnableZoomToWindow();

    GumGallery gallery = new GumGallery();
    gallery.Build();
    gallery.LoadScreen(Math.Clamp(sample.Screen, 0, gallery.ScreenCount - 1)); // --screen, for unattended runs

    Stopwatch clock = Stopwatch.StartNew();
    double previousSeconds = 0;
    bool resized = false;
    while (!window.ShouldClose)
    {
        timing.BeginFrame();
        Glfw.PollEvents();
        double gameTime = clock.Elapsed.TotalSeconds;
        timing.Time(0, () =>
        {
            GumUI.Update(gameTime);
            gallery.Tick(gameTime - previousSeconds);
        });
        previousSeconds = gameTime;
        // the game's own background; Gum draws over it
        timing.Time(5, () => GlClear.ToColor(GetProc, 100 / 255f, 149 / 255f, 237 / 255f));
        timing.Time(1, GumUI.Draw);
        timing.Finish();

        if (sample.ExitAfterSeconds > 0 && gameTime >= sample.ExitAfterSeconds)
        {
            if (sample.CapturePath != null)
            {
                SampleSupport.SavePng(GlReadback.ReadDefaultFramebuffer(GetProc, window.FramebufferSize.X, window.FramebufferSize.Y),
                    window.FramebufferSize.X, window.FramebufferSize.Y, sample.CapturePath);
            }
            window.ShouldClose = true;
        }
        else if (sample.Resize is { } size && !resized && sample.ExitAfterSeconds > 0 && gameTime >= sample.ExitAfterSeconds / 2)
        {
            window.Size = new int2(size.Width, size.Height);
            resized = true;
        }

        timing.Time(3, window.SwapBuffers);
        timing.EndFrame();
    }
    timing.Write($"screen={sample.Screen + 1} hidden={sample.Hidden} offscreen={sample.Offscreen} fb={window.FramebufferSize.X}x{window.FramebufferSize.Y}");
}
finally
{
    Glfw.Terminate();
}

static IntPtr GetProc(string name) => Glfw.GetProcAddress(Encoding.ASCII.GetBytes(name + "\0"));
