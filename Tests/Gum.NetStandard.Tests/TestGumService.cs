namespace Gum.NetStandard.Tests;

// The concrete Gum.GumService lives in SkiaGum.Standalone, which has no netstandard2.1 build.
// A netstandard2.1 host (Unity) derives its own, as this does.
internal sealed class TestGumService : GumServiceSkiaBase { }
