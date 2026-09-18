using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Shouldly;
using System;
using System.Collections.Generic;

namespace MonoGameGum.Tests.V3;

/// <summary>
/// Covers <see cref="FormsUtilities.SetKeyboard"/>, the keyboard-side counterpart of
/// <see cref="FormsUtilities.SetCursor"/>. Tests capture and restore
/// <see cref="FormsUtilities.Keyboard"/> so subsequent tests in the suite are not affected.
/// </summary>
public class FormsUtilitiesKeyboardTests
{
    [Fact]
    public void SetKeyboard_InstallsInstanceOnFormsUtilitiesKeyboardAndFrameworkElementMainKeyboard()
    {
        IInputReceiverKeyboard? priorKeyboard = FormsUtilities.Keyboard;
        StubKeyboard stubKeyboard = new StubKeyboard();

        try
        {
            FormsUtilities.SetKeyboard(stubKeyboard);

            FormsUtilities.Keyboard.ShouldBeSameAs(stubKeyboard);
            FrameworkElement.MainKeyboard.ShouldBeSameAs(stubKeyboard);
        }
        finally
        {
            FormsUtilities.SetKeyboard(priorKeyboard);
        }
    }

    [Fact]
    public void FocusedTextBox_AfterUpdate_ReceivesCharactersFromInstalledKeyboard()
    {
        IInputReceiverKeyboard? priorKeyboard = FormsUtilities.Keyboard;
        StubKeyboard stubKeyboard = new StubKeyboard { StringTyped = "hi" };

        try
        {
            GumService.Default.InitializeForTesting();
            FormsUtilities.SetKeyboard(stubKeyboard);

            TextBox textBox = new TextBox();
            textBox.AddToRoot();
            // Mirrors how a real tap acquires focus (HandleClick/HandlePush set
            // CurrentInputReceiver directly, which cascades into OnGainFocus -> IsFocused = true).
            InteractiveGue.CurrentInputReceiver = textBox;

            FormsUtilities.Update(null!, new GameTime(TimeSpan.Zero, TimeSpan.Zero), GumService.Default.Root);

            textBox.Text.ShouldBe("hi");
        }
        finally
        {
            InteractiveGue.CurrentInputReceiver = null;
            GumService.Default.Root.Children.Clear();
            FormsUtilities.SetKeyboard(priorKeyboard);
        }
    }

    private class StubKeyboard : IInputReceiverKeyboard
    {
        public string StringTyped { get; set; } = string.Empty;

        public bool IsShiftDown => false;
        public bool IsCtrlDown => false;
        public bool IsAltDown => false;
        public IEnumerable<Gum.Forms.Input.Keys> KeysTyped => Array.Empty<Gum.Forms.Input.Keys>();

        public string GetStringTyped() => StringTyped;
        public void Activity(double gameTime) { }
        public bool KeyDown(Gum.Forms.Input.Keys key) => false;
        public bool KeyPushed(Gum.Forms.Input.Keys key) => false;
        public bool KeyReleased(Gum.Forms.Input.Keys key) => false;
        public bool KeyTyped(Gum.Forms.Input.Keys key) => false;
    }
}
