namespace Gum.Avalonia.Diagnostics.CrashReporting;

/// <summary>The crash-report consent, kept in the per-user appsettings.json.</summary>
public class CrashReportingSettings
{
    /// <summary>True to send crash reports, false not to, null while the user has not been asked.</summary>
    public bool? SendCrashReports { get; set; }
}
