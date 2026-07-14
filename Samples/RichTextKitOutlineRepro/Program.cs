// Clean-room repro (NO Gum) for the RichTextKit text-outline miter-spike bug.
//
// RichTextKit paints its halo (Style.HaloColor + Style.HaloWidth) as a *stroked* outline around the
// glyph run but never sets StrokeJoin on the paint (Topten.RichTextKit/FontRun.cs), so SkiaSharp
// defaults to SKStrokeJoin.Miter with miter-limit 4. At acute glyph vertices -- e.g. the bottom
// points of a 'W' -- the miter shoots outward into a spike, and the spike length grows with the
// stroke width. Style exposes only HaloColor/HaloWidth/HaloBlur, so there is no way to change the
// join through RichTextKit's API.
//
// Three lines are drawn, all white fill + black outline over a white background (so only the black
// outline is visible), so the spikes are obvious and the cause is isolated:
//   1) RichTextKit halo             -> spikes (the bug, as shipped)
//   2) manual SKFont stroke, Miter  -> same spikes (proves it is the join, not RichTextKit-specific)
//   3) manual SKFont stroke, Round  -> clean, no spikes (the proposed one-line fix in FontRun.cs)
//
// The Silk.NET + SDL/ANGLE + SkiaSharp GL bootstrap is copied from Samples/SilkNetGum/SilkNetGumSample.

using Silk.NET.OpenGLES;
using Silk.NET.SDL;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using SkiaSharp;
using Topten.RichTextKit;

unsafe class Program
{
    private static Sdl sdl = null!;
    private static GL gl = null!;
    private static IWindow window = null!;
    private static SKCanvas canvas = null!;
    private static SKSurface? surface;
    private static GRBackendRenderTarget? renderTarget;

    private const int Width = 1100;
    private const int Height = 420;
    // EXACT values from the Gum line that showed the artifact -- Samples/.../Screens/TextScreen.cs:
    //   withOutline.FontSize = 24;  withOutline.OutlineThickness = 2;  (font defaults to Arial)
    // GetStyle emits FontSize = 24 * GlobalTextScale(1) * FontScale(1) = 24 and HaloWidth = 2 verbatim.
    // The 'w' in "with" is the lowercase w whose bottom vertices your screenshot zoomed into.
    private const float OutlineWidth = 2f;
    private const float FontSize = 24f;
    private const string SampleText = "I am text with OutlineThickness = 2";
    // The SilkNet sample clears to CornflowerBlue (100,149,237); match it so the render is identical.
    private static readonly SKColor Background = new SKColor(100, 149, 237);

    private static readonly SKTypeface Arial = SKTypeface.FromFamilyName("Arial");
    private static readonly SKFont LabelFont = new SKFont(SKTypeface.FromFamilyName("Arial"), 18);
    private static readonly SKPaint LabelPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

