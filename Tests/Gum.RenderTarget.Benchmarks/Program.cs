using System;
using System.IO;
using System.Threading;

namespace Gum.RenderTarget.Benchmarks;

public static class Program
{
    public static int Main(string[] args)
    {
        BenchOptions options = BenchOptions.Parse(args);
        Directory.CreateDirectory(options.OutputFolder);

        // Backstop for a hang inside the graphics driver; the game loop checks the same limit itself.
        Thread watchdog = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromSeconds(options.TimeoutSeconds + 30));
            File.AppendAllText(Path.Combine(options.OutputFolder, "log.txt"), "WATCHDOG: hard exit\n");
            Environment.Exit(3);
        });
        watchdog.IsBackground = true;
        watchdog.Start();

        using BenchGame game = new BenchGame(options);
        game.Run();
        return game.ExitCode;
    }
}
