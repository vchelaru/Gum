using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class ExistingCodeProjectDetector : IExistingCodeProjectDetector
{
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(2);

    private static readonly string[] CodeProjectPatterns = { "*.csproj", "*.sln", "*.slnx" };

    /// <inheritdoc/>
    public string? FindExistingCodeProject(string folder)
    {
        // An unreachable network share can block a file system call for a long time; give up after a bound.
        Task<string?> search = Task.Run(() => Search(folder));
        return search.Wait(SearchTimeout) ? search.Result : null;
    }

    private static string? Search(string folder)
    {
        DirectoryInfo? directory;
        try
        {
            directory = new DirectoryInfo(Path.GetFullPath(folder));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        while (directory != null)
        {
            bool isBuildOutput =
                string.Equals(directory.Name, "bin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(directory.Name, "obj", StringComparison.OrdinalIgnoreCase);

            if (!isBuildOutput && directory.Exists)
            {
                foreach (string pattern in CodeProjectPatterns)
                {
                    string? match = SafeGetFiles(directory, pattern)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();
                    if (match != null)
                    {
                        return match;
                    }
                }
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <inheritdoc/>
    public bool TargetExistsAndIsNotEmpty(string folder, string name)
    {
        string target = Path.Combine(folder, name);
        return Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any();
    }

    private static string[] SafeGetFiles(DirectoryInfo directory, string pattern)
    {
        try
        {
            return Directory.GetFiles(directory.FullName, pattern);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            return Array.Empty<string>();
        }
    }
}
