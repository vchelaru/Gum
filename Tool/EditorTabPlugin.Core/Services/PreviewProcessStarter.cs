using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Finds the GumPreview executable and starts it. <see cref="PreviewLauncher"/> goes through this
/// so a test can click Preview without opening a real preview window.
/// </summary>
public interface IPreviewProcessStarter
{
    /// <summary>The preview executable to launch, or null when none is installed or built.</summary>
    ResolvedPreviewExecutable? Resolve();

    /// <summary>Starts the preview; null when the process could not be started.</summary>
    IPreviewProcess? Start(ProcessStartInfo startInfo);
}

/// <summary>How a preview process ended: its exit code and what it wrote to standard error.</summary>
public readonly record struct PreviewProcessExit(int ExitCode, string ErrorText);

/// <summary>A started preview process.</summary>
public interface IPreviewProcess
{
    /// <summary>Whether the preview has closed.</summary>
    bool HasExited { get; }

    /// <summary>Closes the preview. Does nothing if it has already exited. A close is not reported through <see cref="Exited"/>.</summary>
    void Close();

    /// <summary>
    /// Raised once, on any thread, when the preview exits by itself. A subscriber added after the
    /// exit is told immediately. The default does nothing, for stand-ins that never exit.
    /// </summary>
    event Action<PreviewProcessExit>? Exited
    {
        add { }
        remove { }
    }
}

/// <inheritdoc cref="IPreviewProcessStarter"/>
public class PreviewProcessStarter : IPreviewProcessStarter
{
    private readonly string _headBaseDirectory;

    /// <param name="headBaseDirectory">
    /// The running head's own base directory (<c>AppContext.BaseDirectory</c>), searched by
    /// <see cref="PreviewExecutableLocator"/>.
    /// </param>
    public PreviewProcessStarter(string headBaseDirectory)
    {
        _headBaseDirectory = headBaseDirectory;
    }

    /// <inheritdoc/>
    public ResolvedPreviewExecutable? Resolve() => PreviewExecutableLocator.Resolve(_headBaseDirectory);

    /// <inheritdoc/>
    public IPreviewProcess? Start(ProcessStartInfo startInfo)
    {
        Process process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        StartedProcess started = new StartedProcess(process);
        try
        {
            if (!started.Begin())
            {
                started.Dispose();
                return null;
            }
        }
        catch
        {
            started.Dispose();
            throw;
        }
        return started;
    }

    private sealed class StartedProcess : IPreviewProcess
    {
        // Enough for the one-line message GumPreview prints plus a few lines of a crash.
        private const int MaxErrorLines = 5;
        private const int MaxErrorLineLength = 500;

        private readonly Process _process;
        private readonly object _lock = new object();
        private readonly Queue<string> _errorLines = new Queue<string>();
        private Action<PreviewProcessExit>? _exited;
        private PreviewProcessExit? _exit;
        private bool _closedByTool;
        private bool _disposed;

        public StartedProcess(Process process)
        {
            _process = process;
        }

        public bool HasExited
        {
            get
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        return true;
                    }
                }
                try
                {
                    return _process.HasExited;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            }
        }

        public event Action<PreviewProcessExit>? Exited
        {
            add
            {
                PreviewProcessExit? alreadyExited;
                lock (_lock)
                {
                    alreadyExited = _exit;
                    if (alreadyExited == null)
                    {
                        _exited += value;
                    }
                }
                if (alreadyExited != null)
                {
                    value?.Invoke(alreadyExited.Value);
                }
            }
            remove
            {
                lock (_lock)
                {
                    _exited -= value;
                }
            }
        }

        public bool Begin()
        {
            // GumPreview is a WinExe with no console, so stderr is only reachable through a pipe.
            // The pipe is drained continuously (keeping the last few lines) so a chatty preview
            // can never block on a full pipe.
            _process.ErrorDataReceived += OnErrorData;
            _process.Exited += OnExited;
            if (!_process.Start())
            {
                return false;
            }
            _process.BeginErrorReadLine();
            return true;
        }

        private void OnErrorData(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }
            string line = e.Data.Length > MaxErrorLineLength ? e.Data.Substring(0, MaxErrorLineLength) : e.Data;
            lock (_lock)
            {
                _errorLines.Enqueue(line);
                while (_errorLines.Count > MaxErrorLines)
                {
                    _errorLines.Dequeue();
                }
            }
        }

        private void OnExited(object? sender, EventArgs e)
        {
            int exitCode;
            try
            {
                // Waits for the redirected stderr to reach end of stream, so the last line is in.
                _process.WaitForExit();
                exitCode = _process.ExitCode;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            Action<PreviewProcessExit>? handlers;
            PreviewProcessExit exit;
            lock (_lock)
            {
                if (_closedByTool || _exit != null)
                {
                    return;
                }
                exit = new PreviewProcessExit(exitCode, string.Join(Environment.NewLine, _errorLines));
                _exit = exit;
                handlers = _exited;
                _exited = null;
            }
            handlers?.Invoke(exit);
            Dispose();
        }

        // Killed rather than asked to close: macOS and Linux have no main window handle to ask, and
        // the preview holds nothing worth saving.
        public void Close()
        {
            lock (_lock)
            {
                _closedByTool = true;
                _exited = null;
            }
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // It exited between the check and the kill.
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Already closing, or not ours to kill; either way the tool is exiting.
            }
            Dispose();
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }
            _process.Dispose();
        }
    }
}
