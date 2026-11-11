using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.GueDeriving;
using GumRuntime;
using RenderingLibrary;
using Gum.Renderables;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum.Tests.Helpers;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pins how every raylib renderable with a Blend (Sprite, NineSlice, Text, Rectangle, Circle, Arc)
/// reads and draws its default, Normal, null and each Blend value (#7878). Pixels are read back from
/// a render texture over a blue backdrop, both through a baked render-target container and through a
/// direct draw with an optional ambient raylib blend mode set around Gum's draw. When the
/// RAYLIB_BLEND_TABLE environment variable names a file, every cell is appended to it as
/// "kind|scenario|context=result" so two runs can be diffed.
/// </summary>
public class RaylibBlendDefaultsTests : BaseTestClass
{
    private const int CanvasSize = 100;
    private static readonly Color Backdrop = new Color((byte)0, (byte)0, (byte)255, (byte)255);

    private static readonly string[] Kinds = { "Sprite", "NineSlice", "Text", "Rectangle", "Circle", "Arc" };

    private static readonly Blend[] AllBlends =
    {
        Blend.Normal, Blend.Additive, Blend.Replace, Blend.ReplaceAlpha, Blend.SubtractAlpha, Blend.MinAlpha
    };

    private sealed class Subject
    {
        public GraphicalUiElement Runtime = null!;
        public Action<Blend?> SetBlend = null!;
        public Func<Blend?> ReadRuntimeBlend = null!;
        public Func<Blend?> ReadRenderableBlend = null!;
        public Texture2D? Texture;
        public GraphicalUiElement Root = null!;
    }

    private static Subject Create(string kind, int alpha, string? loaded = null)
    {
        Subject subject = new();
        Color red = new Color((byte)255, (byte)0, (byte)0, (byte)alpha);

        Texture2D MakeTexture()
        {
            Image image = GenImageColor(4, 4, red);
            Texture2D texture = LoadTextureFromImage(image);
            UnloadImage(image);
            subject.Texture = texture;
            return texture;
        }

        switch (kind)
        {
            case "Sprite":
            {
                SpriteRuntime sprite = Make("Sprite", loaded, subject, () => new SpriteRuntime());
                sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                sprite.X = 20; sprite.Y = 20; sprite.Width = 60; sprite.Height = 60;
                sprite.Texture = MakeTexture();
                subject.Runtime = sprite;
                subject.SetBlend = b => sprite.Blend = b;
                subject.ReadRuntimeBlend = () => sprite.Blend;
                subject.ReadRenderableBlend = () => ((Sprite)sprite.RenderableComponent).Blend;
                break;
            }
            case "NineSlice":
            {
                NineSliceRuntime nine = Make("NineSlice", loaded, subject, () => new NineSliceRuntime());
                nine.X = 20; nine.Y = 20; nine.Width = 60; nine.Height = 60;
                nine.Texture = MakeTexture();
                subject.Runtime = nine;
                subject.SetBlend = b => nine.Blend = b;
                subject.ReadRuntimeBlend = () => nine.Blend;
                subject.ReadRenderableBlend = () => ((NineSlice)nine.RenderableComponent).Blend;
                break;
            }
            case "Text":
            {
                TextRuntime text = Make("Text", loaded, subject, () => new TextRuntime());
                text.Text = "W";
                text.FontSize = 80;
                text.X = 10; text.Y = 5;
                text.Red = 255; text.Green = 0; text.Blue = 0; text.Alpha = alpha;
                subject.Runtime = text;
                subject.SetBlend = b => text.Blend = b;
                subject.ReadRuntimeBlend = () => text.Blend;
                subject.ReadRenderableBlend = () => ((Text)text.RenderableComponent).Blend;
                break;
            }
            case "Rectangle":
            {
                RectangleRuntime rectangle = Make("Rectangle", loaded, subject, () => new RectangleRuntime());
                rectangle.X = 20; rectangle.Y = 20; rectangle.Width = 60; rectangle.Height = 60;
                rectangle.IsFilled = true; rectangle.StrokeWidth = 0; rectangle.FillColor = red;
                subject.Runtime = rectangle;
                LineRectangle renderable = (LineRectangle)rectangle.RenderableComponent;
                subject.SetBlend = b => { if (b.HasValue) rectangle.Blend = b.Value; else renderable.Blend = null; };
                subject.ReadRuntimeBlend = () => rectangle.Blend;
                subject.ReadRenderableBlend = () => renderable.Blend;
                break;
            }
            case "Circle":
            {
                CircleRuntime circle = Make("Circle", loaded, subject, () => new CircleRuntime());
                circle.X = 20; circle.Y = 20; circle.Width = 60; circle.Height = 60;
                circle.IsFilled = true; circle.StrokeWidth = 0; circle.FillColor = red;
                subject.Runtime = circle;
                LineCircle renderable = (LineCircle)circle.RenderableComponent;
                subject.SetBlend = b => { if (b.HasValue) circle.Blend = b.Value; else renderable.Blend = null; };
                subject.ReadRuntimeBlend = () => circle.Blend;
                subject.ReadRenderableBlend = () => renderable.Blend;
                break;
            }
            default:
            {
                ArcRuntime arc = Make("Arc", loaded, subject, () => new ArcRuntime());
                arc.X = 20; arc.Y = 20; arc.Width = 60; arc.Height = 60;
                arc.StartAngle = 0; arc.SweepAngle = 360; arc.Thickness = 12; arc.StrokeColor = red;
                subject.Runtime = arc;
                LineArc renderable = (LineArc)arc.RenderableComponent;
                subject.SetBlend = b => { if (b.HasValue) arc.Blend = b.Value; else renderable.Blend = null; };
                subject.ReadRuntimeBlend = () => arc.Blend;
                subject.ReadRenderableBlend = () => renderable.Blend;
                break;
            }
        }
        subject.Root ??= subject.Runtime;
        return subject;
    }


