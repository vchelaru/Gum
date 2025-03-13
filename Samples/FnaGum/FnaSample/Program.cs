using System;
using System.Linq;

namespace FnaSample
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("FNA_PLATFORM_BACKEND", "SDL3");
            using (var game = new Game1())
            {
                var byEditor = args.Contains("LaunchedByEditor");

                if (byEditor)
                {
                    try
                    {
                        game.Run();
                    }
                    catch (Exception e)
                    {
                        System.IO.File.WriteAllText("CrashInfo.txt", e.ToString());
                        throw;
                    }
                }
                else
                {
                    game.Run();
                }

            }
        }
    }
}