    static unsafe void Main()
    {
        // ANGLE (D3D11) path -- same hints/attributes the SilkNet sample sets before context creation.
        Environment.SetEnvironmentVariable("ANGLE_DEFAULT_PLATFORM", "d3d11");
        sdl = SdlProvider.SDL.Value;
        sdl.SetHint("SDL_OPENGL_ES_DRIVER", "1");
        sdl.SetHint("SDL_HINT_OPENGL_ES_DRIVER", "angle");

        var api = new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 0));

        SdlWindowing.Use();

        var options = WindowOptions.Default;
        options.API = api;
        options.Size = new Vector2D<int>(Width, Height);
        options.Title = "RichTextKit outline miter-spike repro (no Gum)";
        options.WindowBorder = WindowBorder.Fixed;
        options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8);
        options.PreferredDepthBufferBits = 24;
        options.PreferredStencilBufferBits = 8;
        options.VSync = false;

        sdl.GLSetAttribute(GLattr.Doublebuffer, 1);
        // ANGLE binds its EGL surface format at window-creation time, so these must be set before
        // Window.Create, not just before context creation (see the sample's comment / #3652).
        sdl.GLSetAttribute(GLattr.ContextMajorVersion, api.Version.MajorVersion);
        sdl.GLSetAttribute(GLattr.ContextMinorVersion, api.Version.MinorVersion);
        sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)GLprofile.ES);

        window = Silk.NET.Windowing.Window.Create(options);
        window.Initialize();
        gl = GL.GetApi(window);

        using var grGlInterface = GRGlInterface.Create(name => (nint)sdl.GLGetProcAddress(name));
        grGlInterface.Validate();
        using var grContext = GRContext.CreateGl(grGlInterface);

        renderTarget = new GRBackendRenderTarget(Width, Height, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // GL_RGBA8
        surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        canvas = surface.Canvas;

        bool running = true;
        window.Closing += () => running = false;

        while (running && !window.IsClosing)
        {
            window.DoEvents();

            gl.ClearColor(1f, 1f, 1f, 1f);
            gl.Clear((uint)GLEnum.ColorBufferBit);
            grContext.ResetContext();

            DrawRepro();
            canvas.Flush();

            window.GLContext!.SwapBuffers();
        }

        surface?.Dispose();
        renderTarget?.Dispose();
        window?.Dispose();
        sdl?.Quit();
    }

    private static void DrawRepro()
    {
        // Match the SilkNet sample exactly: cornflower background, rendered directly on the GPU
        // (ANGLE) surface at 1:1 -- no CPU offscreen, no artificial zoom. Screenshot + zoom the 'w'.
        canvas.Clear(Background);

        float x = 24;
        float y = 20;

        y = DrawLabel("Exact Gum setup: Arial, FontSize 24, OutlineThickness 2, white text + black halo, CornflowerBlue bg.", x, y) + 12;

        y = DrawLabel("1) RichTextKit halo -- identical to SkiaGum.Text.GetStyle:", x, y) + 4;
        RtkHalo(x, y);
        y += FontSize * 2.2f;

        y = DrawLabel("2) Manual glyph stroke, StrokeJoin.Miter:", x, y) + 4;
        ManualStroke(x, y, SKStrokeJoin.Miter);
        y += FontSize * 2.2f;

        y = DrawLabel("3) Manual glyph stroke, StrokeJoin.Round:", x, y) + 4;
        ManualStroke(x, y, SKStrokeJoin.Round);
    }

    private static float DrawLabel(string text, float x, float y)
    {
        canvas.DrawText(text, x, y + 16, SKTextAlign.Left, LabelFont, LabelPaint);
        return y + 26;
    }

    // White text with a black halo via RichTextKit -- byte-for-byte the Style SkiaGum.Text.GetStyle
    // builds for the sample line (FontFamily Arial, FontSize 24, HaloColor black, HaloWidth 2, blur 0).
    private static void RtkHalo(float x, float y)
    {
        var style = new Style
        {
            FontFamily = "Arial",
            FontSize = FontSize,
            TextColor = SKColors.White,
            HaloColor = SKColors.Black,
            HaloWidth = OutlineWidth,
            HaloBlur = 0,
        };
        var block = new TextBlock();
        block.AddText(SampleText, style);
        block.Paint(canvas, new SKPoint(x, y));
    }

    // Same white text + black outline, but we stroke the glyph blob ourselves so we can choose the
    // StrokeJoin. Outline behind, fill on top.
    private static void ManualStroke(float x, float y, SKStrokeJoin join)
    {
        using var font = new SKFont(Arial, FontSize);
        using var blob = SKTextBlob.Create(SampleText, font);
        float baseline = y + FontSize * 0.9f;

        using var outline = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = OutlineWidth,
            Color = SKColors.Black,
            StrokeJoin = join,
        };
        canvas.DrawText(blob, x, baseline, outline);

        using var fill = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = SKColors.White,
        };
        canvas.DrawText(blob, x, baseline, fill);
    }
}
