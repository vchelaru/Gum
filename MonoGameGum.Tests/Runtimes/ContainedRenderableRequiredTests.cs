using Gum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class ContainedRenderableRequiredTests
{
    public static IEnumerable<object[]> RuntimesWithoutRenderable()
    {
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete but still shipped
        yield return new object[] { (Func<int>)(() => new ColoredRectangleRuntime(fullInstantiation: false).Alpha) };
#pragma warning restore CS0618
        yield return new object[] { (Func<int>)(() => new NineSliceRuntime(fullInstantiation: false).Alpha) };
        yield return new object[] { (Func<int>)(() => new PolygonRuntime(fullInstantiation: false).Alpha) };
        yield return new object[] { (Func<int>)(() => new SpriteRuntime(fullInstantiation: false).Alpha) };
    }

    [Theory]
    [MemberData(nameof(RuntimesWithoutRenderable))]
    public void Alpha_ShouldThrowInvalidOperation_WhenNotFullyInstantiated(Func<int> readAlpha)
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => readAlpha());

        exception.Message.ShouldContain("renderable");
    }
}
