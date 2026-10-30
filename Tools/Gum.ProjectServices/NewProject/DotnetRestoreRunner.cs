using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class DotnetRestoreRunner : IRestoreRunner
{
    private const int TailLineCount = 20;

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
    public async Task<RestoreResult> RestoreAsync(
        string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new RestoreResult { Status = RestoreStatus.Canceled };
        }

        ProcessStartInfo startInfo = _createStartInfo(csprojPath);
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        Queue<string> tail = new Queue<string>();
        void OnLine(string? line)
        {
            if (line == null)
            {
                return;
            }

            lock (tail)
            {
                tail.Enqueue(line);
                while (tail.Count > TailLineCount)
                {
                    tail.Dequeue();
                }
            }

            onOutputLine?.Invoke(line);
        }

        using Process process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => OnLine(e.Data);
        process.ErrorDataReceived += (_, e) => OnLine(e.Data);

        try
        {
            process.Start();
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            return new RestoreResult { Status = RestoreStatus.ProcessNotFound, ErrorMessage = exception.Message };
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                // Already exited.
            }

            return new RestoreResult { Status = RestoreStatus.Canceled };
        }

        string outputTail;
        lock (tail)
        {
            outputTail = string.Join(Environment.NewLine, tail);
        }

        return new RestoreResult
        {
            Status = process.ExitCode == 0 ? RestoreStatus.Succeeded : RestoreStatus.Failed,
            ExitCode = process.ExitCode,
            OutputTail = outputTail
        };
    }

    private static ProcessStartInfo CreateDotnetRestoreStartInfo(string csprojPath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet");
        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(csprojPath);
        return startInfo;
    }
}
