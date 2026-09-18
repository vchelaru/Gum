using System.Collections.Generic;

namespace Gum.Diagnostics;

/// <summary>
/// The on-disk state behind "Gum froze last time; diagnostics are waiting": a dirty-shutdown
/// sentinel written at launch and removed on a clean exit, plus the freeze dumps the watchdog wrote
/// and whether the user has already been shown them. Nothing here compares timestamps: a dump is
/// "reported" once it has been moved into the Reported subfolder, so it can prompt at most once.
/// </summary>
public interface IFreezeDiagnosticsInbox
{
    /// <summary>The folder the watchdog writes to and this inbox manages.</summary>
    string DirectoryPath { get; }

    /// <summary>True once the user has asked not to be prompted again.</summary>
    bool ArePromptsSuppressed { get; }

    /// <summary>
    /// Writes the dirty-shutdown sentinel. Returns true when a sentinel from the previous session
    /// was still there, meaning that session ended without <see cref="EndSessionCleanly"/>.
    /// </summary>
    bool BeginSession();

    /// <summary>Removes the sentinel so the next launch reads as a clean exit.</summary>
    void EndSessionCleanly();

    /// <summary>Full paths of the diagnostic files the user has not been shown yet.</summary>
    IReadOnlyList<string> GetUnreportedFiles();

    /// <summary>Moves <paramref name="files"/> into the Reported subfolder.</summary>
    void MarkReported(IEnumerable<string> files);

    /// <summary>Stops future launches from prompting, until the marker it writes is deleted by hand.</summary>
    void SuppressPrompts();
}
