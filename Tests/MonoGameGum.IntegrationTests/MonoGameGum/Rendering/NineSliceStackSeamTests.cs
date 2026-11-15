using System;
using System.Collections.Generic;
using System.IO;
using CrossRuntimeTests;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using static CrossRuntimeTests.NineSliceSeamSweep;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// #8072: a ListBox moved by a scrollbar stacks Forms NineSlices at fractional positions, and adjacent items must show
/// no seam (no gap, hairline, lost outline or overlap). The sweeps and the counting are shared with raylib and Skia
/// (<see cref="NineSliceSeamSweep"/>); this renders them on the XNA-family NineSlice (MonoGame, KNI, FNA).
/// Set GUM_SEAM_DUMP to a folder to write the first failing frame of each case as a magnified PNG.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class NineSliceStackSeamTests : BaseTestClass
{
    sealed class Scene : SharedGameLease
    {
        public GumService GumService { get; } = new GumService();

        readonly Dictionary<string, Texture2D> _textures = new();
        readonly Dictionary<(int, int), RenderTarget2D> _targets = new();

        protected override void OnInitialize() => GumService.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            foreach (RenderTarget2D target in _targets.Values)
            {
                target.Dispose();
            }
            foreach (Texture2D texture in _textures.Values)
            {
                texture.Dispose();
            }
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }

        Texture2D TextureFor(TextureSpec spec)
        {
            if (spec.Source != null)
            {
                return SystemManagers.Default.GetOrLoadEmbeddedTexture2d("UISpriteSheet.png");
            }
            if (!_textures.TryGetValue(spec.Name, out Texture2D? texture))
            {
                int size = spec.Border * 3;
                texture = new Texture2D(GraphicsDevice, size, size);
                Color[] data = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Rgb c = spec.SyntheticTexel(x, y);
                        data[(y * size) + x] = new Color(c.R, c.G, c.B, (byte)255);
                    }
                }
                texture.SetData(data);
                _textures[spec.Name] = texture;
            }
            return texture;
        }

        public Frame Render(TextureSpec spec, Layout layout)
        {
            SystemManagers managers = SystemManagers.Default;
            Renderer renderer = managers.Renderer;
            int width = layout.PixelWidth;
            int height = layout.PixelHeight;
            if (!_targets.TryGetValue((width, height), out RenderTarget2D? capture))
            {
                capture = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0,
                    RenderTargetUsage.PreserveContents);
                _targets[(width, height)] = capture;
            }

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

            renderer.Camera.Zoom = layout.Zoom;
            stack.AddToManagers(managers, null);
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    GraphicsDevice.SetRenderTarget(capture);
                    GraphicsDevice.Clear(new Color(Background.R, Background.G, Background.B));
                    renderer.Draw(managers);
                }
                GraphicsDevice.SetRenderTarget(null);

                Color[] data = new Color[width * height];
                capture.GetData(data);
                Rgb[] pixels = new Rgb[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    pixels[i] = new Rgb(data[i].R, data[i].G, data[i].B);
                }
                return new Frame(width, height, pixels);
            }
            finally
            {
                stack.RemoveFromManagers();
                renderer.Camera.Zoom = 1f;
            }
        }
    }

    static void Check(string name, SeamCounts counts)
    {
        string? folder = Environment.GetEnvironmentVariable("GUM_SEAM_DUMP");
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "sweep-summary.txt"), "xna " + name + ": " + counts + Environment.NewLine);
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
        using Scene scene = new();
        scene.RunOneFrame();
        string name = $"step 0.0126123, zoom {zoom}, spacing {spacing}";

        SeamCounts counts = SweepSteps(Dumping("xna", $"step-z{zoom}-s{spacing}", scene.Render), zoom, spacing);

        Check(name, counts);
    }

    [Theory]
    [InlineData(1f, 0f)]
    [InlineData(1f, 6f)]
    [InlineData(2f, 0f)]
    [InlineData(2f, 6f)]
    public void Draw_StackedNineSlicesAtRandomOffsets_ShouldHaveNoSeam(float zoom, float spacing)
    {
        using Scene scene = new();
        scene.RunOneFrame();
        string name = $"random, zoom {zoom}, spacing {spacing}";

        SeamCounts counts = SweepRandom(Dumping("xna", $"random-z{zoom}-s{spacing}", scene.Render), zoom, spacing);

        Check(name, counts);
    }
}
