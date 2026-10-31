using Gum.ProjectServices;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// The test container's <see cref="IPackageAddRunner"/>: records each add and succeeds without starting <c>dotnet</c>
/// or touching the network. A test sets <see cref="Result"/> to make adds fail; reset it after the test.
/// </summary>
internal sealed class RecordingPackageAddRunner : IPackageAddRunner
{
    private readonly List<string> _added = new List<string>();

    /// <summary>The package ids added so far, in order.</summary>
    public IReadOnlyList<string> Added => _added;

    /// <summary>What every add returns; success by default.</summary>
    public Func<string, RestoreResult>? Result { get; set; }

    public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken)
    {
        lock (_added)
        {
            _added.Add(packageId);
        }

        return Task.FromResult(Result?.Invoke(packageId) ?? new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
    }

    public void Reset()
    {
        lock (_added)
        {
            _added.Clear();
        }

        Result = null;
    }
}
