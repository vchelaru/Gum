using Gum;
using Moq;
using SkiaHostTests;
using SkiaSharp;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Input;
using System;
using System.Collections.Generic;
using Keys = Stride.Input.Keys;

namespace StrideGum.Tests;

/// <summary>
/// The keyboard and cursor register as <see cref="InputManager"/> listeners, and the manager outlives
/// the service, so Uninitialize and a second Initialize must unregister them. The events travel
/// through a real <see cref="InputManager"/> fed by fake devices. The cases live in
/// <see cref="InputSubscriptionTestsBase"/>; this supplies the Stride devices.
/// </summary>
public class InputSubscriptionTests : InputSubscriptionTestsBase
{
    private sealed class FakeSource : IInputSource
    {
        public TrackingDictionary<Guid, IInputDevice> Devices { get; } = new TrackingDictionary<Guid, IInputDevice>();
        public void Initialize(InputManager inputManager) { }
        public void Scan() { }
        public void Update() { }
        public void Pause() { }
        public void Resume() { }
        public void Dispose() { }
    }

    // A real InputManager with a mouse and keyboard whose events the test queues and Update delivers.
    private sealed class Devices : IHostInputRig
    {
        private readonly List<InputEvent> _pending = new List<InputEvent>();

        public InputManager Manager { get; } = new InputManager();
        public Mock<IMouseDevice> Mouse { get; } = new Mock<IMouseDevice>();
        public Mock<IKeyboardDevice> Keyboard { get; } = new Mock<IKeyboardDevice>();

        public Devices()
        {
            Manager.RegisterEventType<MouseWheelEvent>();
            Manager.RegisterEventType<TextInputEvent>();

            Mouse.SetupGet(m => m.SurfaceSize).Returns(new Vector2(800, 600));
            Mouse.SetupGet(m => m.DownButtons).Returns(new ReadOnlySet<MouseButton>(new HashSet<MouseButton>()));
            Keyboard.SetupGet(k => k.PressedKeys).Returns(new ReadOnlySet<Keys>(new HashSet<Keys>()));
            Keyboard.SetupGet(k => k.ReleasedKeys).Returns(new ReadOnlySet<Keys>(new HashSet<Keys>()));
            Keyboard.SetupGet(k => k.DownKeys).Returns(new ReadOnlySet<Keys>(new HashSet<Keys>()));
            Add(Mouse);
            Add(Keyboard);
        }

        private void Add<T>(Mock<T> device) where T : class, IInputDevice
        {
            FakeSource source = new FakeSource();
            device.SetupGet(d => d.Id).Returns(Guid.NewGuid());
            device.SetupGet(d => d.Source).Returns(source);
            device.Setup(d => d.Update(It.IsAny<List<InputEvent>>()))
                .Callback<List<InputEvent>>(events =>
                {
                    events.AddRange(_pending);
                    _pending.Clear();
                });
            Manager.Sources.Add(source);
            source.Devices.Add(device.Object.Id, device.Object);
        }

        public void Initialize() => InitializeService(Manager);

        public void Scroll(float notches)
        {
            MouseWheelEvent wheel = InputEventPool<MouseWheelEvent>.GetOrCreate(Mouse.Object);
            wheel.WheelDelta = notches;
            _pending.Add(wheel);
            Manager.Update(new GameTime());
        }

        public void Type(string text)
        {
            TextInputEvent typed = InputEventPool<TextInputEvent>.GetOrCreate(Keyboard.Object);
            typed.Text = text;
            typed.Type = TextInputEventType.Input;
            _pending.Add(typed);
            Manager.Update(new GameTime());
        }
    }

    private static void InitializeService(InputManager manager)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.InitializeCore(surface.Canvas, manager, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight, null);
    }

    protected override IHostInputRig CreateRig() => new Devices();

    protected override void RestoreSharedService()
    {
        GumService.Default.Uninitialize();
        InitializeService(new InputManager());
    }
}
