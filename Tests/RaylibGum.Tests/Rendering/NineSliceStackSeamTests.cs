using System;
using System.Collections.Generic;
using System.IO;
using CrossRuntimeTests;
using Gum.DataTypes;
using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using static CrossRuntimeTests.NineSliceSeamSweep;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #8072: a ListBox moved by a scrollbar stacks Forms NineSlices at fractional positions, and adjacent items must show
/// no seam (no gap, hairline, lost outline or overlap). The sweeps and the counting are shared with MonoGame and Skia
/// (<see cref="NineSliceSeamSweep"/>); this renders them on the raylib NineSlice.
/// Set GUM_SEAM_DUMP to a folder to write the first frame of each texture and the first failing frames as magnified PNGs.
/// </summary>
public class NineSliceStackSeamTests : BaseTestClass
{
    readonly Dictionary<string, Texture2D> _textures = new();

    public override void Dispose()
    {
        foreach (Texture2D texture in _textures.Values)
        {
            UnloadTexture(texture);
        }
        _textures.Clear();
        base.Dispose();
    }

    Texture2D TextureFor(TextureSpec spec)
    {
        if (spec.Source != null)
        {
            return SystemManagers.Default.GetOrLoadEmbeddedTexture2d("UISpriteSheet.png");
        }
        if (!_textures.TryGetValue(spec.Name, out Texture2D texture))
        {
            int size = spec.Border * 3;
            Image image = GenImageColor(size, size, Color.Blank);
            try
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Rgb c = spec.SyntheticTexel(x, y);
                        ImageDrawPixel(ref image, x, y, new Color(c.R, c.G, c.B, (byte)255));
                    }
                }
                texture = LoadTextureFromImage(image);
            }
            finally
            {
                UnloadImage(image);
            }
            _textures[spec.Name] = texture;
        }
        return texture;
    }

    Frame Render(TextureSpec spec, Layout layout)
    {
        Renderer renderer = Renderer.Self;
        float logicalWidth = layout.PixelWidth / layout.Zoom;
        float logicalHeight = layout.PixelHeight / layout.Zoom;

        ContainerRuntime wrapper = new()
        {
            X = 0,
            Y = 0,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = logicalWidth,
            Height = logicalHeight,
            IsRenderTarget = true,
        };
        RectangleRuntime background = new()
        {
            Width = logicalWidth,
            Height = logicalHeight,
            IsFilled = true,
            StrokeWidth = 0,
            FillColor = new Color(Background.R, Background.G, Background.B, (byte)255),
        };
        wrapper.Children.Add(background);

        // A ListBox's shape: a stack moved by the scroll offset.
        ContainerRuntime stack = new()
        {
            X = layout.Left,
            Y = layout.BaseTop - layout.Scroll,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = layout.Width,
            Height = 0,
            ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack,
            StackSpacing = layout.Spacing,
        };
        wrapper.Children.Add(stack);
        Texture2D texture = TextureFor(spec);
        for (int i = 0; i < layout.Count; i++)
        {
            NineSliceRuntime item = new()
            {
                Texture = texture,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
                Width = layout.Width,
                Height = layout.ItemHeight,
            };
            if (spec.Source == null)
            {
                item.TextureAddress = Gum.Managers.TextureAddress.EntireTexture;
            }
            else
            {
                item.TextureAddress = Gum.Managers.TextureAddress.Custom;
                item.TextureLeft = spec.Source[0];
                item.TextureTop = spec.Source[1];
                item.TextureWidth = spec.Source[2];
                item.TextureHeight = spec.Source[3];
            }
            stack.Children.Add(item);
        }

        Gum.GumService.Default.Root.Children.Add(wrapper);
        Gum.GumService.Default.Root.UpdateLayout();
        renderer.Camera.Zoom = layout.Zoom;
        try
        {
            BeginDrawing();
            Gum.GumService.Default.Draw();
            EndDrawing();

            RenderTexture2D baked = renderer.TryGetBakedRenderTargetFor(wrapper)!.Value;
            int width = baked.Texture.Width;
            int height = baked.Texture.Height;
            Image image = LoadImageFromTexture(baked.Texture);
            try
            {
                Rgb[] pixels = new Rgb[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // A render texture is stored bottom-up in GL, so flip the row.
                        Color color = GetImageColor(image, x, height - 1 - y);
                        pixels[(y * width) + x] = new Rgb(color.R, color.G, color.B);
                    }
                }
                return new Frame(width, height, pixels);
            }
            finally
            {
                UnloadImage(image);
            }
        }
        finally
        {
            renderer.Camera.Zoom = 1f;
            Gum.GumService.Default.Root.Children.Clear();
        }
    }

    static void Check(string name, SeamCounts counts)
    {
        string? folder = Environment.GetEnvironmentVariable("GUM_SEAM_DUMP");
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "sweep-summary.txt"), "raylib " + name + ": " + counts + Environment.NewLine);
        }
        counts.Total.ShouldBe(0, counts.ToString());
    }

    [Theory]
    [InlineData(1f, 0f)]
    [InlineData(1f, 6f)]
    [InlineData(2f, 0f)]
    [InlineData(2f, 6f)]
    public void Draw_StackedNineSlicesAtAwkwardScrollSteps_ShouldHaveNoSeam(float zoom, float spacing)
    {
        SeamCounts counts = SweepSteps(Dumping("raylib", $"step-z{zoom}-s{spacing}", Render), zoom, spacing);

        Check($"step 0.0126123, zoom {zoom}, spacing {spacing}", counts);
    }

    [Theory]
    [InlineData(1f, 0f)]
    [InlineData(1f, 6f)]
    [InlineData(2f, 0f)]
    [InlineData(2f, 6f)]
    public void Draw_StackedNineSlicesAtRandomOffsets_ShouldHaveNoSeam(float zoom, float spacing)
    {
        SeamCounts counts = SweepRandom(Dumping("raylib", $"random-z{zoom}-s{spacing}", Render), zoom, spacing);

        Check($"random, zoom {zoom}, spacing {spacing}", counts);
    }
}
