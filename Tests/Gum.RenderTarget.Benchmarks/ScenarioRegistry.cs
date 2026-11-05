using System;
using System.Collections.Generic;

namespace Gum.RenderTarget.Benchmarks;

/// <summary>The scenarios the app runs, in order. A new scenario is one line here.</summary>
public static class ScenarioRegistry
{
    public static IReadOnlyList<Func<IBenchScenario>> Factories { get; } = new List<Func<IBenchScenario>>
    {
        () => new PlainSpritesScenario(),
        () => new RenderTargetOutlineScenario(),
        () => new RenderTargetNoShaderScenario(),
        () => new CachedTargetsScenario(),
    };
}
