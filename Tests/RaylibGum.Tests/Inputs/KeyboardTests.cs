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
    public void GetStringTyped_AppendsNewline_WhenEnterIsPressed()
    {
        var sut = new Mock<Keyboard>();
        sut.Protected()
            .Setup<int>("GetCharPressed")
            .Returns(0);
        sut.Protected()
            .Setup<bool>("IsKeyPressed", ItExpr.IsAny<KeyboardKey>())
            .Returns((KeyboardKey k) => k == KeyboardKey.Enter);
        sut.Protected()
            .Setup<bool>("IsKeyPressedRepeat", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);

        sut.Object.Activity(1);

        sut.Object.GetStringTyped().ShouldBe("\n");
    }

    [Fact]
    public void GetStringTyped_AppendsNewline_WhenEnterRepeats()
    {
        var sut = new Mock<Keyboard>();
        sut.Protected()
            .Setup<int>("GetCharPressed")
            .Returns(0);
        sut.Protected()
            .Setup<bool>("IsKeyPressed", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);
        sut.Protected()
            .Setup<bool>("IsKeyPressedRepeat", ItExpr.IsAny<KeyboardKey>())
            .Returns((KeyboardKey k) => k == KeyboardKey.Enter);

        sut.Object.Activity(1);

        sut.Object.GetStringTyped().ShouldBe("\n");
    }

    [Fact]
    public void GetStringTyped_AppendsNewline_WhenKpEnterIsPressed()
    {
        var sut = new Mock<Keyboard>();
        sut.Protected()
            .Setup<int>("GetCharPressed")
            .Returns(0);
        sut.Protected()
            .Setup<bool>("IsKeyPressed", ItExpr.IsAny<KeyboardKey>())
            .Returns((KeyboardKey k) => k == KeyboardKey.KpEnter);
        sut.Protected()
            .Setup<bool>("IsKeyPressedRepeat", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);

        sut.Object.Activity(1);

        sut.Object.GetStringTyped().ShouldBe("\n");
    }

    [Fact]
    public void GetStringTyped_AppendsNewlineAfterCharacters_WhenEnterIsPressed()
    {
        var sut = new Mock<Keyboard>();
        var codepoints = new Queue<int>(new[] { 72, 105, 0 });
        sut.Protected()
            .Setup<int>("GetCharPressed")
            .Returns(() => codepoints.Count > 0 ? codepoints.Dequeue() : 0);
        sut.Protected()
            .Setup<bool>("IsKeyPressed", ItExpr.IsAny<KeyboardKey>())
            .Returns((KeyboardKey k) => k == KeyboardKey.Enter);
        sut.Protected()
            .Setup<bool>("IsKeyPressedRepeat", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);

        sut.Object.Activity(1);

        sut.Object.GetStringTyped().ShouldBe("Hi\n");
    }

    [Fact]
    public void GetStringTyped_AppendsSingleNewline_WhenEnterAndKpEnterBothPressed()
    {
        var sut = new Mock<Keyboard>();
        sut.Protected()
            .Setup<int>("GetCharPressed")
            .Returns(0);
        sut.Protected()
            .Setup<bool>("IsKeyPressed", ItExpr.IsAny<KeyboardKey>())
            .Returns((KeyboardKey k) => k == KeyboardKey.Enter || k == KeyboardKey.KpEnter);
        sut.Protected()
            .Setup<bool>("IsKeyPressedRepeat", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);

        sut.Object.Activity(1);

        sut.Object.GetStringTyped().ShouldBe("\n");
    }

    [Fact]
    public void GetStringTyped_DoesNotAppendNewline_WhenEnterIsNotPressed()
    {
        var sut = new Mock<Keyboard>();
        sut.Protected()
            .Setup<int>("GetCharPressed")
            .Returns(0);
        sut.Protected()
            .Setup<bool>("IsKeyPressed", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);
        sut.Protected()
            .Setup<bool>("IsKeyPressedRepeat", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);

        sut.Object.Activity(1);

        sut.Object.GetStringTyped().ShouldBe(string.Empty);
    }

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
    public void KeysTyped_IncludesKey_WhenIsKeyPressedRepeatReturnsTrue()
    {
        var sut = new Mock<Keyboard>();
        sut.Protected()
            .Setup<bool>("IsKeyPressed", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);
        sut.Protected()
            .Setup<bool>("IsKeyPressedRepeat", ItExpr.IsAny<KeyboardKey>())
            .Returns((KeyboardKey k) => k == KeyboardKey.Delete);

        sut.Object.Activity(1);

        List<GumKeys> result = ((IInputReceiverKeyboard)sut.Object).KeysTyped.ToList();

        result.ShouldBe(new List<GumKeys> { GumKeys.Delete });
    }

    [Fact]
    public void KeysTyped_IncludesKey_WhenIsKeyPressedReturnsTrue()
    {
        var sut = new Mock<Keyboard>();
        sut.Protected()
            .Setup<bool>("IsKeyPressed", ItExpr.IsAny<KeyboardKey>())
            .Returns((KeyboardKey k) => k == KeyboardKey.A);
        sut.Protected()
            .Setup<bool>("IsKeyPressedRepeat", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);

        sut.Object.Activity(1);

        List<GumKeys> result = ((IInputReceiverKeyboard)sut.Object).KeysTyped.ToList();

        result.ShouldBe(new List<GumKeys> { GumKeys.A });
    }

    [Fact]
    public void KeysTyped_IsEmpty_WhenNoKeyPressedOrRepeated()
    {
        var sut = new Mock<Keyboard>();
        sut.Protected()
            .Setup<bool>("IsKeyPressed", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);
        sut.Protected()
            .Setup<bool>("IsKeyPressedRepeat", ItExpr.IsAny<KeyboardKey>())
            .Returns(false);

        sut.Object.Activity(1);

        List<GumKeys> result = ((IInputReceiverKeyboard)sut.Object).KeysTyped.ToList();

        result.ShouldBeEmpty();
    }
}
