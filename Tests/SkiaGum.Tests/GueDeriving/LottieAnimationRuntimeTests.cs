using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

public class LottieAnimationRuntimeTests : BaseTestClass
{
    public LottieAnimationRuntimeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void SourceFile_SetToFileThenNull_LoadsThenClearsAnimation()
    {
        string path = Path.Combine(Path.GetTempPath(), "GumLottieRuntimeTest_" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{\"v\":\"5.5.2\",\"fr\":30,\"ip\":0,\"op\":30,\"w\":10,\"h\":10,\"nm\":\"t\",\"ddd\":0,\"assets\":[],\"layers\":[]}");
        LoaderManager.Self.ContentLoader = new RenderingLibrary.Content.ContentLoader();

        try
        {
            LottieAnimationRuntime sut = new LottieAnimationRuntime();
            sut.SourceFile = path;
            sut.Animation.ShouldNotBeNull();
            sut.SourceFile.ShouldBe(path);

            sut.SourceFile = null;

            sut.Animation.ShouldBeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
