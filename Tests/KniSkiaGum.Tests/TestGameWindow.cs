using System.Reflection;
using XnaGameWindow = Microsoft.Xna.Framework.GameWindow;
using XnaTextInputEventArgs = Microsoft.Xna.Framework.Input.TextInputEventArgs;

namespace KniSkiaGum.Tests;

/// <summary>
/// A <see cref="XnaGameWindow"/> with no platform behind it, so a test can raise typed text and count who
/// is listening. A real window needs a KNI platform package.
/// </summary>
internal sealed class TestGameWindow : XnaGameWindow
{
    public override bool AllowUserResizing { get; set; }
    public override Microsoft.Xna.Framework.Rectangle ClientBounds => default;
    public override Microsoft.Xna.Framework.DisplayOrientation CurrentOrientation => default;
    public override nint Handle => 0;
    public override string ScreenDeviceName => "";

    protected override void SetTitle(string title)
    {
    }

    /// <summary>
    /// Raises <c>TextInput</c> as the platform would.
    /// </summary>
    public void RaiseTextInput(char character)
    {
        FieldInfo field = typeof(XnaGameWindow).GetField("TextInput", BindingFlags.Instance | BindingFlags.NonPublic)!;
        EventHandler<XnaTextInputEventArgs>? handler = (EventHandler<XnaTextInputEventArgs>?)field.GetValue(this);
        handler?.Invoke(this, new XnaTextInputEventArgs(Microsoft.Xna.Framework.Input.Keys.None, character));
    }
}
