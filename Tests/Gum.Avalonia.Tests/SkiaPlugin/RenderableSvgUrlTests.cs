using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Renderables;
using Xunit;

namespace Gum.Avalonia.Tests.SkiaPlugin;

// The tool's editor canvas draws Svg instances through RenderableSvg (Svg.Skia). A SourceFile
// URL has no .svg suffix requirement: the content is what gets parsed.
public class RenderableSvgUrlTests
{
    private const string TwoByOneSvg =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 100">
          <rect x="0" y="0" width="200" height="100" fill="#ff0000" />
        </svg>
        """;

    [Fact]
    public async Task SourceFile_UrlWithoutSvgExtension_DownloadsAndLoadsDocument()
    {
        using HttpListener listener = StartListener(out string baseUrl);
        Task serving = ServeOnceAsync(listener, TwoByOneSvg);
        try
        {
            RenderableSvg svg = new RenderableSvg();

            svg.SourceFile = $"{baseUrl}wikipedia/commons/0/0d/Nintendo?utm_source=commons";

            // Without a request the server never completes; fail fast rather than hang the run.
            (await Task.WhenAny(serving, Task.Delay(TimeSpan.FromSeconds(5)))).ShouldBe(serving);
            svg.AspectRatio.ShouldBe(2f, tolerance: 0.001f);
        }
        finally
        {
            LoaderManager.Self.DisposeAndClear();
        }
    }

    [Fact]
    public void SourceFile_UrlThatCannotBeReached_LeavesTheDocumentEmpty()
    {
        using HttpListener listener = StartListener(out string baseUrl);
        listener.Close();

        RenderableSvg svg = new RenderableSvg();

        svg.SourceFile = $"{baseUrl}missing";

        svg.AspectRatio.ShouldBe(1f);
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
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        context.Response.ContentType = "image/svg+xml";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }
}
