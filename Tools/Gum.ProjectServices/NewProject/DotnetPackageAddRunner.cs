using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class DotnetPackageAddRunner : IPackageAddRunner
{
    /// <inheritdoc/>
    public Task<RestoreResult> AddAsync(
        string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
        AddAsync(csprojPath, packageId, prerelease: false, onOutputLine, cancellationToken);

    /// <inheritdoc/>
    public Task<RestoreResult> AddAsync(
        string csprojPath, string packageId, bool prerelease, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
        DotnetProcess.RunAsync(() => CreateStartInfo(csprojPath, packageId, prerelease), onOutputLine, cancellationToken);

    // --no-restore writes Version="*" (Version="*-*" with --prerelease) without a feed lookup, so adding works offline and the
    // one restore afterwards (or the user's first build) fetches everything.
    private static ProcessStartInfo CreateStartInfo(string csprojPath, string packageId, bool prerelease)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet");
        foreach (string argument in GetArguments(csprojPath, packageId, prerelease))
        {
            startInfo.ArgumentList.Add(argument);
        }
        return startInfo;
    }

    /// <summary>The <c>dotnet</c> arguments of one add: <c>add &lt;csproj&gt; package &lt;id&gt; --no-restore</c>, plus <c>--prerelease</c> when asked.</summary>
    public static IReadOnlyList<string> GetArguments(string csprojPath, string packageId, bool prerelease)
    {
        List<string> arguments = new List<string> { "add", csprojPath, "package", packageId, "--no-restore" };
        if (prerelease)
        {
            arguments.Add("--prerelease");
        }
        return arguments;
    }
}
