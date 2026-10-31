using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class DotnetRestoreRunner : IRestoreRunner
{
    private readonly Func<string, ProcessStartInfo> _createStartInfo;

    public DotnetRestoreRunner() : this(CreateDotnetRestoreStartInfo)
    {
    }

    /// <summary>For tests: supplies the process to run instead of <c>dotnet restore</c>.</summary>
    internal DotnetRestoreRunner(Func<string, ProcessStartInfo> createStartInfo)
    {
        _createStartInfo = createStartInfo;
    }

    /// <inheritdoc/>
    public Task<RestoreResult> RestoreAsync(
        string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
        DotnetProcess.RunAsync(_createStartInfo(csprojPath), onOutputLine, cancellationToken);

    private static ProcessStartInfo CreateDotnetRestoreStartInfo(string csprojPath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet");
        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(csprojPath);
        return startInfo;
    }
}
