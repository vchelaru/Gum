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

using Game1 game = new Game1(options.ProjectPath!, options.ElementName!, options.SelectionFilePath, options.ContentRootDirectory,
    unattended, options.ScreenshotPath);
game.Run();
deadline?.Dispose();
return game.UnattendedExitCode;
