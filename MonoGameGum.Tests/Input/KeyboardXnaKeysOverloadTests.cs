using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace MonoGameGum.Tests.Input;

public class KeyboardXnaKeysOverloadTests
{
    private static Keyboard CreateKeyboard(Mock<KeyboardStateProcessor> processor)
    {
        Keyboard keyboard = new Keyboard();
        keyboard.KeyboardStateProcessor = processor.Object;
        return keyboard;
    }

    [Fact]
    public void KeyDown_WithGumKeys_ShouldReadKeyboardState()
    {
        Mock<KeyboardStateProcessor> processor = new Mock<KeyboardStateProcessor>();
        processor.Setup(x => x.IsKeyDown(XnaKeys.A)).Returns(true);
        Keyboard sut = CreateKeyboard(processor);

        sut.KeyDown(GumKeys.A).ShouldBeTrue();
        sut.KeyDown(GumKeys.B).ShouldBeFalse();
    }

    [Fact]
    public void KeyPushedAndReleased_WithGumKeys_ShouldReadKeyboardState()
    {
        Mock<KeyboardStateProcessor> processor = new Mock<KeyboardStateProcessor>();
        processor.Setup(x => x.KeyPushed(XnaKeys.Left)).Returns(true);
        processor.Setup(x => x.KeyReleased(XnaKeys.Right)).Returns(true);
        Keyboard sut = CreateKeyboard(processor);

        sut.KeyPushed(GumKeys.Left).ShouldBeTrue();
        sut.KeyPushed(GumKeys.Right).ShouldBeFalse();
        sut.KeyReleased(GumKeys.Right).ShouldBeTrue();
        sut.KeyReleased(GumKeys.Left).ShouldBeFalse();
    }

    [Fact]
    public void KeyTyped_WithGumKeys_ShouldBeTrueAfterKeyIsPushedInActivity()
    {
        Mock<KeyboardStateProcessor> processor = new Mock<KeyboardStateProcessor>();
        processor.Setup(x => x.KeyPushed(XnaKeys.Enter)).Returns(true);
        Keyboard sut = CreateKeyboard(processor);

        sut.Activity(0);

        sut.KeyTyped(GumKeys.Enter).ShouldBeTrue();
        sut.KeyTyped(GumKeys.Escape).ShouldBeFalse();
    }

    [Fact]
    public void KeyDown_OnInterface_WithGumKeys_ShouldReadKeyboardState()
    {
        Mock<KeyboardStateProcessor> processor = new Mock<KeyboardStateProcessor>();
        processor.Setup(x => x.IsKeyDown(XnaKeys.Space)).Returns(true);
        IInputReceiverKeyboard sut = CreateKeyboard(processor);

        sut.KeyDown(GumKeys.Space).ShouldBeTrue();
    }

    [Fact]
    public void IsKeyLetter_WithGumKeys_ShouldOnlyBeTrueForLetters()
    {
        Keyboard sut = new Keyboard();

        sut.IsKeyLetter(GumKeys.A).ShouldBeTrue();
        sut.IsKeyLetter(GumKeys.Z).ShouldBeTrue();
        sut.IsKeyLetter(GumKeys.D1).ShouldBeFalse();
    }

    // The obsolete XNA overloads keep working until they are removed (#5028), so this pins them.
#pragma warning disable CS0618
    [Fact]
    public void ObsoleteXnaKeysOverloads_ShouldStillReadKeyboardState()
    {
        Mock<KeyboardStateProcessor> processor = new Mock<KeyboardStateProcessor>();
        processor.Setup(x => x.IsKeyDown(XnaKeys.A)).Returns(true);
        processor.Setup(x => x.KeyPushed(XnaKeys.Enter)).Returns(true);
        processor.Setup(x => x.KeyReleased(XnaKeys.Escape)).Returns(true);
        Keyboard sut = CreateKeyboard(processor);

        sut.Activity(0);

        sut.KeyDown(XnaKeys.A).ShouldBeTrue();
        sut.KeyPushed(XnaKeys.Enter).ShouldBeTrue();
        sut.KeyReleased(XnaKeys.Escape).ShouldBeTrue();
        sut.KeyTyped(XnaKeys.Enter).ShouldBeTrue();
        sut.IsKeyLetter(XnaKeys.A).ShouldBeTrue();
        sut.IsKeyLetter(XnaKeys.D1).ShouldBeFalse();
        IInputReceiverKeyboard asInterface = sut;
        asInterface.KeyDown(XnaKeys.A).ShouldBeTrue();
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

        // 4 on Keyboard, 4 on the interface, 4 extensions, plus Keyboard.IsKeyLetter.
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
