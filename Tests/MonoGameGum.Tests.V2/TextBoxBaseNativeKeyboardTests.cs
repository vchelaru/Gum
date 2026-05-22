using Gum.Forms.Controls;
using Gum.Threading;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2;

/// <summary>
/// Behavior tests for TextBoxBase's interaction with the platform-agnostic
/// <see cref="INativeTextInput"/> abstraction. These verify that the iOS / generic
/// modal-keyboard path on TextBoxBase delegates to whatever
/// <see cref="IGumService.NativeTextInput"/> is registered, rather than calling
/// MonoGame's <c>KeyboardInput.Show</c> directly.
/// </summary>
public class TextBoxBaseNativeKeyboardTests
{
    [Fact]
    public void TryShowNativeKeyboard_WhenShowOnFocusTrue_CallsRegisteredNativeTextInput()
    {
        StubNativeTextInput stubInput = new StubNativeTextInput();
        StubGumService stubService = new StubGumService { NativeTextInput = stubInput };
        IGumService? prior = IGumService.Default;
        IGumService.Default = stubService;
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
        }
        finally
        {
            IGumService.Default = prior;
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
