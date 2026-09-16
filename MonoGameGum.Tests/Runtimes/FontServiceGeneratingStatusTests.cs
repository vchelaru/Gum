using Gum.GueDeriving;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// #4799: while the tool's load-time bulk pass is still generating a font, the synchronous
// IRuntimeFontService answers Generating. Font resolution must then leave the font unresolved for
// now - no placeholder cached under the font's key, no "unresolvable" blacklisting, no error - so
// the next resolution (after the tool reloads content) picks the finished files up.
//
// Font names are GUID-suffixed so nothing in the process-wide LoaderManager cache or the embedded
// Arial-18 resource can short-circuit resolution before it reaches the service under test.
public class FontServiceGeneratingStatusTests : BaseTestClass
{
    [Fact]
    public void GetAndCreateFontIfNecessary_WhenFontServiceReportsGenerating_ShouldAskAgainOnTheNextResolution()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        IRuntimeFontService? savedService = CustomSetPropertyOnRenderable.FontService;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
            Mock<IRuntimeFontService> fontService = NewGeneratingFontService();
            CustomSetPropertyOnRenderable.FontService = fontService.Object;
            string fontName = UniqueFontName();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 18;

            textRuntime.Text = $"AA [Font={fontName}]BB[/Font] CC";
            textRuntime.Text = $"DD [Font={fontName}]EE[/Font] FF";

            fontService.Invocations
                .Count(invocation => invocation.Method.Name == nameof(IRuntimeFontService.CreateFontIfNecessary)
                    && ((BmfcSave)invocation.Arguments[0]).FontName == fontName)
                .ShouldBe(2, "a font still being generated must not be cached as resolved");
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            CustomSetPropertyOnRenderable.FontService = savedService;
        }
    }

    [Fact]
    public void GetOrCreateBakedFont_WhenFontServiceReportsGenerating_ShouldNeitherReportAnErrorNorBlacklistTheFont()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        IRuntimeFontService? savedService = CustomSetPropertyOnRenderable.FontService;
        List<string> capturedMessages = new();
        void Handler(string message) => capturedMessages.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
            Mock<IRuntimeFontService> fontService = NewGeneratingFontService();
            CustomSetPropertyOnRenderable.FontService = fontService.Object;
            string fontName = UniqueFontName();

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 12;
            textRuntime.Font = fontName;
            // Leave and come back: a blacklisted font key short-circuits before the service is asked.
            textRuntime.Font = "Arial";
            textRuntime.Font = fontName;

            capturedMessages.ShouldBeEmpty();
            fontService.Invocations
                .Count(invocation => invocation.Method.Name == nameof(IRuntimeFontService.CreateFontIfNecessary)
                    && ((BmfcSave)invocation.Arguments[0]).FontName == fontName)
                .ShouldBe(2, "a font still being generated must not be treated as unresolvable");
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            CustomSetPropertyOnRenderable.FontService = savedService;
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
    }

    private static string UniqueFontName() => "GumFontGeneratingTest_" + Guid.NewGuid().ToString("N");

    private static Mock<IRuntimeFontService> NewGeneratingFontService()
    {
        Mock<IRuntimeFontService> mock = new();
        mock.Setup(x => x.AbsoluteFontCacheFolder).Returns("C:/FontCache/");
        mock.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>())).Returns(FontFileStatus.Generating);
        return mock;
    }
}
