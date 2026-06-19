using Gum.DataTypes;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
// Alias to the non-obsolete base type so the test doesn't reference the
// [Obsolete] MonoGameGum.GumService shim (which unqualified GumService would
// resolve to via the parent namespace) and trip CS0618.
using GumService = Gum.GumService;

namespace MonoGameGum.Tests;

/// <summary>
/// Covers issue #3199: the runtime should apply the project's
/// <see cref="GumProjectSave.TextureFilter"/> to the global
/// <see cref="Renderer.TextureFilter"/> when a project is loaded, so the editor's
/// Texture Filter setting carries over to the game (WYSIWYG).
/// </summary>
public class TextureFilterTests : BaseTestClass
{
    public override void Dispose()
    {
        // Renderer.TextureFilter is global static state; restore the default so a
        // test that flips it to Linear doesn't leak into unrelated tests.
        Renderer.TextureFilter = TextureFilter.Point;
        base.Dispose();
    }

    [Fact]
    public void ApplyProjectTextureFilter_ShouldSetLinear_WhenProjectTextureFilterIsLinear()
    {
        Renderer.TextureFilter = TextureFilter.Point;
        GumProjectSave project = new GumProjectSave { TextureFilter = "Linear" };

        GumService.ApplyProjectTextureFilter(project);

        Renderer.TextureFilter.ShouldBe(TextureFilter.Linear);
    }

    [Fact]
    public void ApplyProjectTextureFilter_ShouldSetPoint_WhenProjectTextureFilterIsNull()
    {
        Renderer.TextureFilter = TextureFilter.Linear;
        GumProjectSave project = new GumProjectSave { TextureFilter = null };

        GumService.ApplyProjectTextureFilter(project);

        Renderer.TextureFilter.ShouldBe(TextureFilter.Point);
    }

    [Fact]
    public void ApplyProjectTextureFilter_ShouldSetPoint_WhenProjectTextureFilterIsPoint()
    {
        Renderer.TextureFilter = TextureFilter.Linear;
        GumProjectSave project = new GumProjectSave { TextureFilter = "Point" };

        GumService.ApplyProjectTextureFilter(project);

        Renderer.TextureFilter.ShouldBe(TextureFilter.Point);
    }

    [Fact]
    public void ApplyProjectTextureFilter_ShouldSetPoint_WhenProjectTextureFilterIsUnrecognized()
    {
        Renderer.TextureFilter = TextureFilter.Linear;
        GumProjectSave project = new GumProjectSave { TextureFilter = "SomethingElse" };

        GumService.ApplyProjectTextureFilter(project);

        Renderer.TextureFilter.ShouldBe(TextureFilter.Point);
    }
}
