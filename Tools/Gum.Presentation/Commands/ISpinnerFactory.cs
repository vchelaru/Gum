namespace Gum.Commands;

/// <summary>
/// Constructs and shows an <see cref="ISpinner"/>. Exists so <see cref="IGuiCommands.ShowSpinner"/>
/// can obtain a spinner without naming the concrete WPF <see cref="Gum.Controls.Spinner"/> type
/// directly. The WPF implementation is <c>Gum.Controls.SpinnerFactory</c>.
/// </summary>
public interface ISpinnerFactory
{
    /// <summary>
    /// Creates a new spinner, shows it, and returns it.
    /// </summary>
    ISpinner Create();
}
