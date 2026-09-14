namespace WpfDataUi.DataTypes;

/// <summary>
/// How a row's shown value relates to its default. Displayers turn this into their own field tint
/// (green for default, gray for indeterminate, the theme field color otherwise).
/// </summary>
public enum DataUiValueState
{
    /// <summary>The value is set explicitly on the edited object.</summary>
    Custom,

    /// <summary>The value is inherited or otherwise the default.</summary>
    Default,

    /// <summary>A multi-selection whose objects disagree on the value.</summary>
    Indeterminate,
}
