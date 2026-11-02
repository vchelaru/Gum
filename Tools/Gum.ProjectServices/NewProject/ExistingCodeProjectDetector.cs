using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class ExistingCodeProjectDetector : IExistingCodeProjectDetector
{
    private static readonly TimeSpan DefaultSearchTimeout = TimeSpan.FromSeconds(2);

    private static readonly string[] CodeProjectPatterns = { "*.csproj", "*.sln", "*.slnx" };

    private readonly TimeSpan _searchTimeout;
    private readonly Func<string, string?> _search;

    /// <param name="searchTimeout">How long a search may take before it gives up with null. Defaults to 2 seconds.</param>
    /// <param name="search">The folder walk, for tests. Defaults to the real one.</param>
    public ExistingCodeProjectDetector(TimeSpan? searchTimeout = null, Func<string, string?>? search = null)
    {
        _searchTimeout = searchTimeout ?? DefaultSearchTimeout;
        _search = search ?? Search;
    }

    /// <inheritdoc/>
    public string? FindExistingCodeProject(string folder)
    {
        // An unreachable network share can block a file system call for a long time; give up after a bound.
        // A dedicated thread, so repeated hangs cannot starve the thread pool. A search that never returns is
        // abandoned (background thread), and its failure is swallowed so nothing is left unobserved.
        TaskCompletionSource<string?> completion = new TaskCompletionSource<string?>();
        Thread thread = new Thread(() =>
        {
            try
            {
                completion.TrySetResult(_search(folder));
            }
            catch (Exception)
            {
                completion.TrySetResult(null);
            }
        })
        {
            IsBackground = true
        };
        thread.Start();

        return completion.Task.Wait(_searchTimeout) ? completion.Task.Result : null;
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
    public bool TargetExists(string folder, string name) => Directory.Exists(Path.Combine(folder, name));

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
