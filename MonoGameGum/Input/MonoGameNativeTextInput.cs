#if MONOGAME || KNI
using Gum.Forms.Controls;
using System;
using System.Threading.Tasks;

namespace MonoGameGum.Input;

/// <summary>
/// MonoGame / KNI implementation of <see cref="INativeTextInput"/>. Wraps
/// <c>Microsoft.Xna.Framework.Input.KeyboardInput.Show</c>, which surfaces the
/// OS's modal text-entry dialog (used most visibly on iOS). Registered onto
/// <c>GumService.NativeTextInput</c> during initialization so platform-agnostic
/// callers like <c>TextBoxBase</c> can request native text entry through
/// <see cref="IGumService.NativeTextInput"/>.
/// </summary>
/// <remarks>
/// FNA does not ship <c>Microsoft.Xna.Framework.Input.KeyboardInput</c>, so this
/// type is excluded from the FNA build via the surrounding preprocessor gate.
/// Browser (Blazor) hosts skip the call at runtime — <c>KeyboardInput.Show</c>
/// is not implemented there and would hang or throw — and return a completed
/// null task so callers see a no-op rather than an exception.
/// </remarks>
internal sealed class MonoGameNativeTextInput : INativeTextInput
{
    public Task<string?> ShowAsync(string title, string description, string initialText, bool isPassword)
    {
        if (OperatingSystem.IsBrowser())
        {
            return Task.FromResult<string?>(null);
        }

        return Microsoft.Xna.Framework.Input.KeyboardInput.Show(title, description, initialText, isPassword)!;
    }
}
#endif
