using Apos.Shapes;
using Gum.DataTypes;
using Gum.GueDeriving;
using GumRuntime;
using MonoGameAndGum.Content;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Content;
using Shouldly;
using System.Net;
using System.Text;

namespace MonoGameGum.Shapes.Tests;

// Issue #4506 — Apos.Shapes-backed SVG runtime for the XNA-like backends. These tests run
// headlessly: ShapeSvg parsing is pure geometry with no GraphicsDevice, so everything except the
// actual DrawSvg call is exercisable here. The draw itself is covered by the manual sample pass.
public class SvgRuntimeTests
{
    // 2:1 viewBox so an aspect-ratio assertion can't accidentally pass on a square default.
    private const string TwoByOneSvg =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 100">
          <rect x="0" y="0" width="200" height="100" fill="#ff0000" />
        </svg>
        """;

    private static string WriteTempSvg(string markup)
    {
        var path = Path.Combine(Path.GetTempPath(), $"gum-svg-test-{Guid.NewGuid():N}.svg");
        File.WriteAllText(path, markup);
        return path;
    }

    [Fact]
    public void Load_SamePathTwice_ReturnsCachedInstance()
    {
        var path = WriteTempSvg(TwoByOneSvg);
        try
        {
            var first = ShapeSvgLoader.Load(path);
            var second = ShapeSvgLoader.Load(path);

            first.ShouldNotBeNull();
            second.ShouldBeSameAs(first);
        }
        finally
        {
            LoaderManager.Self.DisposeAndClear();
            File.Delete(path);
        }
    }

    // GumService.Uninitialize() tears the cache down via LoaderManager.Self.DisposeAndClear().
    // Pinning that here so a future move off LoaderManager to a private static dictionary — which
    // DisposeAndClear would no longer reach — fails loudly instead of leaking documents across an
    // Uninitialize/Initialize cycle.
    [Fact]
    public void Load_AfterDisposeAndClear_ReloadsRatherThanReturningStale()
    {
        var path = WriteTempSvg(TwoByOneSvg);
        try
        {
            var first = ShapeSvgLoader.Load(path);

            LoaderManager.Self.DisposeAndClear();

            var second = ShapeSvgLoader.Load(path);

            second.ShouldNotBeNull();
            second.ShouldNotBeSameAs(first);
        }
        finally
        {
            LoaderManager.Self.DisposeAndClear();
            File.Delete(path);
        }
    }

    // Wikimedia-style URL: no .svg suffix, with a query string.
    [Fact]
    public async Task Load_UrlWithoutSvgExtension_DownloadsAndParsesDocument()
    {
        using HttpListener listener = StartListener(out string baseUrl);
        Task serving = ServeOnceAsync(listener, TwoByOneSvg);
        try
        {
            ShapeSvg? svg = ShapeSvgLoader.Load($"{baseUrl}wikipedia/commons/0/0d/Nintendo?utm_source=commons");

            // Without a request the server never completes; fail fast rather than hang the run.
            (await Task.WhenAny(serving, Task.Delay(TimeSpan.FromSeconds(5)))).ShouldBe(serving);
            svg.ShouldNotBeNull();
            svg.Width.ShouldBe(200f, tolerance: 0.001f);
        }
        finally
        {
            LoaderManager.Self.DisposeAndClear();
        }
    }

    [Fact]
    public void Load_UrlThatCannotBeReached_ReturnsNullRatherThanThrowing()
    {
        // Bind then release a port so nothing is listening on it.
        using HttpListener listener = StartListener(out string baseUrl);
        listener.Close();

        ShapeSvgLoader.Load($"{baseUrl}missing").ShouldBeNull();
    }

    private static HttpListener StartListener(out string baseUrl)
    {
        System.Net.Sockets.TcpListener probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        baseUrl = $"http://127.0.0.1:{port}/";
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(baseUrl);
        listener.Start();
        return listener;
    }

    private static async Task ServeOnceAsync(HttpListener listener, string body)
    {
        HttpListenerContext context = await listener.GetContextAsync();

        // Wikimedia answers 403 to a request with no User-Agent.
        if (string.IsNullOrEmpty(context.Request.UserAgent))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(body);
        context.Response.ContentType = "image/svg+xml";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    [Fact]
    public void Load_MissingFile_ReturnsNullRatherThanThrowing()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"gum-svg-missing-{Guid.NewGuid():N}.svg");

        ShapeSvgLoader.Load(missing).ShouldBeNull();
    }

    // MaintainFileAspectRatio layout reads IRenderableIpso's IAspectRatio, so a wrong value here
    // silently mis-sizes every SVG that uses the runtime's default height units.
    [Fact]
    public void AspectRatio_MatchesViewBox_AndDefaultsToOneWithNoDocument()
    {
        var path = WriteTempSvg(TwoByOneSvg);
        try
        {
            var svg = new Svg();

            svg.AspectRatio.ShouldBe(1f);

            svg.Document = ShapeSvgLoader.Load(path);

            svg.AspectRatio.ShouldBe(2f, tolerance: 0.001f);
        }
        finally
        {
            LoaderManager.Self.DisposeAndClear();
            File.Delete(path);
        }
    }

    [Fact]
    public void SourceFile_SetToRealFile_PopulatesContainedDocument()
    {
        var path = WriteTempSvg(TwoByOneSvg);
        try
        {
            var runtime = new SvgRuntime();

            runtime.SourceFile = path;

            runtime.Document.ShouldNotBeNull();
        }
        finally
        {
            LoaderManager.Self.DisposeAndClear();
            File.Delete(path);
        }
    }

    [Fact]
    public void SourceFile_ShouldResizeMaintainFileAspectRatioHeight_AfterLayout()
    {
        string path = WriteTempSvg(TwoByOneSvg);
        try
        {
            SvgRuntime runtime = new SvgRuntime();
            runtime.AbsoluteHeight.ShouldBe(100);

            runtime.SourceFile = path;

            runtime.AbsoluteHeight.ShouldBe(50, tolerance: 0.01f);
        }
        finally
        {
            LoaderManager.Self.DisposeAndClear();
            File.Delete(path);
        }
    }

    [Fact]
    public void CreateGueForElement_ForSvgBaseType_ProducesSvgRuntime()
    {
        AposShapeRuntime.RegisterRuntimeTypes();

        ComponentSave elementSave = new ComponentSave { Name = "Svg" };
        var gue = ElementSaveExtensions.CreateGueForElement(elementSave);

        gue.ShouldBeOfType<SvgRuntime>();
    }

    // The Gum tool sets IsSvgRuntimeEnabled = false so its Svg preview stays on the Skia plugin's
    // Svg.Skia renderable. The factory must therefore decline at creation time (returning null so
    // CreateGueForElement falls through to CustomCreateGraphicalComponentFunc), NOT skip
    // registration - RegisterRuntimeTypes is a [ModuleInitializer] and has already run before any
    // host can set the flag.
    [Fact]
    public void CreateGueForElement_ForSvgBaseType_WhenSvgRuntimeDisabled_DoesNotProduceSvgRuntime()
    {
        AposShapeRuntime.IsSvgRuntimeEnabled = false;
        try
        {
            AposShapeRuntime.RegisterRuntimeTypes();

            ComponentSave elementSave = new ComponentSave { Name = "Svg" };
            var gue = ElementSaveExtensions.CreateGueForElement(elementSave);

            gue.ShouldNotBeOfType<SvgRuntime>();
        }
        finally
        {
            AposShapeRuntime.IsSvgRuntimeEnabled = true;
        }
    }

    [Fact]
    public void GetDefaultState_ForSvg_FollowsIsSvgRuntimeEnabled()
    {
        var resolve = AposShapeRuntime.CombineGetDefaultState(existing: null);

        resolve("Svg").ShouldNotBeNull();

        AposShapeRuntime.IsSvgRuntimeEnabled = false;
        try
        {
            // Null hands the type back to whatever resolver was already registered — the Skia
            // plugin's, in the tool.
            resolve("Svg").ShouldBeNull();
        }
        finally
        {
            AposShapeRuntime.IsSvgRuntimeEnabled = true;
        }
    }
}
