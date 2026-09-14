namespace Gum.Messages;

/// <summary>
/// Broadcast when the <see cref="Gum.Settings.GeneralSettingsFile.UseStandardsPalette"/> setting
/// is toggled, so the element tree view can rebuild (showing or hiding the Standard folder) and the
/// chip palette can be shown or hidden.
/// </summary>
/// <param name="UseStandardsPalette">The new value of the setting.</param>
public record StandardsPaletteSettingChangedMessage(bool UseStandardsPalette);
