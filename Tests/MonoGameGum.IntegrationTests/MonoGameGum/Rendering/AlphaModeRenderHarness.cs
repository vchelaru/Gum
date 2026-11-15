using System;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Shared setup for the straight-alpha vs premultiplied-alpha pixel tests (#7585): renders one element
/// over an opaque backdrop on a real GL device and reads the pixels back. The texture is written to a PNG
/// as straight alpha and loaded through <see cref="LoaderManager"/>, the way a host loads it, so a
/// premultiplied pipeline gets premultiplied texels from the loader itself.
/// </summary>
public static class AlphaModeRenderHarness
{
    public const int Width = 96;
    public const int Height = 48;

    public static readonly XnaColor Backdrop = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);

    /// <summary>The kinds of draw the tests run: plain, color tinted, and half alpha.</summary>
    public enum Variant { Plain, Tint, HalfAlpha }

    public static readonly XnaColor TintColor = new XnaColor((byte)255, (byte)128, (byte)64, (byte)255);
    public const int HalfAlphaValue = 128;

    public static void Apply(Variant variant, Action<XnaColor> setColor, Action<int> setAlpha)
    {
        if (variant == Variant.Tint)
        {
            setColor(TintColor);
        }
        else if (variant == Variant.HalfAlpha)
        {
            setAlpha(HalfAlphaValue);
        }
    }

    /// <summary>
    /// The composite over <see cref="Backdrop"/> worked out by hand from a straight-alpha texel:
    /// coverage = texelAlpha * elementAlpha, color = texel * tint, result = color * coverage + backdrop * (1 - coverage).
    /// </summary>
    public static (float R, float G, float B) Expected(XnaColor straightTexel, Variant variant)
    {
        XnaColor tint = variant == Variant.Tint ? TintColor : XnaColor.White;
        float alpha = variant == Variant.HalfAlpha ? HalfAlphaValue / 255f : 1f;
        float coverage = straightTexel.A / 255f * alpha;
        float r = straightTexel.R * (tint.R / 255f) * coverage + Backdrop.R * (1 - coverage);
        float g = straightTexel.G * (tint.G / 255f) * coverage + Backdrop.G * (1 - coverage);
        float b = straightTexel.B * (tint.B / 255f) * coverage + Backdrop.B * (1 - coverage);
        return (r, g, b);
    }

    public static int MaxChannelDifference(XnaColor pixel, (float R, float G, float B) expected) =>
        (int)Math.Round(Math.Max(Math.Abs(pixel.R - expected.R),
            Math.Max(Math.Abs(pixel.G - expected.G), Math.Abs(pixel.B - expected.B))));

    public static int MaxChannelDifference(XnaColor[] a, XnaColor[] b)
    {
        int max = 0;
        for (int i = 0; i < a.Length; i++)
        {
            max = Math.Max(max, Math.Abs(a[i].R - b[i].R));
            max = Math.Max(max, Math.Abs(a[i].G - b[i].G));
            max = Math.Max(max, Math.Abs(a[i].B - b[i].B));
        }
        return max;
    }

    public static XnaColor[] Render(bool premultiplied, int textureWidth, int textureHeight, XnaColor[] straightTexels,
        Func<Texture2D, GraphicalUiElement> create)
    {
        using MinimalGame game = new(premultiplied);
        game.RunOneFrame();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        string pngPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".png");
        ContainerRuntime root = new();
        try
        {
            using (Texture2D source = new(gd, textureWidth, textureHeight, false, SurfaceFormat.Color))
            {
                source.SetData(straightTexels);
                using System.IO.FileStream fs = System.IO.File.Create(pngPath);
                source.SaveAsPng(fs, textureWidth, textureHeight);
            }
            Texture2D loaded = LoaderManager.Self.LoadContent<Texture2D>(pngPath)!;

            root.Width = Width;
            root.Height = Height;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest opaque solid fill.
            root.AddChild(new ColoredRectangleRuntime { Width = Width, Height = Height, Color = Backdrop });
#pragma warning restore CS0618
            root.AddChild(create(loaded));
            root.AddToManagers(managers, null);
            root.UpdateLayout();

            using RenderTarget2D target = new(gd, Width, Height, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            for (int i = 0; i < 2; i++)
            {
                gd.SetRenderTarget(target);
                gd.Clear(XnaColor.Black);
                managers.Renderer.Draw(managers);
            }
            gd.SetRenderTarget(null);

            XnaColor[] pixels = new XnaColor[Width * Height];
            target.GetData(pixels);
            return pixels;
        }
        finally
        {
            root.RemoveFromManagers();
            try { System.IO.File.Delete(pngPath); } catch { /* best effort */ }
        }
    }

    private sealed class MinimalGame : SharedGameLease
    {
        private readonly bool _premultiplied;

        public MinimalGame(bool premultiplied) : base(Width, Height)
        {
            _premultiplied = premultiplied;
        }

        protected override void OnInitialize()
        {
            Renderer.UsePremultipliedAlpha = _premultiplied;
            Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);
        }

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
            Renderer.UsePremultipliedAlpha = false;
        }
    }
}
