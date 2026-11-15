using System;
using System.Runtime.InteropServices;
using System.Threading;
using GumPreview;

NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, SdlLibrary.Resolve);

if (Array.IndexOf(args, SdlLibrary.ProbeFlag) >= 0)
{
    return SdlLibrary.Probe(Console.Out);
}

PreviewOptions options = PreviewOptions.Parse(args);
if (options.Error != null)
{
    Console.Error.WriteLine(options.Error);
    Console.Error.WriteLine(PreviewOptions.Usage);
    return 1;
}

if (options.ParentProcessId is int parentProcessId)
{
    // Exit with the tool however it ended, so a killed or crashed tool can't orphan this window.
    ParentProcessWatcher.Start(parentProcessId, () => Environment.Exit(0));
}

UnattendedPreviewRun? unattended = null;
Timer? deadline = null;
if (options.ExitAfterSeconds is double exitAfterSeconds)
{
    unattended = new UnattendedPreviewRun(exitAfterSeconds);
    UnattendedWindow.KeepInBackground();
    // A thread-pool timer, so the run still ends when the game thread is stuck loading the project.
    deadline = new Timer(_ =>
    {
        string? message = unattended.TryExpire();
        if (message != null)
        {
            Console.Error.WriteLine(message);
            Console.Error.Flush();
            Environment.Exit(1);
        }
    }, null, TimeSpan.FromSeconds(exitAfterSeconds), Timeout.InfiniteTimeSpan);
}

// A path given by the tool or the command line is appended to (the tool rotates once per session, and
// Preview is relaunched during one); only Preview's own Debug default rotates.
string? logPath = PreviewDebugLog.ResolvePath(options.LogPath);
using PreviewDebugLog? debugLog = PreviewDebugLog.Open(logPath, rotate: options.LogPath == null);
if (debugLog != null)
{
    debugLog.Write("config", PreviewDebugLog.DescribeProcess(logPath, args));
    AppDomain.CurrentDomain.UnhandledException += (_, e) => debugLog.LogUnhandled(e.ExceptionObject);
}
using Game1 game = new Game1(options.ProjectPath!, options.ElementName!, options.SelectionFilePath, options.ContentRootDirectory,
    unattended, options.ScreenshotPath, options.FocusName, options.TypedText, options.SelectAll, debugLog);
game.Run();
deadline?.Dispose();
return game.UnattendedExitCode;
