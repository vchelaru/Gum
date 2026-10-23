#nullable enable
using ToolsUtilities;

namespace Gum.Tests.Shared;

/// <summary>
/// The process-wide <see cref="FileManager"/> settings that decide where files resolve from. A test
/// base class captures them when a test starts and restores them when it ends, so a test that changes
/// one (directly, or through GumService Initialize/Uninitialize or a project load) does not change
/// which font or file every later test in the process loads (#6496).
/// </summary>
public sealed class FileManagerState
{
    private readonly string _relativeDirectory;
    private readonly bool? _isMobileOverride;
    private readonly string? _userApplicationDataFolderOverride;

    private FileManagerState()
    {
        _relativeDirectory = FileManager.RelativeDirectory;
        _isMobileOverride = FileManager.IsMobileOverride;
        _userApplicationDataFolderOverride = FileManager.UserApplicationDataFolderOverride;
    }

    public static FileManagerState Capture() => new FileManagerState();

    /// <summary>
    /// Restores the captured values and clears <see cref="FileManager.CustomGetStreamFromFile"/>, whose
    /// default is no hook.
    /// </summary>
    public void Restore()
    {
        // First: the RelativeDirectory setter reads it to decide whether the path is relative.
        FileManager.IsMobileOverride = _isMobileOverride;
        FileManager.RelativeDirectory = _relativeDirectory;
        FileManager.UserApplicationDataFolderOverride = _userApplicationDataFolderOverride;
        FileManager.CustomGetStreamFromFile = null;
    }
}
