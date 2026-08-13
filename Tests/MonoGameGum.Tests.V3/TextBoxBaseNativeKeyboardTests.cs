using Gum;
using Gum.Forms.Controls;
using Gum.Threading;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V3;

/// <summary>
/// Behavior tests for TextBoxBase's two native-keyboard paths: the inline soft keyboard
/// surfaced through <see cref="FrameworkElement.MainKeyboard"/> (Android), and the modal
/// <see cref="INativeTextInput"/> dialog registered on
/// <see cref="IGumService.NativeTextInput"/> (iOS and anything else that supplies one).
/// Which path is taken is decided at runtime by
/// <see cref="IInputReceiverKeyboard.SupportsInlineKeyboard"/>, so both are reachable from
/// this desktop test assembly.
/// </summary>
public class TextBoxBaseNativeKeyboardTests
{
    [Fact]
    public void TryShowNativeKeyboard_WhenShowOnFocusTrue_CallsRegisteredNativeTextInput()
    {
        StubNativeTextInput stubInput = new StubNativeTextInput();
        StubGumService stubService = new StubGumService { NativeTextInput = stubInput };
        StubKeyboard stubKeyboard = new StubKeyboard { SupportsInlineKeyboard = false };
        IGumService? prior = IGumService.Default;
        IInputReceiverKeyboard? priorKeyboard = FrameworkElement.MainKeyboard;
        IGumService.Default = stubService;
        FrameworkElement.MainKeyboard = stubKeyboard;
        try
        {
            TextBox textBox = new TextBox();
            textBox.ShowNativeKeyboardOnFocus = true;
            textBox.NativeKeyboardTitle = "TITLE";
            textBox.NativeKeyboardDescription = "DESC";
            textBox.Text = "INIT";

            textBox.TryShowNativeKeyboard();

            stubInput.CallCount.ShouldBe(1);
            stubInput.LastTitle.ShouldBe("TITLE");
            stubInput.LastDescription.ShouldBe("DESC");
            stubInput.LastInitialText.ShouldBe("INIT");
            stubInput.LastIsPassword.ShouldBe(false);
            stubKeyboard.ShowCallCount.ShouldBe(0);
        }
        finally
        {
            IGumService.Default = prior;
            FrameworkElement.MainKeyboard = priorKeyboard;
        }
    }

    [Fact]
    public void TryShowNativeKeyboard_WhenMainKeyboardSupportsInline_ShowsInlineKeyboardInsteadOfDialog()
    {
        StubNativeTextInput stubInput = new StubNativeTextInput();
        StubGumService stubService = new StubGumService { NativeTextInput = stubInput };
        StubKeyboard stubKeyboard = new StubKeyboard { SupportsInlineKeyboard = true };
        IGumService? prior = IGumService.Default;
        IInputReceiverKeyboard? priorKeyboard = FrameworkElement.MainKeyboard;
        IGumService.Default = stubService;
        FrameworkElement.MainKeyboard = stubKeyboard;
        try
        {
            TextBox textBox = new TextBox();
            textBox.ShowNativeKeyboardOnFocus = true;

            textBox.TryShowNativeKeyboard();

            stubKeyboard.ShowCallCount.ShouldBe(1);
            stubInput.CallCount.ShouldBe(0);
        }
        finally
        {
            IGumService.Default = prior;
            FrameworkElement.MainKeyboard = priorKeyboard;
        }
    }

    [Fact]
    public void TryHideNativeKeyboard_WhenMainKeyboardSupportsInline_HidesInlineKeyboard()
    {
        StubKeyboard stubKeyboard = new StubKeyboard { SupportsInlineKeyboard = true };
        IInputReceiverKeyboard? priorKeyboard = FrameworkElement.MainKeyboard;
        FrameworkElement.MainKeyboard = stubKeyboard;
        try
        {
            TextBox textBox = new TextBox();

            textBox.TryHideNativeKeyboard();

            stubKeyboard.HideCallCount.ShouldBe(1);
        }
        finally
        {
            FrameworkElement.MainKeyboard = priorKeyboard;
        }
    }

