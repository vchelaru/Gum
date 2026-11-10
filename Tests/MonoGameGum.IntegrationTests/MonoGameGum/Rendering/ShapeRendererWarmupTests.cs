using System.Reflection;
using Apos.Shapes;
using Gum.Renderables;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #7808: from Apos.Shapes 0.10.0 the ShapeBatch constructor's warmup leaves out ellipses,
/// paths, clips and ramps, which 0.9.4's startup compile covered. ShapeRenderer warms them up itself
/// so those shapes don't compile a shader mid-game.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ShapeRendererWarmupTests : BaseTestClass
{
    [Fact]
    public void Initialize_CompilesVersionCovering_EllipsesPathsClipsAndRamps()
    {
        using ShapeGameLease game = new ShapeGameLease();

        ShapeFeatures required = ShapeFeatures.EllipsesAndPaths | ShapeFeatures.Clips | ShapeFeatures.Ramps;

        // Apos.Shapes has no public way to ask which shader versions compiled, so this reads its
        // private table: one flag per ShapeFeatures mix, set once that version has drawn.
        FieldInfo? field = typeof(ShapeBatch).GetField("_compiled", BindingFlags.Instance | BindingFlags.NonPublic);
        field.ShouldNotBeNull("Apos.Shapes renamed ShapeBatch._compiled; update this test to its new name.");
        bool[] compiled = (bool[])field.GetValue(ShapeRenderer.Self.ShapeBatch)!;

        bool covered = false;
        for (int i = 0; i < compiled.Length; i++)
        {
            if (compiled[i] && (i & (int)required) == (int)required)
            {
                covered = true;
            }
        }
        covered.ShouldBeTrue();
    }
}
