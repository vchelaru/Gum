using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Moq;
using Shouldly;
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
/// through a real <see cref="InputManager"/> fed by fake devices. These tests re-initialize the
/// assembly-wide service, so each one puts it back.
/// </summary>
public class InputSubscriptionTests : BaseTestClass
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
    private sealed class Devices
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

        public void ScrollNotches(float notches)
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

    private static void Initialize(InputManager manager)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.InitializeCore(surface.Canvas, manager, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight, null);
    }

    private static void RestoreSharedService()
    {
        GumService.Default.Uninitialize();
        Initialize(new InputManager());
        SkiaHostTestBase.CaptureRenderableBaseline();
        FrameworkElement.KeyboardsForUiControl.Clear();
    }

    [Fact]
    public void Uninitialize_StopsTheOldKeyboardReceivingTypedText()
    {
        Devices devices = new Devices();
        try
        {
            GumService.Default.Uninitialize();
            Initialize(devices.Manager);
            Keyboard oldKeyboard = GumService.Default.Keyboard;

            GumService.Default.Uninitialize();
            devices.Type("x");
            oldKeyboard.Activity(0);

            oldKeyboard.GetStringTyped().ShouldBe("");
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Uninitialize_StopsTheOldCursorReceivingScroll()
    {
        Devices devices = new Devices();
        try
        {
            GumService.Default.Uninitialize();
            Initialize(devices.Manager);
            Cursor oldCursor = GumService.Default.Cursor;

            GumService.Default.Uninitialize();
            devices.ScrollNotches(2);
            oldCursor.Activity(0);

            oldCursor.ScrollWheelChange.ShouldBe(0);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void SecondInitialize_StopsThePreviousKeyboardReceivingTypedText()
    {
        Devices devices = new Devices();
        try
        {
            GumService.Default.Uninitialize();
            Initialize(devices.Manager);
            Keyboard firstKeyboard = GumService.Default.Keyboard;

            Initialize(devices.Manager);
            devices.Type("x");
            firstKeyboard.Activity(0);

            firstKeyboard.GetStringTyped().ShouldBe("");
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void LiveCursorAndKeyboard_ReceiveTheEvents()
    {
        // Guards the tests above: with the service still initialized, the same events do arrive.
        Devices devices = new Devices();
        try
        {
            GumService.Default.Uninitialize();
            Initialize(devices.Manager);

            devices.Type("x");
            devices.ScrollNotches(2);
            GumService.Default.Keyboard.Activity(0);
            GumService.Default.Cursor.Activity(0);

            GumService.Default.Keyboard.GetStringTyped().ShouldBe("x");
            GumService.Default.Cursor.ScrollWheelChange.ShouldBe(2);
        }
        finally
        {
            RestoreSharedService();
        }
    }
}
