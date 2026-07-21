using Gum.Commands;

namespace Gum.Controls;

/// <summary>
/// WPF implementation of <see cref="ISpinnerFactory"/>. Constructs and shows a
/// <see cref="Spinner"/> window.
/// </summary>
public class SpinnerFactory : ISpinnerFactory
{
    /// <inheritdoc/>
    public ISpinner Create()
    {
        Spinner spinner = new Spinner();
        spinner.Show();

        return spinner;
    }
}
