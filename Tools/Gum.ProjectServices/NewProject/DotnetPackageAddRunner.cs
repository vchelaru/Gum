using System;
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
        DotnetProcess.RunAsync(() => CreateStartInfo(csprojPath, packageId), onOutputLine, cancellationToken);

    // --no-restore writes Version="*" without a feed lookup, so adding works offline and the one restore afterwards
    // (or the user's first build) fetches everything.
    private static ProcessStartInfo CreateStartInfo(string csprojPath, string packageId)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet");
        startInfo.ArgumentList.Add("add");
        startInfo.ArgumentList.Add(csprojPath);
        startInfo.ArgumentList.Add("package");
        startInfo.ArgumentList.Add(packageId);
        startInfo.ArgumentList.Add("--no-restore");
        return startInfo;
    }
}
