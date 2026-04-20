using Gum.Wireframe;
using Moq;
using Moq.Protected;
using Raylib_cs;
using Gum.Input;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GumKeys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Inputs;

public class KeyboardTests : BaseTestClass
{
    [Fact]
    public void GetStringTyped_ShouldReturnSameValue_WhenCalledMultipleTimes()
    {
        var sut = new Mock<Keyboard>();
        var codepoints = new Queue<int>(new[] { 72, 101, 108, 108, 111, 0 });
        sut.Protected()
            .Setup<int>("GetCharPressed")
            .Returns(() => codepoints.Count > 0 ? codepoints.Dequeue() : 0);

        sut.Object.Activity(1);

        string firstCallResult = sut.Object.GetStringTyped();
        string secondCallResult = sut.Object.GetStringTyped();

        firstCallResult.ShouldBe("Hello");
        secondCallResult.ShouldBe("Hello");

        sut.Object.Activity(2);

        string thirdCallResult = sut.Object.GetStringTyped();
        thirdCallResult.ShouldBe(string.Empty);
    }

    [Fact]
    public void KeysTyped_DropsUnmappedRaylibKeys()
    {
        var sut = new Mock<Keyboard>();
        var pressed = new Queue<int>(new[] { (int)KeyboardKey.Menu, 0 });
        sut.Protected()
            .Setup<int>("GetKeyPressed")
            .Returns(() => pressed.Count > 0 ? pressed.Dequeue() : 0);

        sut.Object.Activity(1);

        List<GumKeys> result = ((IInputReceiverKeyboard)sut.Object).KeysTyped.ToList();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void KeysTyped_ResetsAfterActivity()
    {
        var sut = new Mock<Keyboard>();
        var pressed = new Queue<int>(new[] { (int)KeyboardKey.A, 0 });
        sut.Protected()
            .Setup<int>("GetKeyPressed")
            .Returns(() => pressed.Count > 0 ? pressed.Dequeue() : 0);

        sut.Object.Activity(1);

        // Read twice in frame 1 to exercise the cache, then advance the frame and
        // load a different key. A cache that isn't invalidated by Activity would keep
        // returning [A] instead of picking up [B].
        ((IInputReceiverKeyboard)sut.Object).KeysTyped.ToList();
        List<GumKeys> firstFrameRepeat = ((IInputReceiverKeyboard)sut.Object).KeysTyped.ToList();
        firstFrameRepeat.ShouldBe(new List<GumKeys> { GumKeys.A });

        sut.Object.Activity(2);
        pressed.Enqueue((int)KeyboardKey.B);
        pressed.Enqueue(0);

        List<GumKeys> secondFrame = ((IInputReceiverKeyboard)sut.Object).KeysTyped.ToList();
        secondFrame.ShouldBe(new List<GumKeys> { GumKeys.B });
    }

    [Fact]
    public void KeysTyped_ReturnsSameValuesWhenReadTwiceInOneFrame()
    {
        var sut = new Mock<Keyboard>();
        var pressed = new Queue<int>(new[] { (int)KeyboardKey.A, (int)KeyboardKey.B, 0 });
        sut.Protected()
            .Setup<int>("GetKeyPressed")
            .Returns(() => pressed.Count > 0 ? pressed.Dequeue() : 0);

        sut.Object.Activity(1);

        List<GumKeys> firstRead = ((IInputReceiverKeyboard)sut.Object).KeysTyped.ToList();
        List<GumKeys> secondRead = ((IInputReceiverKeyboard)sut.Object).KeysTyped.ToList();

        var expected = new List<GumKeys> { GumKeys.A, GumKeys.B };
        firstRead.ShouldBe(expected);
        secondRead.ShouldBe(expected);
    }
}
