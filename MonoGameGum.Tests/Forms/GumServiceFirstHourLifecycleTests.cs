using Microsoft.Xna.Framework;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Ordering mistakes around GumService that a beginner makes before or between Initialize calls.
/// </summary>
public class GumServiceFirstHourLifecycleTests : BaseTestClass
{
    [Fact]
    public void Uninitialize_ShouldNotThrowOrResetLiveService_WhenCalledOnNeverInitializedService()
    {
        Gum.GumService live = Gum.GumService.Default;
        Gum.GumService fresh = new();

        Should.NotThrow(() => fresh.Uninitialize());

        Gum.GumService.Default.ShouldBeSameAs(live);
        live.Root.ShouldNotBeNull();
    }
}
