// Plumbing both BRUTAL samples (OpenGL and Vulkan) share: command line, window hints and the PNG capture.
// No Gum calls live here; every Gum call a game needs is in each Program.cs.
using Brutal.GlfwApi;
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

    /// <summary>
    /// OpenGL 3.3 core for the window's context. macOS creates no core context without GLFW_OPENGL_FORWARD_COMPAT,
    /// and then hands out the newest it has (4.1 core), which Gum.Brutal.OGL runs on.
    /// </summary>
    public static void ApplyOpenGlHints()
    {
        Glfw.WindowHint(GlfwWindowHint.ContextVersionMajor, 3);
        Glfw.WindowHint(GlfwWindowHint.ContextVersionMinor, 3);
        Glfw.WindowHint(GlfwWindowHint.OpenGlProfile, 0x32001 /* GLFW_OPENGL_CORE_PROFILE */);
        if (OperatingSystem.IsMacOS())
        {
            Glfw.WindowHint((GlfwWindowHint)0x22006 /* GLFW_OPENGL_FORWARD_COMPAT */, Glfw.Constants.GLFW_TRUE);
        }
    }

    public static void PlaceWindow(GlfwWindow window, SampleArgs args)
    {
        if (args.Offscreen)
        {
            window.Position = new Brutal.Numerics.int2(-20000, -20000);
        }
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
