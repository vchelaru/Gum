using System.Collections.Generic;
using System.Reflection;
using Apos.Shapes;
using Gum.Renderables;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// ShapeRenderer compiles the shader version Gum's draws select at startup, so no shape compiles a
/// shader mid-game, and <see cref="ShapeRenderer.WarmupFeatures"/> can limit it to the default version.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ShapeRendererWarmupTests : BaseTestClass
{
    [Fact]
    public void Initialize_ByDefault_CompilesOneVersionCoveringGradientsEllipsesClipsAndRamps()
    {
        ShapeRenderer.Self.WarmupFeatures.ShouldBe(ShapeRenderer.DefaultWarmupFeatures);

        using ShapeGameLease game = new ShapeGameLease();

        // A solid Gradient argument already selects Gradients, so ellipses and paths draw with
        // Gradients|EllipsesAndPaths and arcs and ramps with Gradients|BorderGradients|Ramps; one
        // version holding all of them also covers a batch that mixes them.
        ShapeFeatures required = ShapeFeatures.Gradients | ShapeFeatures.BorderGradients
            | ShapeFeatures.EllipsesAndPaths | ShapeFeatures.Clips | ShapeFeatures.Ramps;
        CompiledVersions().ShouldBe(new[] { (int)required });
    }

    [Fact]
    public void Initialize_WithWarmupFeaturesNone_CompilesOnlyTheDefaultVersion()
    {
        ShapeRenderer.Self.WarmupFeatures = ShapeFeatures.None;
        try
        {
            using ShapeGameLease game = new ShapeGameLease();

            CompiledVersions().ShouldBe(new[] { (int)ShapeFeatures.None });
        }
        finally
        {
            ShapeRenderer.Self.WarmupFeatures = ShapeRenderer.DefaultWarmupFeatures;
        }
    }

    // Apos.Shapes has no public way to ask which shader versions compiled, so this reads its private
    // table: one flag per ShapeFeatures mix, set once that version has drawn.
    private static List<int> CompiledVersions()
    {
        FieldInfo? field = typeof(ShapeBatch).GetField("_compiled", BindingFlags.Instance | BindingFlags.NonPublic);
        field.ShouldNotBeNull("Apos.Shapes renamed ShapeBatch._compiled; update this test to its new name.");
        bool[] compiled = (bool[])field.GetValue(ShapeRenderer.Self.ShapeBatch)!;
        List<int> versions = new List<int>();
        for (int i = 0; i < compiled.Length; i++)
        {
            if (compiled[i])
            {
                versions.Add(i);
            }
        }
        return versions;
    }
}
