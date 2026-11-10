// A gallery of Gum screens (the same ones SilkNetGumSample shows) drawn by Gum.Brutal.OGL on OpenGL 3.3.
// Gum owns Skia and the render target; the game only creates the window and calls Initialize, Update, Draw.
//
//   BrutalGumSampleOgl                                   opens a resizable window
//   BrutalGumSampleOgl --hidden --exit-after 5           same, invisible, closes after 5 seconds
//   add --capture <png>      saves the window's pixels (glReadPixels on the default framebuffer) before exiting
//   add --resize 900x500     resizes the window once, halfway through the run
//   BrutalGumSampleOgl --hidden --batch-stats <txt>       appends each screen's same-image draw runs, then exits (#7826)
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
    Glfw.SwapInterval(1);

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

    // --batch-stats <file> (#7826): draws every gallery screen twice, appends its same-image draw runs, then exits.
    string? batchStatsPath = args.SkipWhile(arg => arg != "--batch-stats").Skip(1).FirstOrDefault();
    if (batchStatsPath != null)
    {
        for (int i = 0; i < gallery.ScreenCount; i++)
        {
            gallery.LoadScreen(i);
            for (int frame = 0; frame < 2; frame++)
            {
                GumUI.Update(frame / 60.0);
                GumUI.Draw();
                window.SwapBuffers();
            }
            BatchStats.Append(batchStatsPath, gallery.GetScreenName(i));
        }
        return;
    }

    Stopwatch clock = Stopwatch.StartNew();
    double previousSeconds = 0;
    bool resized = false;
    while (!window.ShouldClose)
    {
        Glfw.PollEvents();
        double gameTime = clock.Elapsed.TotalSeconds;
        GumUI.Update(gameTime);
        gallery.Tick(gameTime - previousSeconds);
        previousSeconds = gameTime;
        GlClear.ToColor(GetProc, 100 / 255f, 149 / 255f, 237 / 255f); // the game's own background; Gum draws over it
        GumUI.Draw();

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

        window.SwapBuffers();
    }
}
finally
{
    Glfw.Terminate();
}

static IntPtr GetProc(string name) => Glfw.GetProcAddress(Encoding.ASCII.GetBytes(name + "\0"));
