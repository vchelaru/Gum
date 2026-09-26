using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

// #4798: a .fnt that loads but whose page PNG can't be (missing, half-written, undecodable) used to
// swap in Sprite.InvalidTexture silently, so text rendered as a red X with nothing in the output
// window saying why. These pin that the page failure is reported through PropertyAssignmentError,
// once per page path.
//
// The .fnt is served through FileManager.CustomGetStreamFromFile and the page through a fake
// IContentLoader so no file or GraphicsDevice is needed; paths are GUID-suffixed so the process-wide
// dedupe set never carries state between tests.
public class BitmapFontPageLoadFailureTests : BaseTestClass
{
    private const string UndecodableMessage = "Simulated corrupt PNG.";

    private static string OnePageFntData(string pageFile) =>
$@"info face=""Test"" size=-14 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=16 base=12 scaleW=1 scaleH=1 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""{pageFile}""
chars count=0
";

    [Fact]
    public void Constructor_WhenPageTextureIsMissing_ShouldReportItThroughPropertyAssignmentError()
    {
        string pageFile = $"MissingPage_{Guid.NewGuid():N}.png";
        string fntFile = $"MissingPage_{Guid.NewGuid():N}.fnt";
        Mock<IContentLoader> loader = new();
        loader.Setup(x => x.LoadContent<Texture2D>(It.IsAny<string>())).Returns((Texture2D)null!);

        List<string> captured = RunCapturingErrors(loader.Object, fntFile, pageFile,
            () => new BitmapFont(fntFile, checkForShadowSibling: false));

        captured.Count.ShouldBe(1);
        captured[0].ShouldContain(pageFile);
        captured[0].ShouldContain(fntFile);
    }

    [Fact]
    public void Constructor_WhenPageTextureIsUndecodable_ShouldNotThrowAndShouldReportTheException()
    {
        string pageFile = $"CorruptPage_{Guid.NewGuid():N}.png";
        string fntFile = $"CorruptPage_{Guid.NewGuid():N}.fnt";
        Mock<IContentLoader> loader = new();
        loader.Setup(x => x.LoadContent<Texture2D>(It.IsAny<string>()))
            .Throws(new InvalidOperationException(UndecodableMessage));

        BitmapFont? font = null;
        List<string> captured = RunCapturingErrors(loader.Object, fntFile, pageFile,
            () => font = new BitmapFont(fntFile, checkForShadowSibling: false));

        font.ShouldNotBeNull();
        captured.Count.ShouldBe(1);
        captured[0].ShouldContain(pageFile);
        captured[0].ShouldContain(UndecodableMessage);
    }

    [Fact]
    public void Constructor_ShouldReportAMissingPageOnlyOnce()
    {
        string pageFile = $"DedupePage_{Guid.NewGuid():N}.png";
        string fntFile = $"DedupePage_{Guid.NewGuid():N}.fnt";
        Mock<IContentLoader> loader = new();
        loader.Setup(x => x.LoadContent<Texture2D>(It.IsAny<string>())).Returns((Texture2D)null!);

        List<string> captured = RunCapturingErrors(loader.Object, fntFile, pageFile, () =>
        {
            // The same font is re-resolved on every variable assignment; per-assignment reports
            // would flood the output window.
            new BitmapFont(fntFile, checkForShadowSibling: false);
            new BitmapFont(fntFile, checkForShadowSibling: false);
        });

        captured.Count.ShouldBe(1);
    }

    private static List<string> RunCapturingErrors(IContentLoader loader, string fntFile, string pageFile, Action act)
    {
        IContentLoader? savedLoader = LoaderManager.Self.ContentLoader;
        Func<string, System.IO.Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        List<string> captured = new();
        void Handler(string message) => captured.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            LoaderManager.Self.ContentLoader = loader;
            FileManager.CustomGetStreamFromFile = path =>
            {
                if (path.EndsWith(".fnt", StringComparison.OrdinalIgnoreCase))
                {
                    return new System.IO.MemoryStream(Encoding.UTF8.GetBytes(OnePageFntData(pageFile)));
                }
                throw new System.IO.FileNotFoundException(path);
            };

            act();
        }
        finally
        {
            LoaderManager.Self.ContentLoader = savedLoader;
            FileManager.CustomGetStreamFromFile = savedHook;
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
        return captured;
    }
}
