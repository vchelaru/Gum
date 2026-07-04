using System;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// AposShapeRuntime.HandleCustomGetDefaultState used to fall back to Container's default state for
// any type it didn't recognize (Svg, LottieAnimation, Canvas, ...), and it was chained onto
// StandardElementsManager.CustomGetDefaultState via `+=` - a multicast Func whose Invoke() only
// returns the LAST subscriber's result. In the Gum tool process, where both AposShapeRuntime
// (KniGumShapes) and the Skia plugin register a resolver, this silently discarded the Skia
// plugin's correct Svg/LottieAnimation/Canvas answer and injected Container-only variables (e.g.
// IsRenderTarget) into those standards' .gutx files on every project load.
public class AposShapeRuntimeDefaultStateTests
{
    [Fact]
    public void CombineGetDefaultState_KnownShapeType_ResolvesOwnState()
    {
        var combined = AposShapeRuntime.CombineGetDefaultState(existing: null);

        combined("Arc").ShouldBeSameAs(StandardElementsManager.GetArcState());
    }

    [Fact]
    public void CombineGetDefaultState_UnknownType_NoExistingResolver_ReturnsNull()
    {
        var combined = AposShapeRuntime.CombineGetDefaultState(existing: null);

        combined("LottieAnimation").ShouldBeNull(
            "because AposShapeRuntime doesn't know about LottieAnimation and must not silently " +
            "fall back to Container's default state for it.");
    }

    [Fact]
    public void CombineGetDefaultState_UnknownType_FallsBackToExistingResolver()
    {
        StateSave sentinelLottieState = new() { Name = "SentinelLottieDefault" };
        Func<string, StateSave?> existing = type => type == "LottieAnimation" ? sentinelLottieState : null;

        var combined = AposShapeRuntime.CombineGetDefaultState(existing);

        combined("LottieAnimation").ShouldBeSameAs(sentinelLottieState,
            "because a previously-registered resolver's answer for a type AposShapeRuntime " +
            "doesn't recognize must not be discarded.");
    }
}
