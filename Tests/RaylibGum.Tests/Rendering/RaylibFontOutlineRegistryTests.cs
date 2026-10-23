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

    [Fact]
    public void TryGetOffsets_ClampsTheScaledRadius_SoAHugeScaleCannotCostThousandsOfPasses()
    {
        RaylibFontOutlineRegistry.Register(TextureId, 5);
        try
        {
            RaylibFontOutlineRegistry.TryGetOffsets(TextureId, 1000f, out System.Numerics.Vector2[] huge).ShouldBeTrue();

            huge.ShouldBe(RaylibFontOutlineRegistry.GetOffsets(RaylibFontOutlineRegistry.MaxScaledRadius));
        }
        finally
        {
            RaylibFontOutlineRegistry.Register(TextureId, 0);
        }
    }

    [Fact]
    public void TryGetOffsets_ScalesTheThicknessByTheGivenScale()
    {
        RaylibFontOutlineRegistry.Register(TextureId, 3);
        try
        {
            RaylibFontOutlineRegistry.TryGetOffsets(TextureId, 2f, out System.Numerics.Vector2[] doubled).ShouldBeTrue();

            doubled.ShouldBe(RaylibFontOutlineRegistry.GetOffsets(6));
        }
        finally
        {
            RaylibFontOutlineRegistry.Register(TextureId, 0);
        }
    }
}
