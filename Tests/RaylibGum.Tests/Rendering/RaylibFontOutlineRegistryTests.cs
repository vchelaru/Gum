using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests.Rendering;

public class RaylibFontOutlineRegistryTests
{
    private const uint TextureId = 987654;

    [Fact]
    public void Register_ZeroThickness_ClearsTheEntryAReusedTextureIdLeftBehind()
    {
        RaylibFontOutlineRegistry.Register(TextureId, 3);
        RaylibFontOutlineRegistry.TryGet(TextureId, out int thickness).ShouldBeTrue();
        thickness.ShouldBe(3);

        RaylibFontOutlineRegistry.Register(TextureId, 0);

        RaylibFontOutlineRegistry.TryGet(TextureId, out _).ShouldBeFalse();
    }

    [Fact]
    public void GetOffsets_AreDistinctWithinTheRadius_AndExcludeTheOrigin()
    {
        System.Numerics.Vector2[] offsets = RaylibFontOutlineRegistry.GetOffsets(2);

        offsets.ShouldNotContain(System.Numerics.Vector2.Zero);
        offsets.Distinct().Count().ShouldBe(offsets.Length);
        offsets.ShouldAllBe(offset => offset.Length() <= 2.3f);
        offsets.ShouldContain(new System.Numerics.Vector2(2, 0));
    }
}