    // Builds the element the way a loaded project does: a screen instance whose base standard element
    // carries the authored Blend variable ("Normal" or "Additive" on the instance, "absent" for a project
    // that never wrote one). Null builds it in code.
    private static T Make<T>(string standardName, string? loaded, Subject subject, Func<T> create) where T : GraphicalUiElement
    {
        if (loaded == null)
        {
            return create();
        }

        StandardElementSave standard = new() { Name = standardName };
        standard.States.Add(new StateSave { Name = "Default", ParentContainer = standard });
        if (loaded != "absent")
        {
            standard.DefaultState.Variables.Add(new VariableSave { Name = "Blend", Type = "Blend", Value = Blend.Normal, SetsValue = true });
        }

        ScreenSave screen = new() { Name = "BlendScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Inst", BaseType = standardName, ParentContainer = screen });
        if (loaded == "Additive")
        {
            screen.DefaultState.Variables.Add(new VariableSave { Name = "Inst.Blend", Type = "Blend", Value = Blend.Additive, SetsValue = true });
        }

        typeof(SystemManagers).GetMethod("RegisterComponentRuntimeInstantiations", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(new SystemManagers(), null);

        GumProjectSave project = new();
        project.StandardElements.Add(standard);
        project.Screens.Add(screen);
        GumProjectSave? previous = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            GraphicalUiElement visual = screen.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);
            subject.Root = visual;
            return (T)visual.GetGraphicalUiElementByName("Inst")!;
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = previous;
        }
    }

    /// <summary>The pixel that differs most from the backdrop, so text and ring shapes are probed on their ink.</summary>
    private static Color Probe(PixelGrid grid)
    {
        Color best = grid[0, 0];
        int bestDiff = -1;
        foreach (Color pixel in grid.Pixels)
        {
            int diff = Math.Abs(pixel.R - Backdrop.R) + Math.Abs(pixel.G - Backdrop.G)
                + Math.Abs(pixel.B - Backdrop.B) + Math.Abs(pixel.A - Backdrop.A);
            if (diff > bestDiff)
            {
                bestDiff = diff;
                best = pixel;
            }
        }
        return best;
    }

    private static Color RenderBaked(string kind, int alpha, string? mode, string? loaded = null)
    {
        Subject subject = Create(kind, alpha, loaded);
        Apply(subject, mode);
        try
        {
            PixelGrid grid = RaylibPixelHarness.Render(CanvasSize, CanvasSize, Backdrop,
                container => container.Children.Add(subject.Root));
            return Probe(grid);
        }
        finally
        {
            Release(subject);
        }
    }

    private static Color RenderDirect(string kind, int alpha, string? mode, BlendMode? ambient, string? loaded = null)
    {
        Subject subject = Create(kind, alpha, loaded);
        Apply(subject, mode);
        RenderTexture2D target = LoadRenderTexture(CanvasSize, CanvasSize);
        Gum.GumService.Default.Root.Children.Add(subject.Root);
        Gum.GumService.Default.Root.UpdateLayout();
        try
        {
            BeginDrawing();
            BeginTextureMode(target);
            ClearBackground(Backdrop);
            if (ambient.HasValue)
            {
                BeginBlendMode(ambient.Value);
            }
            Gum.GumService.Default.Draw();
            if (ambient.HasValue)
            {
                EndBlendMode();
            }
            EndTextureMode();
            EndDrawing();
            return Probe(RaylibPixelHarness.Read(target, CanvasSize, CanvasSize));
        }
        finally
        {
            Gum.GumService.Default.Root.Children.Remove(subject.Root);
            UnloadRenderTexture(target);
            Release(subject);
        }
    }

    // mode: null = leave the default untouched, "null" = assign null, else a Blend name.
    private static void Apply(Subject subject, string? mode)
    {
        if (mode == null)
        {
            return;
        }
        if (mode == "null")
        {
            subject.SetBlend(null);
            return;
        }
        subject.SetBlend(Enum.Parse<Blend>(mode));
    }

    private static void Release(Subject subject)
    {
        if (subject.Texture.HasValue)
        {
            UnloadTexture(subject.Texture.Value);
        }
    }

    private static string Format(Color c) => $"({c.R},{c.G},{c.B},{c.A})";

    private static void Record(string line)
    {
        string? path = Environment.GetEnvironmentVariable("RAYLIB_BLEND_TABLE");
        if (!string.IsNullOrEmpty(path))
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }

    private static void AssertClose(Color actual, Color expected, string cell, int tolerance = 16)
    {
        string message = $"{cell}: was {Format(actual)}, expected {Format(expected)} +/- {tolerance}";
        Math.Abs(actual.R - expected.R).ShouldBeLessThanOrEqualTo(tolerance, message);
        Math.Abs(actual.G - expected.G).ShouldBeLessThanOrEqualTo(tolerance, message);
        Math.Abs(actual.B - expected.B).ShouldBeLessThanOrEqualTo(tolerance, message);
    }

    [Fact]
    public void Readback_RecordsDefaultNormalNullAndEveryBlend()
    {
        foreach (string kind in Kinds)
        {
            Subject subject = Create(kind, 255);
            Record($"{kind}|default|readback=rt:{subject.ReadRuntimeBlend()?.ToString() ?? "null"} renderable:{subject.ReadRenderableBlend()?.ToString() ?? "null"}");

            foreach (string mode in AllBlends.Select(b => b.ToString()).Append("null"))
            {
                Subject set = Create(kind, 255);
                Apply(set, mode);
                Record($"{kind}|set {mode}|readback=rt:{set.ReadRuntimeBlend()?.ToString() ?? "null"} renderable:{set.ReadRenderableBlend()?.ToString() ?? "null"}");
                Release(set);
            }
            Release(subject);
        }
    }

    [Fact]
    public void Readback_RuntimeBlendRoundTripsAdditive()
    {
        foreach (string kind in Kinds)
        {
            Subject subject = Create(kind, 255);
            subject.SetBlend(Blend.Additive);
            subject.ReadRuntimeBlend().ShouldBe(Blend.Additive, kind);
            subject.ReadRenderableBlend().ShouldBe(Blend.Additive, kind);
            Release(subject);
        }
    }

    [Fact]
    public void Clone_CarriesTheRenderableBlend()
    {
        foreach (string kind in Kinds)
        {
            foreach (string? mode in new string?[] { null, "Additive", "Normal" })
            {
                Subject subject = Create(kind, 255);
                Apply(subject, mode);
                Blend? before = subject.ReadRenderableBlend();

                GraphicalUiElement clone = subject.Runtime.Clone();
                Blend? cloned = kind switch
                {
                    "Sprite" => ((Sprite)clone.RenderableComponent).Blend,
                    "NineSlice" => ((NineSlice)clone.RenderableComponent).Blend,
                    "Text" => ((Text)clone.RenderableComponent).Blend,
                    "Rectangle" => ((LineRectangle)clone.RenderableComponent).Blend,
                    "Circle" => ((LineCircle)clone.RenderableComponent).Blend,
                    _ => ((LineArc)clone.RenderableComponent).Blend,
                };
                Record($"{kind}|clone of {mode ?? "default"}|renderable={cloned?.ToString() ?? "null"}");
                cloned.ShouldBe(before, $"{kind} clone of {mode ?? "default"}");
                Release(subject);
            }
        }
    }

    [Fact]
    public void Text_BlendState_NullAndNormalAreNonPremultiplied()
    {
        TextRuntime text = new();
        Text renderable = (Text)text.RenderableComponent;

        text.Blend = null;
        Gum.BlendState forNull = ((global::RenderingLibrary.Graphics.IRenderable)renderable).BlendState;
        text.Blend = Blend.Normal;
        Gum.BlendState forNormal = ((global::RenderingLibrary.Graphics.IRenderable)renderable).BlendState;
        text.Blend = Blend.Additive;
        Gum.BlendState forAdditive = ((global::RenderingLibrary.Graphics.IRenderable)renderable).BlendState;

        Record($"Text|BlendState|null={forNull == Gum.BlendState.NonPremultiplied} normal={forNormal == Gum.BlendState.NonPremultiplied} additiveIsNonPremultiplied={forAdditive == Gum.BlendState.NonPremultiplied}");
        forNull.ShouldBe(Gum.BlendState.NonPremultiplied);
        forNormal.ShouldBe(Gum.BlendState.NonPremultiplied);
        forAdditive.ShouldNotBe(Gum.BlendState.NonPremultiplied);

        TextRuntime defaultText = new();
        ((global::RenderingLibrary.Graphics.IRenderable)defaultText.RenderableComponent).BlendState
            .ShouldBe(Gum.BlendState.NonPremultiplied);
    }

    [Fact]
    public void Pixels_EveryKindEveryBlendAtBothAlphas_BakedAndDirect()
    {
        foreach (string kind in Kinds)
        {
            foreach (int alpha in new[] { 255, 128 })
            {
                foreach (string? mode in new string?[] { null, "null" }.Concat(AllBlends.Select(b => b.ToString())))
                {
                    string label = mode ?? "default";
                    Color baked = RenderBaked(kind, alpha, mode);
                    Color direct = RenderDirect(kind, alpha, mode, ambient: null);
                    Record($"{kind}|{label}|a{alpha}|baked={Format(baked)}");
                    Record($"{kind}|{label}|a{alpha}|direct={Format(direct)}");
                }
            }
        }
    }

    [Fact]
    public void Pixels_NormalAndAdditiveOverBackdrop_MatchTheAlphaMath()
    {
        foreach (string kind in Kinds)
        {
            foreach (int alpha in new[] { 255, 128 })
            {
                // Normal: red over blue by source alpha. Additive: red added onto the blue.
                int half = alpha;
                Color normal = new Color((byte)alpha, (byte)0, (byte)(255 - alpha), (byte)255);
                Color additive = new Color((byte)half, (byte)0, (byte)255, (byte)255);

                AssertClose(RenderDirect(kind, alpha, "Normal", null), normal, $"{kind} Normal a{alpha} direct", 30);
                AssertClose(RenderDirect(kind, alpha, "Additive", null), additive, $"{kind} Additive a{alpha} direct", 30);
                AssertClose(RenderBaked(kind, alpha, "Normal"), normal, $"{kind} Normal a{alpha} baked", 30);
            }
        }
    }

    [Fact]
    public void Pixels_WithNoAmbientMode_DefaultNullAndNormalDrawIdentically()
    {
        foreach (string kind in Kinds)
        {
            foreach (int alpha in new[] { 255, 128 })
            {
                Color normal = RenderDirect(kind, alpha, "Normal", null);
                AssertClose(RenderDirect(kind, alpha, null, null), normal, $"{kind} default a{alpha}", 2);
                AssertClose(RenderDirect(kind, alpha, "null", null), normal, $"{kind} null a{alpha}", 2);

                Color bakedNormal = RenderBaked(kind, alpha, "Normal");
                AssertClose(RenderBaked(kind, alpha, null), bakedNormal, $"{kind} baked default a{alpha}", 2);
                AssertClose(RenderBaked(kind, alpha, "null"), bakedNormal, $"{kind} baked null a{alpha}", 2);
            }
        }
    }

    [Fact]
    public void Pixels_AmbientAdditive_NormalOverridesIt()
    {
        foreach (string kind in Kinds)
        {
            foreach (int alpha in new[] { 255, 128 })
            {
                Color normalNoAmbient = RenderDirect(kind, alpha, "Normal", null);
                Color normalAmbient = RenderDirect(kind, alpha, "Normal", BlendMode.Additive);
                Record($"{kind}|Normal|a{alpha}|ambientAdditive={Format(normalAmbient)}");
                // TODAY: CircleRuntime and ArcRuntime store Normal as a null renderable Blend (shared source), so they keep the ambient mode.
                if (kind is "Circle" or "Arc")
                {
                    AssertClose(normalAmbient, RenderDirect(kind, alpha, "Additive", null), $"{kind} Normal under ambient additive a{alpha}", 30);
                    continue;
                }
                AssertClose(normalAmbient, normalNoAmbient, $"{kind} Normal under ambient additive a{alpha}", 2);
            }
        }
    }

    [Fact]
    public void Pixels_AmbientAdditive_DefaultAndNullFollowTheDefaultRule()
    {
        foreach (string kind in Kinds)
        {
            foreach (int alpha in new[] { 255, 128 })
            {
                Color additive = RenderDirect(kind, alpha, "Additive", null);
                Color normal = RenderDirect(kind, alpha, "Normal", null);
                Color defaultAmbient = RenderDirect(kind, alpha, null, BlendMode.Additive);
                Color nullAmbient = RenderDirect(kind, alpha, "null", BlendMode.Additive);
                Record($"{kind}|default|a{alpha}|ambientAdditive={Format(defaultAmbient)}");
                Record($"{kind}|null|a{alpha}|ambientAdditive={Format(nullAmbient)}");

                // TODAY (before the default changes): null draws under the game's own blend mode.
                AssertClose(defaultAmbient, additive, $"{kind} default under ambient additive a{alpha}", 30);
                AssertClose(nullAmbient, additive, $"{kind} null under ambient additive a{alpha}", 30);
                if (alpha == 128)
                {
                    Math.Abs(defaultAmbient.B - normal.B).ShouldBeGreaterThan(40, kind);
                }
            }
        }
    }

    [Fact]
    public void Polygon_HasNoBlendMember_SoNothingToDefault()
    {
        bool runtimeHasBlend = typeof(PolygonRuntime).GetProperty("Blend") != null;
        bool renderableHasBlend = typeof(LinePolygon).GetProperty("Blend") != null;
        Record($"Polygon|Blend member|runtime={runtimeHasBlend} renderable={renderableHasBlend}");
        runtimeHasBlend.ShouldBeFalse();
        renderableHasBlend.ShouldBeFalse();
    }

    [Fact]
    public void ProjectLoaded_ReadbackAndPixels_IncludingAmbientAdditive()
    {
        foreach (string kind in Kinds)
        {
            foreach (string loaded in new[] { "Normal", "Additive", "absent" })
            {
                Subject subject = Create(kind, 255, loaded);
                Record($"{kind}|loaded {loaded}|readback=rt:{subject.ReadRuntimeBlend()?.ToString() ?? "null"} renderable:{subject.ReadRenderableBlend()?.ToString() ?? "null"}");
                Release(subject);

                foreach (int alpha in new[] { 255, 128 })
                {
                    Color baked = RenderBaked(kind, alpha, null, loaded);
                    Color direct = RenderDirect(kind, alpha, null, null, loaded);
                    Color ambient = RenderDirect(kind, alpha, null, BlendMode.Additive, loaded);
                    Record($"{kind}|loaded {loaded}|a{alpha}|baked={Format(baked)}");
                    Record($"{kind}|loaded {loaded}|a{alpha}|direct={Format(direct)}");
                    Record($"{kind}|loaded {loaded}|a{alpha}|ambientAdditive={Format(ambient)}");

                    Color additive = RenderDirect(kind, alpha, "Additive", null);
                    Color normal = RenderDirect(kind, alpha, "Normal", null);
                    if (loaded == "Additive")
                    {
                        AssertClose(direct, additive, $"{kind} loaded Additive a{alpha}", 30);
                        AssertClose(ambient, additive, $"{kind} loaded Additive ambient a{alpha}", 30);
                    }
                    else if (loaded == "Normal")
                    {
                        AssertClose(direct, normal, $"{kind} loaded Normal a{alpha}", 30);
                        // TODAY: a loaded Normal overrides the ambient mode except on Circle, whose Blend setter stores Normal as null
                        // (Arc keeps Normal on the renderable when loaded).
                        AssertClose(ambient, kind == "Circle" ? additive : normal, $"{kind} loaded Normal ambient a{alpha}", 30);
                    }
                    else
                    {
                        // TODAY: no authored Blend leaves the renderable default (null), which follows the ambient mode.
                        AssertClose(direct, normal, $"{kind} loaded absent a{alpha}", 30);
                        AssertClose(ambient, additive, $"{kind} loaded absent ambient a{alpha}", 30);
                    }
                }
            }
        }
    }
}
