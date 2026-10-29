using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;
using System;
using System.IO;
using Xunit;

namespace SkiaGum.Tests;

/// <summary>
/// Initializing Gum on Skia defaults <see cref="MissingFileBehavior"/> to ThrowException, the same as
/// MonoGame, KNI, FNA and raylib (#6889).
/// </summary>
public class SkiaMissingFileBehaviorDefaultTests : BaseTestClass
{
    [Fact]
    public void Initialize_DefaultsMissingFileBehaviorToThrowException()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));

        Gum.GumService.Default.Initialize(surface.Canvas, 200, 100);

        GraphicalUiElement.MissingFileBehavior.ShouldBe(MissingFileBehavior.ThrowException);
    }

    [Fact]
    public void Initialize_ThenSpriteSourceFileMissing_ThrowsFileNotFound()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        Gum.GumService.Default.Initialize(surface.Canvas, 200, 100);
        string missing = Path.Combine(Path.GetTempPath(), "GumSkiaDefault_" + Guid.NewGuid().ToString("N"), "missing.png");

        Exception thrown = Should.Throw<Exception>(() => new SpriteRuntime { SourceFileName = missing });

        for (Exception? e = thrown; e != null; e = e.InnerException)
        {
            if (e is FileNotFoundException) { return; }
        }
        thrown.ShouldBeOfType<FileNotFoundException>();
    }
}
