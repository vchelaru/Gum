using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Content;
using System;
using System.IO;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

public class SvgRuntimeTests
{
    public SvgRuntimeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void SourceFile_SetToNullAfterFile_ClearsTexture()
    {
        string svgPath = Path.Combine(Path.GetTempPath(), "GumSvgRuntimeTest_" + Guid.NewGuid().ToString("N") + ".svg");
        File.WriteAllText(svgPath,
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\" fill=\"red\"/></svg>");
        IContentLoader? originalLoader = LoaderManager.Self.ContentLoader;
        LoaderManager.Self.ContentLoader = new EmbeddedResourceContentLoader();

        try
        {
            SvgRuntime sut = new SvgRuntime();
            sut.SourceFile = svgPath;
            sut.Texture.ShouldNotBeNull();

            sut.SourceFile = null;

            sut.Texture.ShouldBeNull();
        }
        finally
        {
            LoaderManager.Self.ContentLoader = originalLoader;
            File.Delete(svgPath);
        }
    }
}
