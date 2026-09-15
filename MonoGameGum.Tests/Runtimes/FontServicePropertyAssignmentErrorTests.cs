using Gum.GueDeriving;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// #4732: a wired IRuntimeFontService (the Gum tool always has one) that throws from
// CreateFontIfNecessary was swallowed by a bare catch { } on both FontService call sites (the main
// GetOrCreateBakedFont path and the inline BBCode-run GetAndCreateFontIfNecessary path), so a
// missing bmfont, a bad font name, or a path-resolution bug produced no diagnostic -- only the
// generic "No usable font could be resolved" fallback, which blames the wrong tier. The in-memory
// creator's matching catch sites already report (#4464); this pins the FontService sites doing the
// same.
//
// Font names are GUID-suffixed so nothing in the process-wide LoaderManager cache or the embedded
// Arial-18 resource can short-circuit resolution before it reaches the service under test.
public class FontServicePropertyAssignmentErrorTests : BaseTestClass
{
    private const string SimulatedFailureMessage = "Simulated bmfont generation failure.";

    [Fact]
    public void GetOrCreateBakedFont_WhenFontServiceThrows_ShouldReportTheExceptionThroughPropertyAssignmentError()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        IRuntimeFontService? savedService = CustomSetPropertyOnRenderable.FontService;
        List<string> capturedMessages = new();
        void Handler(string message) => capturedMessages.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
            CustomSetPropertyOnRenderable.FontService = NewThrowingFontService();

            TextRuntime textRuntime = new();
            textRuntime.Font = UniqueFontName();
            textRuntime.FontSize = 12;

            // The generic "nothing resolved" message (#4553) still fires afterward; the service's
            // own exception must be surfaced as well, not hidden behind it.
            capturedMessages.ShouldContain(message => message.Contains(SimulatedFailureMessage));
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            CustomSetPropertyOnRenderable.FontService = savedService;
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
    }

    [Fact]
    public void GetAndCreateFontIfNecessary_WhenFontServiceThrows_ShouldReportTheExceptionThroughPropertyAssignmentError()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        IRuntimeFontService? savedService = CustomSetPropertyOnRenderable.FontService;
        List<string> capturedMessages = new();
        void Handler(string message) => capturedMessages.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
            CustomSetPropertyOnRenderable.FontService = NewThrowingFontService();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 18;
            // Arial 18 resolves from the embedded resource without consulting the service, so any
            // failure below is isolated to the inline [Font=...] run.
            capturedMessages.Clear();

            textRuntime.Text = $"AA [Font={UniqueFontName()}]BB[/Font] CC";

            capturedMessages.ShouldContain(message => message.Contains(SimulatedFailureMessage));
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            CustomSetPropertyOnRenderable.FontService = savedService;
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
    }

    private static string UniqueFontName() => "GumFontServiceErrorTest_" + Guid.NewGuid().ToString("N");

    private static IRuntimeFontService NewThrowingFontService()
    {
        Mock<IRuntimeFontService> mock = new();
        mock.Setup(x => x.AbsoluteFontCacheFolder).Returns("C:/FontCache/");
        mock.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Throws(new InvalidOperationException(SimulatedFailureMessage));
        return mock.Object;
    }
}
