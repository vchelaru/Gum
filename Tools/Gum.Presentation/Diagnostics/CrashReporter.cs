using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Gum.Diagnostics;

/// <inheritdoc cref="ICrashReporter"/>
/// <remarks>
/// A survived error is written as <c>error-*.txt</c>; a fatal one as <c>crash-*.txt</c>, which stays
/// in the folder root until <see cref="PromptForPreviousCrash"/> has shown it. The user is told about
/// survived errors only once per session, so an error that repeats (every frame, say) can't bury
/// the tool in dialogs.
/// </remarks>
public sealed class CrashReporter : ICrashReporter
{
    /// <summary>The most log files one session writes, so an error repeating every frame can't fill the disk.</summary>
    public const int MaxLogFilesPerSession = 20;

    private const string RecoverablePrefix = "error-";
    private const string FatalPrefix = "crash-";
    private const string ReportedFolderName = "Reported";
    private const string IssuesUrl = "github.com/vchelaru/Gum/issues";

    private readonly string _toolVersion;
    private readonly Action<string> _notifyUser;
    private readonly object _lock;
    private int _logFilesWritten;
    private bool _hasNotifiedThisSession;

    /// <param name="directory">The folder the logs go to, normally <c>CrashLogs</c> under the user's Gum data folder.</param>
    /// <param name="toolVersion">The version written at the top of every log.</param>
    /// <param name="notifyUser">Shows a message to the user. May be called from any thread.</param>
    public CrashReporter(string directory, string toolVersion, Action<string> notifyUser)
    {
        DirectoryPath = directory;
        _toolVersion = toolVersion;
        _notifyUser = notifyUser;
        _lock = new object();
    }

    /// <inheritdoc/>
    public string DirectoryPath { get; }

    /// <inheritdoc/>
    public string? ReportRecoverable(Exception exception, string source)
    {
        string? logPath;
        bool shouldNotify;
        lock (_lock)
        {
            logPath = TryWriteLog(RecoverablePrefix, exception, source, ignoreCap: false);
            shouldNotify = !_hasNotifiedThisSession;
            _hasNotifiedThisSession = true;
        }

        if (shouldNotify)
        {
            string where = logPath != null
                ? "Details were saved to:\n" + logPath
                : "The details could not be saved: " + exception.Message;
            TryNotify(
                "Gum hit an unexpected error and kept running. If something looks wrong, restart Gum.\n\n" +
                where + "\n\n" +
                "Please attach it to a GitHub issue at " + IssuesUrl + ". " +
                "Further errors this session are saved to the same folder without this message.");
        }

        return logPath;
    }

    /// <inheritdoc/>
    public string? ReportFatal(Exception exception, string source)
    {
        lock (_lock)
        {
            // The crash itself matters more than the errors before it, so it ignores the session cap.
            return TryWriteLog(FatalPrefix, exception, source, ignoreCap: true);
        }
    }

    /// <inheritdoc/>
    public void PromptForPreviousCrash()
    {
        List<string> reportedPaths = new List<string>();
        try
        {
            if (!Directory.Exists(DirectoryPath))
            {
                return;
            }

            string[] crashLogs = Directory.GetFiles(DirectoryPath, FatalPrefix + "*.txt");
            if (crashLogs.Length == 0)
            {
                return;
            }

            string reportedFolder = Path.Combine(DirectoryPath, ReportedFolderName);
            Directory.CreateDirectory(reportedFolder);
            foreach (string crashLog in crashLogs)
            {
                string reportedPath = Path.Combine(reportedFolder, Path.GetFileName(crashLog));
                File.Move(crashLog, reportedPath, overwrite: true);
                reportedPaths.Add(reportedPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("Could not read crash logs: " + exception);
            return;
        }

        TryNotify(
            "Gum closed unexpectedly last time. Details were saved to:\n" +
            string.Join("\n", reportedPaths) + "\n\n" +
            "Please attach them to a GitHub issue at " + IssuesUrl + ".");
    }

    private string? TryWriteLog(string prefix, Exception exception, string source, bool ignoreCap)
    {
        Console.Error.WriteLine($"Unhandled exception ({source}): {exception}");
        if (!ignoreCap && _logFilesWritten >= MaxLogFilesPerSession)
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(DirectoryPath);
            DateTime now = DateTime.Now;
            string path = GetUnusedPath(prefix + now.ToString("yyyyMMdd-HHmmss-fff"));
            StringBuilder log = new StringBuilder();
            log.AppendLine("Gum version: " + _toolVersion);
            log.AppendLine("Time: " + now.ToString("o"));
            log.AppendLine("Source: " + source);
            log.AppendLine("OS: " + RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + ")");
            log.AppendLine(".NET: " + RuntimeInformation.FrameworkDescription);
            log.AppendLine();
            log.AppendLine(exception.ToString());
            File.WriteAllText(path, log.ToString());
            _logFilesWritten++;
            return path;
        }
        catch (Exception writeException) when (writeException is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("Could not write the crash log: " + writeException);
            return null;
        }
    }

    private string GetUnusedPath(string baseName)
    {
        string path = Path.Combine(DirectoryPath, baseName + ".txt");
        for (int suffix = 2; File.Exists(path); suffix++)
        {
            path = Path.Combine(DirectoryPath, baseName + "-" + suffix + ".txt");
        }
        return path;
    }

    // Reporting runs inside the process's last-chance handlers, so a failure to show the message
    // must not become a second unhandled exception.
    private void TryNotify(string message)
    {
        try
        {
            _notifyUser(message);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Could not show the error message: " + exception);
        }
    }
}
