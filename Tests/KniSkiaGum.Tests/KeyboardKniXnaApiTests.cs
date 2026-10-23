using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Reflection;
using GumKeys = Gum.Forms.Input.Keys;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace KniSkiaGum.Tests;

/// <summary>
/// The KNI Skia keyboard mirrors KniGum's keyboard API: XNA <c>Keys</c> overloads, <c>KeysTyped</c>,
/// <c>KeyboardStateProcessor</c>, <c>IsKeyLetter</c> and <c>NumberOfKeys</c>, so code written against
/// KniGum compiles and behaves the same. Ported from MonoGameGum's <c>KeyboardXnaKeysOverloadTests</c>.
/// </summary>
public class KeyboardKniXnaApiTests
{
    private static Keyboard CreateKeyboard(params XnaKeys[] keysDown)
    {
        Mock<KeyboardStateProcessor> processor = new Mock<KeyboardStateProcessor>();
        foreach (XnaKeys key in keysDown)
        {
            processor.Setup(x => x.IsKeyDown(key)).Returns(true);
        }

        return new Keyboard { KeyboardStateProcessor = processor.Object };
    }

    [Fact]
    public void Activity_ReadsTheDeviceThroughTheKeyboardStateProcessor()
    {
        Mock<KeyboardStateProcessor> processor = new Mock<KeyboardStateProcessor>();
        processor.Setup(x => x.IsKeyDown(XnaKeys.A)).Returns(true);
        Keyboard sut = new Keyboard { KeyboardStateProcessor = processor.Object };

        sut.Activity(0);

        processor.Verify(x => x.Update(), Times.Once);
        sut.KeyDown(GumKeys.A).ShouldBeTrue();
        sut.KeyDown(GumKeys.B).ShouldBeFalse();
    }

    [Fact]
    public void IsKeyLetter_WithGumKeys_ShouldOnlyBeTrueForLetters()
    {
        Keyboard sut = new Keyboard();

        sut.IsKeyLetter(GumKeys.A).ShouldBeTrue();
        sut.IsKeyLetter(GumKeys.Z).ShouldBeTrue();
        sut.IsKeyLetter(GumKeys.D1).ShouldBeFalse();
    }

    [Fact]
    public void Keyboard_ShouldImplementTheMonoGameKeyboardInterface()
    {
        new Keyboard().ShouldBeAssignableTo<IInputReceiverKeyboardMonoGame>();
    }

    [Fact]
    public void KeysTyped_ShouldReturnTheXnaKeysTypedThisFrame()
    {
        Keyboard sut = CreateKeyboard(XnaKeys.A, XnaKeys.Enter);

        sut.Activity(0);

        sut.KeysTyped.ShouldBe(new[] { XnaKeys.Enter, XnaKeys.A }, ignoreOrder: true);
    }

    [Fact]
    public void KeysTyped_ShouldBeEmpty_WhenAHeldKeyIsNotRepeating()
    {
        Keyboard sut = CreateKeyboard(XnaKeys.A);

        sut.Activity(0);
        sut.Activity(0.01);

        sut.KeysTyped.ShouldBeEmpty();
    }

    [Fact]
    public void NumberOfKeys_ShouldMatchKniGum()
    {
        Keyboard.NumberOfKeys.ShouldBe(255);
    }

    // The obsolete XNA overloads mirror KniGum's until they are removed there (#5028), so this pins them.
#pragma warning disable CS0618
    [Fact]
    public void ObsoleteXnaKeysOverloads_ShouldMatchTheGumKeysOverloads()
    {
        Keyboard sut = CreateKeyboard(XnaKeys.A, XnaKeys.Enter);
        sut.Activity(0);

        sut.KeyDown(XnaKeys.A).ShouldBeTrue();
        sut.KeyDown(XnaKeys.B).ShouldBeFalse();
        sut.KeyPushed(XnaKeys.Enter).ShouldBeTrue();
        sut.KeyTyped(XnaKeys.Enter).ShouldBeTrue();
        sut.IsKeyLetter(XnaKeys.A).ShouldBeTrue();
        sut.IsKeyLetter(XnaKeys.D1).ShouldBeFalse();
        IInputReceiverKeyboard asInterface = sut;
        asInterface.KeyDown(XnaKeys.A).ShouldBeTrue();

        sut.KeyboardStateProcessor = new Mock<KeyboardStateProcessor>().Object;
        sut.Activity(0.1);

        sut.KeyReleased(XnaKeys.Enter).ShouldBeTrue();
        sut.KeyPushed(XnaKeys.Enter).ShouldBeFalse();
    }
#pragma warning restore CS0618

    [Fact]
    public void XnaKeysOverloads_ShouldAllBeObsoleteWithReplacementAndDates()
    {
        List<MethodInfo> xnaOverloads = new List<MethodInfo>();
        foreach (Type type in new[] { typeof(Keyboard), typeof(IInputReceiverKeyboardMonoGame), typeof(XnaKeyboardExtensions) })
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.GetParameters().Any(p => p.ParameterType == typeof(XnaKeys)))
                {
                    xnaOverloads.Add(method);
                }
            }
        }

        // Same count as KniGum: 4 on Keyboard, 4 on the interface, 4 extensions, plus Keyboard.IsKeyLetter.
        xnaOverloads.Count.ShouldBe(13);

        foreach (MethodInfo method in xnaOverloads)
        {
            ObsoleteAttribute? obsolete = method.GetCustomAttribute<ObsoleteAttribute>();
            obsolete.ShouldNotBeNull($"{method.DeclaringType!.Name}.{method.Name}(XnaKeys) must be [Obsolete]");
            obsolete!.IsError.ShouldBeFalse();
            string message = obsolete.Message ?? string.Empty;
            message.ShouldContain("Gum.Forms.Input.Keys");
            message.ShouldContain("March 2027");
            message.ShouldContain("July 2027");
        }
    }
}
