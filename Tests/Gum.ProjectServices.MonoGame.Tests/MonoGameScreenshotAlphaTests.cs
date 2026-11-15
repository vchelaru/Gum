using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.MonoGame;
using Gum.ProjectServices.Screenshot;
using Shouldly;
using SkiaSharp;

namespace Gum.ProjectServices.MonoGame.Tests;

/// <summary>
/// <c>gumcli screenshot</c> with no background color writes a straight-alpha PNG (#7992): a translucent element
/// keeps its own color and alpha whether it is drawn directly or through a render-target container, and
/// overlapping translucent elements combine by the "over" operator. Expected values by hand, with
/// a = alpha / 255: one element is (color, a); two overlapping are alpha = a2 + a1 * (1 - a2),
/// color = (c2 * a2 + c1 * a1 * (1 - a2)) / alpha.
/// </summary>
public class MonoGameScreenshotAlphaTests : IDisposable
{
    private const int Tolerance = 1;
    private readonly string _tempDirectory;

    public MonoGameScreenshotAlphaTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumMonoGameScreenshotAlpha_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void TakeScreenshot_TranslucentElementsOnTransparentBackground_WritesStraightAlphaPng()
    {
        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");
        GumProjectSave project = new ProjectCreator().Create(projectPath);
        WriteSolidPng(Path.Combine(_tempDirectory, "red.png"), SKColors.Red);
        WriteSolidPng(Path.Combine(_tempDirectory, "green.png"), new SKColor(0, 255, 0));

        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        void Add(string name, string baseType, float x, float y)
        {
            screen.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = screen });
            Set($"{name}.X", "float", x);
            Set($"{name}.Y", "float", y);
        }
        void Set(string name, string type, object value) =>
            state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
        void AddSprite(string name, float x, float y, string file, int alpha)
        {
            Add(name, "Sprite", x, y);
            Set($"{name}.SourceFile", "string", file);
            Set($"{name}.Alpha", "int", alpha);
        }
        void AddRenderTarget(string name, float x, float y, int alpha)
        {
            Add(name, "Container", x, y);
            Set($"{name}.Width", "float", 60f);
            Set($"{name}.Height", "float", 40f);
            Set($"{name}.IsRenderTarget", "bool", true);
            Set($"{name}.Alpha", "int", alpha);
        }

        AddSprite("Direct", 20, 20, "red.png", 128);

        AddRenderTarget("Holder", 100, 20, 255);
        AddSprite("InTarget", 0, 0, "red.png", 128);
        Set("InTarget.Parent", "string", "Holder");

        Add("Shape", "Rectangle", 180, 20);
        Set("Shape.Width", "float", 60f);
        Set("Shape.Height", "float", 40f);
        Set("Shape.IsFilled", "bool", true);
        Set("Shape.FillRed", "int", 255);
        Set("Shape.FillGreen", "int", 0);
        Set("Shape.FillBlue", "int", 0);
        Set("Shape.FillAlpha", "int", 128);
        Set("Shape.StrokeWidth", "float", 0f);

        AddRenderTarget("Faded", 260, 20, 128);
        AddSprite("InFaded", 0, 0, "red.png", 255);
        Set("InFaded.Parent", "string", "Faded");

        AddSprite("Under", 20, 80, "red.png", 128);
        AddSprite("Over", 50, 100, "green.png", 128);

        Add("Label", "Text", 180, 90);
        Set("Label.Text", "string", "HOUZ");
        Set("Label.Red", "int", 255);
        Set("Label.Green", "int", 0);
        Set("Label.Blue", "int", 0);
        Set("Label.Alpha", "int", 128);

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = "Screen", ElementType = ElementType.Screen });
        project.Save(projectPath, saveElements: true);

        string outputPath = Path.Combine(_tempDirectory, "Screen.png");
        ScreenshotResult result = new MonoGameScreenshotService().TakeScreenshot(new ScreenshotRequest
        {
            ProjectPath = projectPath,
            ElementName = "Screen",
            OutputPath = outputPath,
            Width = 360,
            Height = 160,
        });
        result.Success.ShouldBeTrue(result.ErrorMessage);

        // Decode into an unpremultiplied bitmap so the PNG's own straight-alpha bytes are read as stored.
        using SKCodec codec = SKCodec.Create(outputPath);
        SKImageInfo info = codec.Info.WithAlphaType(SKAlphaType.Unpremul);
        using SKBitmap image = new SKBitmap(info);
        codec.GetPixels(info, image.GetPixels()).ShouldBe(SKCodecResult.Success);

        (string Name, int X, int Y, byte R, byte G, byte B, byte A)[] samples =
        {
            ("direct sprite", 40, 30, 255, 0, 0, 128),
            ("render target", 120, 30, 255, 0, 0, 128),
            ("rectangle shape", 210, 40, 255, 0, 0, 128),
            ("render target at alpha 128", 290, 40, 255, 0, 0, 128),
            ("under only", 30, 90, 255, 0, 0, 128),
            ("overlap", 65, 110, 85, 170, 0, 192),
            ("over only", 100, 130, 0, 255, 0, 128),
            ("outside", 340, 140, 0, 0, 0, 0),
        };

        List<string> report = new();
        bool allMatch = true;
        foreach ((string name, int x, int y, byte r, byte g, byte b, byte a) in samples)
        {
            SKColor pixel = image.GetPixel(x, y);
            int delta = a == 0
                ? pixel.Alpha
                : Math.Max(Math.Max(Math.Abs(pixel.Red - r), Math.Abs(pixel.Green - g)),
                    Math.Max(Math.Abs(pixel.Blue - b), Math.Abs(pixel.Alpha - a)));
            allMatch &= delta <= Tolerance;
            report.Add($"{name}: expected ({r},{g},{b},{a}) actual ({pixel.Red},{pixel.Green},{pixel.Blue},{pixel.Alpha}) delta {delta}");
        }

        // Red text at alpha 128: a fully covered glyph pixel is (255,0,0,128), so the most opaque pixel in the
        // text's area must be exactly that, and every pixel at alpha 64 or more must still be pure red (within 3).
        SKColor densest = default;
        int worstColor = 0;
        for (int y = 90; y < 130; y++)
        {
            for (int x = 180; x < 260; x++)
            {
                SKColor pixel = image.GetPixel(x, y);
                if (pixel.Alpha > densest.Alpha)
                {
                    densest = pixel;
                }
                if (pixel.Alpha >= 64)
                {
                    worstColor = Math.Max(worstColor, Math.Max(255 - pixel.Red, Math.Max(pixel.Green, pixel.Blue)));
                }
            }
        }
        int textDelta = Math.Max(Math.Max(Math.Abs(densest.Red - 255), Math.Max(densest.Green, densest.Blue)),
            Math.Abs(densest.Alpha - 128));
        allMatch &= textDelta <= Tolerance && worstColor <= 3;
        report.Add($"text, most opaque glyph pixel: expected (255,0,0,128) actual ({densest.Red},{densest.Green},{densest.Blue},{densest.Alpha}) delta {textDelta}");
        report.Add($"text, worst color off pure red at alpha >= 64: {worstColor} (limit 3)");

        string? keep = Environment.GetEnvironmentVariable("GUM_EXPORT_ALPHA_DIR");
        if (!string.IsNullOrEmpty(keep))
        {
            Directory.CreateDirectory(keep);
            File.Copy(outputPath, Path.Combine(keep, "cli-screenshot.png"), overwrite: true);
            File.AppendAllLines(Path.Combine(keep, "results.txt"), report.Prepend("[cli]"));
        }
        allMatch.ShouldBeTrue(string.Join(Environment.NewLine, report));
    }

    // 60 x 40, the sprite's default size (100% of its file).
    private static void WriteSolidPng(string path, SKColor color)
    {
        using SKBitmap bitmap = new SKBitmap(60, 40);
        bitmap.Erase(color);
        using FileStream stream = File.Create(path);
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100).ShouldBeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
