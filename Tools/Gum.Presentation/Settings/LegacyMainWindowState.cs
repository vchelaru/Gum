namespace Gum.Settings;

/// <summary>
/// Mirrors <c>System.Windows.Forms.FormWindowState</c>'s member names exactly (<c>Normal</c>,
/// <c>Minimized</c>, <c>Maximized</c>) so a <c>GeneralSettings.xml</c> file written by an older
/// version of the tool (which serialized the real WinForms enum) keeps deserializing correctly --
/// <see cref="System.Xml.Serialization.XmlSerializer"/> round-trips an enum by its member name, not
/// its underlying integer value, so matching names is sufficient. This neutral copy exists purely so
/// <see cref="GeneralSettingsFile"/> can live in the headless Gum.Presentation assembly (ADR-0005);
/// it is read only once, by <c>LayoutSettings.MigrateLegacyLayout</c>, to migrate the legacy value
/// into <c>WindowSettings.IsMaximized</c>. Never written by current code.
/// </summary>
public enum LegacyMainWindowState
{
    Normal,
    Minimized,
    Maximized
}
