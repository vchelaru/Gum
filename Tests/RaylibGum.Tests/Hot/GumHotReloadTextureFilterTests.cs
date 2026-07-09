using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace RaylibGum.Tests.Hot;

/// <summary>
/// Drives <see cref="GumHotReloadManager.PerformReload"/> end-to-end on Raylib to cover the
/// platform-divergent branch of <c>GumService.ApplyProjectTextureFilter</c> it calls
/// (documented as diverging at <c>GumHotReloadManager.cs:236-239</c>): on raylib there is no
/// global sampler state, so a reload must push the project's <c>TextureFilter</c> into
/// <see cref="ContentLoader.DefaultTextureFilter"/> instead of the XNALIKE
/// <c>Renderer.TextureFilter</c> path. Issue #3571.
/// </summary>
public class GumHotReloadTextureFilterTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static string CreateSourceDirectory()
    {
        string sourceDirectory = Path.Combine(
            Path.GetTempPath(), "GumHotReloadTextureFilterTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDirectory);
        return sourceDirectory;
    }

    [Fact]
    public void PerformReload_WhenProjectTextureFilterIsLinear_SetsContentLoaderDefaultTextureFilterToBilinear()
    {
        Raylib_cs.TextureFilter savedDefaultFilter = ContentLoader.DefaultTextureFilter;
        string sourceDirectory = CreateSourceDirectory();

        try
        {
            ContentLoader.DefaultTextureFilter = Raylib_cs.TextureFilter.Point;

            string gumxPath = Path.Combine(sourceDirectory, "Proj.gumx");
            GumProjectSave project = new GumProjectSave { TextureFilter = "Linear" };
            project.Save(gumxPath, saveElements: false);

            GumHotReloadManager manager = new GumHotReloadManager();
            manager.Start(gumxPath);
            // Release the OS watcher immediately; Start has already recorded the source path.
            manager.Stop();

            manager.PerformReload(Array.Empty<GraphicalUiElement>());

            ContentLoader.DefaultTextureFilter.ShouldBe(Raylib_cs.TextureFilter.Bilinear);
        }
        finally
        {
            ContentLoader.DefaultTextureFilter = savedDefaultFilter;
            try { Directory.Delete(sourceDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void PerformReload_WhenProjectTextureFilterIsNotLinear_SetsContentLoaderDefaultTextureFilterToPoint()
    {
        Raylib_cs.TextureFilter savedDefaultFilter = ContentLoader.DefaultTextureFilter;
        string sourceDirectory = CreateSourceDirectory();

        try
        {
            // Start from the opposite filter so the assertion actually proves the reload changed it.
            ContentLoader.DefaultTextureFilter = Raylib_cs.TextureFilter.Bilinear;

            string gumxPath = Path.Combine(sourceDirectory, "Proj.gumx");
            GumProjectSave project = new GumProjectSave { TextureFilter = "Point" };
            project.Save(gumxPath, saveElements: false);

            GumHotReloadManager manager = new GumHotReloadManager();
            manager.Start(gumxPath);
            manager.Stop();

            manager.PerformReload(Array.Empty<GraphicalUiElement>());

            ContentLoader.DefaultTextureFilter.ShouldBe(Raylib_cs.TextureFilter.Point);
        }
        finally
        {
            ContentLoader.DefaultTextureFilter = savedDefaultFilter;
            try { Directory.Delete(sourceDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }
}
