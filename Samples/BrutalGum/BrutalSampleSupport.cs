// Plumbing both BRUTAL samples (OpenGL and Vulkan) share, so each Program.cs is only window setup plus
// GumService.Initialize / Update / Draw. Nothing here is Gum API a game needs.
using Brutal.GlfwApi;
using Gum;
using Gum.Forms.Controls;
using GumSamples;
using SkiaSharp;

/// <summary>
/// Command line for runs with nobody watching: <c>--hidden</c> keeps the window invisible, <c>--exit-after
/// &lt;seconds&gt;</c> closes it, <c>--screen &lt;n&gt;</c> starts on gallery screen n (1 based),
/// <c>--offscreen</c> shows the window far off the desktop without taking focus (so the OS still composes it for a window capture), <c>--resize &lt;w&gt;x&lt;h&gt;</c> resizes the window once after half the run, and <c>--capture &lt;png&gt;</c> (OpenGL sample)
/// saves the window's pixels before exiting.
/// </summary>
internal sealed class SampleArgs
{
    public bool Hidden { get; }
    public bool Offscreen { get; }
    public double ExitAfterSeconds { get; }
    public int Screen { get; }
    public (int Width, int Height)? Resize { get; }
    public string? CapturePath { get; }

    public SampleArgs(string[] args)
    {
        Hidden = args.Contains("--hidden");
        Offscreen = args.Contains("--offscreen");
        ExitAfterSeconds = double.Parse(Value(args, "--exit-after") ?? "0");
        Screen = int.Parse(Value(args, "--screen") ?? "1") - 1;
        CapturePath = Value(args, "--capture");
        string? resize = Value(args, "--resize");
        if (resize != null)
        {
            string[] parts = resize.Split('x');
            Resize = (int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }

    private static string? Value(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}

internal static class SampleSupport
{
    /// <summary>
    /// Sets up the shared gallery (the same screens SilkNetGumSample shows) after GumService.Initialize. Click the
    /// buttons along the top, or press 1..9, to switch screens.
    /// </summary>
    /// <summary>The window hints every sample shares: resizable, and visible unless a run is --hidden.</summary>
    public static void ApplyWindowHints(SampleArgs args)
    {
        Glfw.WindowHint(GlfwWindowHint.Resizable, Glfw.Constants.GLFW_TRUE);
        Glfw.WindowHint(GlfwWindowHint.Visible, args.Hidden ? Glfw.Constants.GLFW_FALSE : Glfw.Constants.GLFW_TRUE);
        if (args.Offscreen)
        {
            Glfw.WindowHint(GlfwWindowHint.Focused, Glfw.Constants.GLFW_FALSE);
            Glfw.WindowHint(GlfwWindowHint.FocusOnShow, Glfw.Constants.GLFW_FALSE);
        }
    }

    public static void PlaceWindow(GlfwWindow window, SampleArgs args)
    {
        if (args.Offscreen)
        {
            window.Position = new Brutal.Numerics.int2(-20000, -20000);
        }
    }

    public static GumGallery CreateGallery(GumService GumUI, GlfwWindow window, SampleArgs args)
    {
        // Turns an RT Shader screen .sksl path into a compiled effect, then lets .fx and .slang files work too.
        SkiaGum.CustomSetPropertyOnRenderable.RenderTargetEffectResolver = SilkNetGum.Screens.RenderTargetShaderScreen.CompileEffectFromFile;
        GumUI.UseShadowDusk();
        GumUI.UseKeyboardDefaults();
        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.AddRange(GumUI.Gamepads);
        GumUI.ClearColor = new SKColor(32, 32, 40);

        GumGallery gallery = new GumGallery();
        gallery.Build();
        gallery.LoadScreen(Math.Clamp(args.Screen, 0, gallery.ScreenCount - 1));

        window.OnKey += (_, key, _, action, _) =>
        {
            if (action == GlfwKeyAction.Press && key >= GlfwKey.Number1 && key < GlfwKey.Number1 + Math.Min(9, gallery.ScreenCount))
            {
                gallery.LoadScreen(key - GlfwKey.Number1);
            }
        };
        return gallery;
    }

    public static void SavePng(byte[] rgba, int width, int height, string path)
    {
        using SKBitmap bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        System.Runtime.InteropServices.Marshal.Copy(rgba, 0, bitmap.GetPixels(), rgba.Length);
        using SKData png = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, png.ToArray());
        Console.WriteLine(path);
    }
}
