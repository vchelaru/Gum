using System;
using Microsoft.Xna.Framework.Graphics;
using Shouldly;
using XnaAndWinforms;

namespace GumToolUnitTests.Rendering;

public class GraphicsDeviceServiceRenderHostAdapterTests : BaseTestClass
{
    private class FakeGraphicsDeviceService : IGraphicsDeviceService
    {
        public GraphicsDevice? GraphicsDevice { get; set; }

#pragma warning disable CS0067 // required by IGraphicsDeviceService, never raised by this fake
        public event EventHandler<EventArgs>? DeviceCreated;
        public event EventHandler<EventArgs>? DeviceDisposing;
        public event EventHandler<EventArgs>? DeviceReset;
        public event EventHandler<EventArgs>? DeviceResetting;
#pragma warning restore CS0067
    }

    private class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    [Fact]
    public void Constructor_NullGraphicsDeviceService_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new GraphicsDeviceServiceRenderHostAdapter(null!, new FakeServiceProvider()));
    }

    [Fact]
    public void Constructor_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new GraphicsDeviceServiceRenderHostAdapter(new FakeGraphicsDeviceService(), null!));
    }

    [Fact]
    public void GraphicsDevice_ForwardsToWrappedGraphicsDeviceService()
    {
        FakeGraphicsDeviceService graphicsDeviceService = new FakeGraphicsDeviceService
        {
            GraphicsDevice = null
        };
        GraphicsDeviceServiceRenderHostAdapter adapter =
            new GraphicsDeviceServiceRenderHostAdapter(graphicsDeviceService, new FakeServiceProvider());

        adapter.GraphicsDevice.ShouldBe(graphicsDeviceService.GraphicsDevice);
    }

    [Fact]
    public void Services_ForwardsToWrappedServiceProvider()
    {
        FakeServiceProvider services = new FakeServiceProvider();
        GraphicsDeviceServiceRenderHostAdapter adapter =
            new GraphicsDeviceServiceRenderHostAdapter(new FakeGraphicsDeviceService(), services);

        adapter.Services.ShouldBeSameAs(services);
    }
}
