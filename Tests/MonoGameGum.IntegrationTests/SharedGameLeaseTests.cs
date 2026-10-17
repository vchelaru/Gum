using Apos.Shapes;
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

    // MonoGame skips re-pointing the vertex attributes when a draw's vertex buffer has the same GL name as the
    // last one drawn. Once the old buffer's deferred delete has run, the next buffer gets that name back and the
    // skipped re-point leaves the draw reading from nothing, which is an access violation in the native GL
    // layer (#6107). A lease's Dispose has to leave that cache invalid. The ShapeBatch built after two frames
    // is the draw that crashed the test host.
    [Fact]
    public void Dispose_ShouldLeaveTheDeviceSafeForANewVertexBufferThatReusesAFreedGlName()
    {
        const int iterations = 3;
        for (int i = 0; i < iterations; i++)
        {
            PlainLease lease = new();
            // Warmup draws once through the vertex buffer, which is what caches its GL name.
            ShapeBatch shapeBatch = new(lease.GraphicsDevice, (Effect?)null, warmup: true);
            shapeBatch.Dispose();
            lease.Dispose();

            // Two frames after the buffer was disposed is when its GL name is free to be reused.
            SharedGame.CurrentHost.RunOneFrame();
        }
    }

    private sealed class PlainLease : SharedGameLease
    {
    }
}
