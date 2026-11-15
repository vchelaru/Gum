using System;
using System.Collections.Generic;
using System.IO;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;

namespace Gum.Tests.Shared.CrossRuntime;

/// <summary>
/// A screen of translucent solid-color elements for checking that a screenshot service writes a correct
/// straight-alpha PNG over a transparent background (#7992). Expected values are worked out by hand, with
/// a = alpha / 255: one element is (color, a); two overlapping are alpha = a2 + a1 * (1 - a2),
/// color = (c2 * a2 + c1 * a1 * (1 - a2)) / alpha. Linked into each runtime's screenshot test project, which
/// supplies the PNG writer and reader.
/// </summary>
public static class ScreenshotAlphaScene
{
    public const string ScreenName = "Screen";
    public const int Width = 360;
    public const int Height = 160;
    private const int Tolerance = 1;

    /// <summary>Writes the project into <paramref name="folder"/> and returns the .gumx path.</summary>
    /// <param name="writeSolidPng">Writes an opaque 60 x 40 PNG of the given color to the given path.</param>
    public static string CreateProject(string folder, Action<string, byte, byte, byte> writeSolidPng)
    {
        string projectPath = Path.Combine(folder, "Project.gumx");
        GumProjectSave project = new ProjectCreator().Create(projectPath);
        writeSolidPng(Path.Combine(folder, "red.png"), 255, 0, 0);
        writeSolidPng(Path.Combine(folder, "green.png"), 0, 255, 0);

        ScreenSave screen = new ScreenSave { Name = ScreenName };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        void Set(string name, string type, object value) =>
            state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
        void Add(string name, string baseType, float x, float y)
        {
            screen.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = screen });
            Set($"{name}.X", "float", x);
            Set($"{name}.Y", "float", y);
        }
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

        // Opaque red sprite at Alpha 128, drawn directly.
        AddSprite("Direct", 20, 20, "red.png", 128);

        // The same, inside a render-target container.
        AddRenderTarget("Holder", 100, 20, 255);
        AddSprite("InTarget", 0, 0, "red.png", 128);
        Set("InTarget.Parent", "string", "Holder");

        // A filled Rectangle at fill alpha 128.
        Add("Shape", "Rectangle", 180, 20);
        Set("Shape.Width", "float", 60f);
        Set("Shape.Height", "float", 40f);
        Set("Shape.IsFilled", "bool", true);
        Set("Shape.FillRed", "int", 255);
        Set("Shape.FillGreen", "int", 0);
        Set("Shape.FillBlue", "int", 0);
        Set("Shape.FillAlpha", "int", 128);
        Set("Shape.StrokeWidth", "float", 0f);

        // An opaque red sprite inside a render-target container at Alpha 128.
        AddRenderTarget("Faded", 260, 20, 128);
        AddSprite("InFaded", 0, 0, "red.png", 255);
        Set("InFaded.Parent", "string", "Faded");

        // Red then green, both Alpha 128, overlapping by 30 x 20.
        AddSprite("Under", 20, 80, "red.png", 128);
        AddSprite("Over", 50, 100, "green.png", 128);

        // Red text at Alpha 128.
        Add("Label", "Text", 180, 90);
        Set("Label.Text", "string", "HOUZ");
        Set("Label.Red", "int", 255);
        Set("Label.Green", "int", 0);
        Set("Label.Blue", "int", 0);
        Set("Label.Alpha", "int", 128);

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = ScreenName, ElementType = ElementType.Screen });
        project.Save(projectPath, saveElements: true);
        return projectPath;
    }

    /// <summary>
    /// Checks the saved PNG's straight-alpha pixels. Each sample must match within 1. For the text, the most
    /// opaque pixel in its area must be (255,0,0,128) within 1, and every pixel at alpha 64 or more must be
    /// pure red within 3. Returns whether all match, and one line per check.
    /// </summary>
    /// <param name="pixel">The PNG's stored (straight-alpha) RGBA at x, y.</param>
    public static (bool AllMatch, List<string> Report) Check(Func<int, int, (byte R, byte G, byte B, byte A)> pixel)
    {
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
            (byte pr, byte pg, byte pb, byte pa) = pixel(x, y);
            int delta = a == 0
                ? pa
                : Math.Max(Math.Max(Math.Abs(pr - r), Math.Abs(pg - g)), Math.Max(Math.Abs(pb - b), Math.Abs(pa - a)));
            allMatch &= delta <= Tolerance;
            report.Add($"{name}: expected ({r},{g},{b},{a}) actual ({pr},{pg},{pb},{pa}) delta {delta}");
        }

        (byte R, byte G, byte B, byte A) densest = default;
        int worstColor = 0;
        for (int y = 90; y < 130; y++)
        {
            for (int x = 180; x < 260; x++)
            {
                (byte R, byte G, byte B, byte A) p = pixel(x, y);
                if (p.A > densest.A)
                {
                    densest = p;
                }
                if (p.A >= 64)
                {
                    worstColor = Math.Max(worstColor, Math.Max(255 - p.R, Math.Max(p.G, p.B)));
                }
            }
        }
        int textDelta = Math.Max(Math.Max(Math.Abs(densest.R - 255), Math.Max(densest.G, densest.B)), Math.Abs(densest.A - 128));
        allMatch &= textDelta <= Tolerance && worstColor <= 3;
        report.Add($"text, most opaque glyph pixel: expected (255,0,0,128) actual ({densest.R},{densest.G},{densest.B},{densest.A}) delta {textDelta}");
        report.Add($"text, worst color off pure red at alpha >= 64: {worstColor} (limit 3)");
        return (allMatch, report);
    }

    /// <summary>When GUM_EXPORT_ALPHA_DIR is set, keeps a copy of the PNG and appends the report there.</summary>
    public static void Keep(string png, string runName, List<string> report)
    {
        string? keep = Environment.GetEnvironmentVariable("GUM_EXPORT_ALPHA_DIR");
        if (string.IsNullOrEmpty(keep))
        {
            return;
        }
        Directory.CreateDirectory(keep);
        File.Copy(png, Path.Combine(keep, runName + ".png"), overwrite: true);
        List<string> lines = new() { $"[{runName}]" };
        lines.AddRange(report);
        File.AppendAllLines(Path.Combine(keep, "results.txt"), lines);
    }
}
