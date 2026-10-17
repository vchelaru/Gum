using Microsoft.Xna.Framework.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests;

/// <summary>
/// What a lease does to the shared device between tests: the back buffer a test is laid out for, and
/// the frame that lets MonoGame run its deferred GL deletions (#6101).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class SharedGameLeaseTests : BaseTestClass
{
    [Fact]
    public void MultiServiceLease_ShouldResizeTheBackBuffer_AndTheNextLeaseShouldGetTheDefaultSize()
    {
        using (MultiServiceGameLease multi = new())
        {
            multi.GraphicsDevice.PresentationParameters.BackBufferWidth.ShouldBe(MultiServiceGameLease.Width);
            multi.GraphicsDevice.PresentationParameters.BackBufferHeight.ShouldBe(MultiServiceGameLease.Height);
        }

        using (PlainLease plain = new())
        {
            plain.GraphicsDevice.PresentationParameters.BackBufferWidth.ShouldBe(SharedGame.DefaultWidth);
            plain.GraphicsDevice.PresentationParameters.BackBufferHeight.ShouldBe(SharedGame.DefaultHeight);
            plain.GraphicsDevice.Viewport.Width.ShouldBe(SharedGame.DefaultWidth);
        }
    }

    // MonoGame deletes a disposed texture's GL object on the next Present. A test that never draws would
    // otherwise keep every texture it disposed (about 20 MB per themed multi-service test).
    [Fact]
    public void Dispose_ShouldPresentAFrame_SoDeferredGlDeletionsRun()
    {
        int framesBefore = SharedGame.CurrentHost.FramesDrawn;
        PlainLease lease = new();
        using Texture2D texture = new(lease.GraphicsDevice, 4, 4);

        lease.Dispose();

        SharedGame.CurrentHost.FramesDrawn.ShouldBeGreaterThan(framesBefore);
    }

    private sealed class PlainLease : SharedGameLease
    {
    }
}
