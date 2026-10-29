using EditorTabPlugin_XNA.Services;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Output tab says "Compiled render-target shader" for a shader that compiled, and does not say it
/// for one that fell back to its last good version (#7017).
/// </summary>
public class RenderTargetShaderReporterTests
{
    private const string Path = "/shaders/Blur.slang";

    [Fact]
    public void ACompiledShader_IsAnnounced()
    {
        List<string> output = new List<string>();
        RenderTargetShaderReporter reporter = new RenderTargetShaderReporter(output.Add);

        object? effect = reporter.ResolveAndReport(_ => new object(), Path);

        effect.ShouldNotBeNull();
        output.Count.ShouldBe(1);
        output[0].ShouldContain("Compiled render-target shader");
        output[0].ShouldContain(Path);
    }

    [Fact]
    public void AShaderThatFellBackToItsLastGoodVersion_ReportsTheError_AndIsNotAnnouncedAsCompiled()
    {
        List<string> output = new List<string>();
        RenderTargetShaderReporter reporter = new RenderTargetShaderReporter(output.Add);

        object? effect = reporter.ResolveAndReport(_ =>
        {
            reporter.ReportError("syntax error on line 3");
            return new object();
        }, Path);

        effect.ShouldNotBeNull();
        output.ShouldBe(new[] { "syntax error on line 3" });
    }

    [Fact]
    public void ACompileAfterAFallback_IsAnnouncedAgain()
    {
        List<string> output = new List<string>();
        RenderTargetShaderReporter reporter = new RenderTargetShaderReporter(output.Add);
        reporter.ResolveAndReport(_ =>
        {
            reporter.ReportError("syntax error on line 3");
            return new object();
        }, Path);
        output.Clear();

        reporter.ResolveAndReport(_ => new object(), Path);

        output.Count.ShouldBe(1);
        output[0].ShouldContain("Compiled render-target shader");
    }

    [Fact]
    public void AResolverThatReturnsNothing_AnnouncesNothing()
    {
        List<string> output = new List<string>();
        RenderTargetShaderReporter reporter = new RenderTargetShaderReporter(output.Add);

        reporter.ResolveAndReport(null, Path).ShouldBeNull();

        output.ShouldBeEmpty();
    }
}
