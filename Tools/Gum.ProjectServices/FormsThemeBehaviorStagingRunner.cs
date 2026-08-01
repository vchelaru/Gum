using System;
using System.IO;

namespace Gum.ProjectServices;

/// <summary>
/// Thin glue around <see cref="IFormsThemeBehaviorStagingService"/> shared by every caller that
/// needs to run forms-behavior staging as a standalone step (a CLI command, a build-time postbuild
/// step) without each caller re-implementing file validation and error reporting.
/// </summary>
public static class FormsThemeBehaviorStagingRunner
{
    /// <summary>
    /// Validates <paramref name="projectGumxPath"/> exists, stages its behaviors into
    /// <paramref name="destinationBehaviorsDirectory"/>, and reports the result to <see cref="Console"/>.
    /// </summary>
    /// <returns>0 on success, 2 if the project file is missing or staging fails.</returns>
    public static int Run(string projectGumxPath, string destinationBehaviorsDirectory)
    {
        string fullProjectPath = Path.GetFullPath(projectGumxPath);

        if (!File.Exists(fullProjectPath))
        {
            Console.Error.WriteLine($"Project file not found: {fullProjectPath}");
            return 2;
        }

        IFormsThemeBehaviorStagingService stagingService = new FormsThemeBehaviorStagingService();
        try
        {
            int stagedCount = stagingService.Stage(fullProjectPath, Path.GetFullPath(destinationBehaviorsDirectory));
            Console.WriteLine($"Staged {stagedCount} behavior(s) to {destinationBehaviorsDirectory}");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }
}