    [Fact]
    public void IsFocused_SetFalseAfterInlineKeyboardShown_HidesInlineKeyboard()
    {
        GumService.Default.InitializeForTesting();
        StubKeyboard stubKeyboard = new StubKeyboard { SupportsInlineKeyboard = true };
        IInputReceiverKeyboard? priorKeyboard = FrameworkElement.MainKeyboard;
        FrameworkElement.MainKeyboard = stubKeyboard;
        try
        {
            TextBox textBox = new TextBox();
            textBox.ShowNativeKeyboardOnFocus = true;
            textBox.AddToRoot();

            // Mirrors how a real tap acquires focus (HandleClick/HandlePush set
            // CurrentInputReceiver directly, which cascades into OnGainFocus -> IsFocused =
            // true) rather than assigning IsFocused directly, which reenters through that
            // same cascade and double-counts the show call.
            InteractiveGue.CurrentInputReceiver = textBox;
            stubKeyboard.ShowCallCount.ShouldBe(1);

            // FrameworkElement.IsFocused's base setter clears CurrentInputReceiver back to
            // null as soon as focus is lost, before TextBoxBase.UpdateToIsFocused runs its
            // own "am I still the current receiver" check on the way to TryHideNativeKeyboard.
            // Gating the hide call on that check meant it never fired. Pins the keyboard
            // actually dismissing on focus loss.
            textBox.IsFocused = false;

            stubKeyboard.HideCallCount.ShouldBe(1);
        }
        finally
        {
            FrameworkElement.MainKeyboard = priorKeyboard;
            InteractiveGue.CurrentInputReceiver = null;
            GumService.Default.Root.Children.Clear();
        }
    }

    [Fact]
    public void TryShowNativeKeyboard_WhenShowOnFocusFalse_DoesNotCallNativeTextInput()
    {
        StubNativeTextInput stubInput = new StubNativeTextInput();
        StubGumService stubService = new StubGumService { NativeTextInput = stubInput };
        IGumService? prior = IGumService.Default;
        IGumService.Default = stubService;
        try
        {
            TextBox textBox = new TextBox();
            textBox.ShowNativeKeyboardOnFocus = false;

            textBox.TryShowNativeKeyboard();

            stubInput.CallCount.ShouldBe(0);
        }
        finally
        {
            IGumService.Default = prior;
        }
    }

    [Fact]
    public void TryShowNativeKeyboard_WhenNativeTextInputNotRegistered_IsNoOp()
    {
        StubGumService stubService = new StubGumService { NativeTextInput = null };
        IGumService? prior = IGumService.Default;
        IGumService.Default = stubService;
        try
        {
            TextBox textBox = new TextBox();
            textBox.ShowNativeKeyboardOnFocus = true;

            Should.NotThrow(() => textBox.TryShowNativeKeyboard());
        }
        finally
        {
            IGumService.Default = prior;
        }
    }

    private class StubKeyboard : IInputReceiverKeyboard
    {
        public bool SupportsInlineKeyboard { get; set; }
        public int ShowCallCount { get; private set; }
        public int HideCallCount { get; private set; }

        public bool IsShiftDown => false;
        public bool IsCtrlDown => false;
        public bool IsAltDown => false;
        public IEnumerable<Gum.Forms.Input.Keys> KeysTyped => System.Array.Empty<Gum.Forms.Input.Keys>();

        public void ShowKeyboard() => ShowCallCount++;
        public void HideKeyboard() => HideCallCount++;

        public string GetStringTyped() => string.Empty;
        public void Activity(double gameTime) { }
        public bool KeyDown(Gum.Forms.Input.Keys key) => false;
        public bool KeyPushed(Gum.Forms.Input.Keys key) => false;
        public bool KeyReleased(Gum.Forms.Input.Keys key) => false;
        public bool KeyTyped(Gum.Forms.Input.Keys key) => false;
    }

    private class StubNativeTextInput : INativeTextInput
    {
        public int CallCount { get; private set; }
        public string? LastTitle { get; private set; }
        public string? LastDescription { get; private set; }
        public string? LastInitialText { get; private set; }
        public bool LastIsPassword { get; private set; }

        public Task<string?> ShowAsync(string title, string description, string initialText, bool isPassword)
        {
            CallCount++;
            LastTitle = title;
            LastDescription = description;
            LastInitialText = initialText;
            LastIsPassword = isPassword;
            return Task.FromResult<string?>(null);
        }
    }

    private class StubGumService : IGumService
    {
        public bool IsInitialized => true;
        public IRenderer Renderer => null!;
        public ICursor Cursor => null!;
        public float CanvasWidth { get; set; }
        public float CanvasHeight { get; set; }
        public InteractiveGue Root => null!;
        public DeferredActionQueue DeferredQueue { get; } = new DeferredActionQueue();
        public float? GameTime => null;
        public INativeTextInput? NativeTextInput { get; set; }
        public IGumClipboard? Clipboard { get; set; }

        public void Initialize() { }
        public void Initialize(string gumProjectFile) { }
        public void Draw() { }
        public RenderingLibrary.Graphics.IRenderable CreateSpriteRenderable() => null!;
    }
}
