// #7826 research: how many sprite draws in a row would one SKCanvas.DrawAtlas call cover on a real screen.
// Walks the renderables in the order SkiaGum's Renderer.DrawList draws them (layers, Visible, children, clips,
// render targets) and counts runs of consecutive image draws from the same image. Run with --batch-stats <file>.
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Text;

internal static class BatchStats
{
    // A run ends at anything else that draws, at a clip push or pop, and at a render target.
    private sealed class Runs
    {
        public readonly List<int> ByImage = new();
        public readonly List<int> ByImageAndRsxform = new();
        // Runs counted in renderables (a NineSlice is one) rather than image draws.
        public readonly List<int> ByObject = new();
        int _objectRun;
        object? _image;
        int _imageRun;
        int _rsxformRun;
        public int ImageDraws;
        public int Breaks;
        public readonly Dictionary<string, int> BreakKinds = new();

        public void Image(object image, int draws, bool rsxformExpressible, bool newObject = true)
        {
            if (!ReferenceEquals(image, _image))
            {
                Close();
                _image = image;
            }
            ImageDraws += draws;
            if (newObject)
            {
                _objectRun++;
            }
            _imageRun += draws;
            if (rsxformExpressible)
            {
                _rsxformRun += draws;
            }
            else
            {
                // Its own canvas matrix: a run of its own.
                CloseRsxform();
                ByImageAndRsxform.Add(draws);
            }
        }

        public void Break(string kind)
        {
            Breaks++;
            BreakKinds[kind] = BreakKinds.GetValueOrDefault(kind) + 1;
            Close();
            _image = null;
        }

        void CloseRsxform()
        {
            if (_rsxformRun > 0)
            {
                ByImageAndRsxform.Add(_rsxformRun);
            }
            _rsxformRun = 0;
        }

        public void Close()
        {
            if (_imageRun > 0)
            {
                ByImage.Add(_imageRun);
            }
            _imageRun = 0;
            if (_objectRun > 0)
            {
                ByObject.Add(_objectRun);
            }
            _objectRun = 0;
            CloseRsxform();
        }
    }

    public static void Append(string path, string screenName)
    {
        var runs = new Runs();
        foreach (Layer layer in SystemManagers.Default.Renderer.Layers)
        {
            Walk(layer.Renderables, runs);
        }
        runs.Close();
        var line = new StringBuilder($"{screenName,-14} imageDraws={runs.ImageDraws,4} | same-image draw runs={runs.ByImage.Count,3} {Describe(runs.ByImage)}" +
            $" | same-image renderable runs {Describe(runs.ByObject)} | RSXform-expressible draw runs={runs.ByImageAndRsxform.Count,3} {Describe(runs.ByImageAndRsxform)} | breaks:");
        foreach (KeyValuePair<string, int> kind in runs.BreakKinds.OrderByDescending(pair => pair.Value).Take(4))
        {
            line.Append($" {kind.Key}={kind.Value}");
        }
        File.AppendAllText(path, line + Environment.NewLine);
    }

    private static string Describe(List<int> runs)
    {
        if (runs.Count == 0)
        {
            return "max=0 mean=0 runs>1=0";
        }
        return $"max={runs.Max(),3} mean={runs.Average(),5:F2} runs>1={runs.Count(run => run > 1),3} drawsInRuns>1={runs.Where(run => run > 1).Sum(),4}";
    }

    private static void Walk(IList<IRenderableIpso> renderables, Runs runs)
    {
        foreach (IRenderableIpso renderable in renderables)
        {
            if (!renderable.Visible)
            {
                continue;
            }
            if (renderable.IsRenderTarget)
            {
                runs.Break("renderTarget");
                continue;
            }
            if (renderable.ClipsChildren)
            {
                runs.Break("clip");
            }
            Classify(renderable, runs);
            Walk(renderable.Children, runs);
            if (renderable.ClipsChildren)
            {
                runs.Break("clip");
            }
        }
    }

    private static void Classify(IRenderableIpso renderable, Runs runs)
    {
        // Layers hold GraphicalUiElements, whose Render draws the renderable they wrap.
        object? drawn = renderable is GraphicalUiElement element ? element.RenderableComponent : renderable;
        switch (drawn)
        {
            case null:
            case InvisibleRenderable:
                return;
            case Sprite sprite when sprite.RenderTargetTextureSource == null && sprite.Blend == null:
                object? image = (object?)sprite.Image ?? sprite.Texture;
                if (image == null)
                {
                    return;
                }
                runs.Image(image, 1, IsRsxformExpressible(sprite));
                return;
            case NineSlice nineSlice when nineSlice.Blend == null:
                object? nineImage = (object?)nineSlice.Image ?? nineSlice.Texture;
                if (nineImage == null)
                {
                    return;
                }
                // Nine DrawImage calls (NineSlice.DrawMiddleSection). The four corners keep the texture's scale and can
                // share an RSXform run; the four edges and the center stretch one or both axes, so each needs its own
                // canvas matrix.
                runs.Image(nineImage, 4, true);
                for (int slice = 0; slice < 5; slice++)
                {
                    runs.Image(nineImage, 1, false, newObject: false);
                }
                return;
            default:
                runs.Break(drawn.GetType().Name);
                return;
        }
    }

    // Rotation and uniform scale only: no flip (swapped source edges, FlipDiagonal) and equal x and y scale.
    private static bool IsRsxformExpressible(Sprite sprite)
    {
        if (sprite.FlipDiagonal)
        {
            return false;
        }
        float sourceWidth;
        float sourceHeight;
        if (sprite.EffectiveRectangle is { } source)
        {
            if (source.Width < 0 || source.Height < 0)
            {
                return false;
            }
            sourceWidth = source.Width;
            sourceHeight = source.Height;
        }
        else
        {
            SKSizeI size = sprite.Image != null ? new SKSizeI(sprite.Image.Width, sprite.Image.Height)
                : new SKSizeI(sprite.Texture!.Width, sprite.Texture.Height);
            sourceWidth = size.Width;
            sourceHeight = size.Height;
        }
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return false;
        }
        float scaleX = sprite.Width / sourceWidth;
        float scaleY = sprite.Height / sourceHeight;
        return MathF.Abs(scaleX - scaleY) <= 1e-3f * MathF.Max(scaleX, scaleY);
    }
}
