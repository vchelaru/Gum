using Gum.Forms.Controls;
using Gum.Wireframe;
using GumSamples.Benchmark;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Performance;

// Spike probe (#8099): counts NineSlices in the benchmark scene that follow another NineSlice of the same
// texture in draw order with no drawing renderable or clip change between them.
public class NineSliceRunProbeTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost(1280, 720);

    public NineSliceRunProbeTests(ITestOutputHelper output) => _output = output;

    public override void Dispose()
    {
        _host.Dispose();
        base.Dispose();
    }

    object? _previousKey;
    int _nineSlices;
    int _consecutive;
    int _runs;
    readonly System.Collections.Generic.List<string> _breakers = new();

    [Fact]
    public void CountConsecutiveNineSlices()
    {
        string old = ToolsUtilities.FileManager.RelativeDirectory;
        ToolsUtilities.FileManager.RelativeDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../../Samples/raylib/resources/"));
        var screen = new BenchmarkScreen();
        ToolsUtilities.FileManager.RelativeDirectory = old;
        screen.AddToRoot();
        _host.Update();
        _host.Draw();
        _host.Update();
        _host.Draw();

        Walk(screen.Visual);
        _output.WriteLine($"NineSlices drawn: {_nineSlices}; following a same-texture NineSlice directly: {_consecutive}; runs of 2+: {_runs}");
        foreach (var g in _breakers.GroupBy(b => b).OrderByDescending(g => g.Count()))
        {
            _output.WriteLine($"  next after a NineSlice: {g.Key} x{g.Count()}");
        }
    }

    bool _lastWasNineSlice;
    bool _inRun;

    void Walk(IRenderableIpso renderable)
    {
        if (!renderable.Visible)
        {
            return;
        }
        if (renderable.ClipsChildren)
        {
            Break("clip");
        }
        IRenderableIpso inner = renderable is GraphicalUiElement gue && gue.RenderableComponent is IRenderableIpso c ? c : renderable;
        if (inner is NineSlice nine)
        {
            _nineSlices++;
            object? key = (object?)nine.Texture ?? nine.Image;
            if (_lastWasNineSlice && key != null && ReferenceEquals(key, _previousKey))
            {
                _consecutive++;
                if (!_inRun) { _runs++; _inRun = true; }
            }
            else
            {
                _inRun = false;
            }
            _lastWasNineSlice = true;
            _previousKey = key;
        }
        else if (inner is not InvisibleRenderable && !(inner is Text t && string.IsNullOrEmpty(t.RawText)))
        {
            Break(inner.GetType().Name);
        }
        foreach (var child in renderable.Children)
        {
            Walk(child);
        }
        if (renderable.ClipsChildren)
        {
            Break("clip end");
        }
    }

    void Break(string why)
    {
        if (_lastWasNineSlice)
        {
            _breakers.Add(why);
        }
        _lastWasNineSlice = false;
        _inRun = false;
        _previousKey = null;
    }
}
