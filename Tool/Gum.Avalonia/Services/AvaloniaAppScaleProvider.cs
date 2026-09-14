using Gum.Services;

namespace Gum.Avalonia.Services;

/// <summary>
/// Holds the user's base font size. The main window applies it as its <c>FontSize</c>, which
/// Avalonia's controls inherit, so scaling works the same way the WPF <c>AppScale</c> resource did.
/// </summary>
public class AvaloniaAppScaleProvider : IAppScaleProvider
{
    private const double DefaultBaseFontSize = 12;
    private double _baseFontSize;

    /// <summary>Creates the provider at the default size.</summary>
    public AvaloniaAppScaleProvider()
    {
        _baseFontSize = DefaultBaseFontSize;
    }

    /// <summary>Raised when <see cref="BaseFontSize"/> changes, so the window can re-apply it.</summary>
    public event System.Action? BaseFontSizeChanged;

    /// <inheritdoc/>
    public double BaseFontSize
    {
        get => _baseFontSize;
        set
        {
            if (_baseFontSize != value)
            {
                _baseFontSize = value;
                BaseFontSizeChanged?.Invoke();
            }
        }
    }
}
