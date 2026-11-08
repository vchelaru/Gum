using Gum;
using SkiaHostTests;
using SkiaSharp;

namespace BrutalGum.Tests;

/// <summary>
/// The keyboard and cursor subscribe to the window's events, and the window outlives the service, so
/// Uninitialize and a second Initialize must unsubscribe them. The cases live in
/// <see cref="InputSubscriptionTestsBase"/>; this supplies the fake window.
/// </summary>
public class InputSubscriptionTests : InputSubscriptionTestsBase
{
    private sealed class BrutalRig : IHostInputRig
    {
        private readonly FakeBrutalInput _input = new();

        public void Initialize() => InitializeService(_input);

        public void Type(string text) => _input.RaiseChar(text[0]);

        public void Scroll(float notches) => _input.RaiseScroll(notches);
    }

    private static void InitializeService(FakeBrutalInput input)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.Initialize(surface.Canvas, input, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
    }

    protected override IHostInputRig CreateRig() => new BrutalRig();

    protected override void RestoreSharedService()
    {
        GumService.Default.Uninitialize();
        InitializeService(new FakeBrutalInput());
    }
}
