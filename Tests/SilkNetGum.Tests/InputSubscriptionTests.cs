using Gum;
using Moq;
using Silk.NET.Input;
using SkiaHostTests;
using SkiaSharp;
using System.Collections.Generic;

namespace SilkNetGum.Tests;

/// <summary>
/// The keyboard and cursor subscribe to the Silk devices' events, and the devices outlive the service, so
/// Uninitialize and a second Initialize must unsubscribe them. The cases live in
/// <see cref="InputSubscriptionTestsBase"/>; this supplies the Silk devices.
/// </summary>
public class InputSubscriptionTests : InputSubscriptionTestsBase
{
    private sealed class SilkRig : IHostInputRig
    {
        private readonly Mock<IKeyboard> _keyboard = new Mock<IKeyboard>();
        private readonly Mock<IMouse> _mouse = new Mock<IMouse>();

        public void Initialize() => InitializeService(CreateContext(_keyboard, _mouse).Object);

        public void Type(string text) => _keyboard.Raise(k => k.KeyChar += null, _keyboard.Object, text[0]);

        public void Scroll(float notches) =>
            _mouse.Raise(m => m.Scroll += null, _mouse.Object, new ScrollWheel(0, notches));
    }

    private static Mock<IInputContext> CreateContext(Mock<IKeyboard> keyboard, Mock<IMouse> mouse)
    {
        mouse.SetupGet(m => m.ScrollWheels).Returns(new List<ScrollWheel>());

        Mock<IInputContext> context = new Mock<IInputContext>();
        context.SetupGet(c => c.Keyboards).Returns(new List<IKeyboard> { keyboard.Object });
        context.SetupGet(c => c.Mice).Returns(new List<IMouse> { mouse.Object });
        context.SetupGet(c => c.Gamepads).Returns(new List<IGamepad>());
        return context;
    }

    private static void InitializeService(IInputContext context)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.Initialize(surface.Canvas, context, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
    }

    protected override IHostInputRig CreateRig() => new SilkRig();

    protected override void RestoreSharedService()
    {
        GumService.Default.Uninitialize();
        InitializeService(CreateContext(new Mock<IKeyboard>(), new Mock<IMouse>()).Object);
    }
}
