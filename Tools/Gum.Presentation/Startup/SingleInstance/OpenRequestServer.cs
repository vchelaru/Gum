using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gum.Startup.SingleInstance;

/// <summary>Where a running Gum receives open requests from later launches.</summary>
public interface IOpenRequestInbox
{
    /// <summary>
    /// Starts delivering requests to <paramref name="handler"/>, first the ones that arrived while
    /// the tool was still starting, in arrival order. The handler is called on a thread-pool thread.
    /// </summary>
    void Attach(Func<OpenRequest, Task> handler);
}

/// <summary>
/// Listens on a per-user named pipe and queues each valid <see cref="OpenRequest"/> until a handler
/// is attached. Create it before the tool starts loading so a launch during startup connects and
/// queues. The pipe is restricted to the current user (<see cref="PipeOptions.CurrentUserOnly"/>).
/// </summary>
public sealed class OpenRequestServer : IOpenRequestInbox, IDisposable
{
    private static readonly Encoding _utf8 = new UTF8Encoding(false);

    private readonly string _pipeName;
    // Cancelled but never disposed: the accept loop and in-flight clients may still read its token.
    private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
    private readonly object _gate = new object();
    private readonly Queue<OpenRequest> _queued = new Queue<OpenRequest>();
    private Func<OpenRequest, Task>? _handler;
    private NamedPipeServerStream _waiting;
    private Task _acceptLoop = Task.CompletedTask;

    /// <summary>
    /// Returns an error to refuse a request, or null to accept it. Runs before the request is queued.
    /// </summary>
    public Func<OpenRequest, string?> Validate { get; set; } = _ => null;

    /// <summary>Opens the pipe, so a client that connects from now on waits instead of failing.</summary>
    public OpenRequestServer(string pipeName)
    {
        _pipeName = pipeName;
        DeleteStaleSocket(pipeName);
        _waiting = CreateStream();
    }

    /// <summary>Starts accepting connections.</summary>
    public void Start() => _acceptLoop = Task.Run(AcceptLoopAsync);

    /// <inheritdoc/>
    public void Attach(Func<OpenRequest, Task> handler)
    {
        lock (_gate)
        {
            _handler = handler;
            while (_queued.Count > 0)
            {
                Deliver(handler, _queued.Dequeue());
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cancellation.Cancel();
        _waiting.Dispose();
        try
        {
            _acceptLoop.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }

    }

    // The socket file of a crashed owner stays behind on macOS and Linux; the caller holds the
    // instance lock, so nobody is listening on it.
    private static void DeleteStaleSocket(string pipeName)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.Delete(Path.Combine(Path.GetTempPath(), "CoreFxPipe_" + pipeName));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private NamedPipeServerStream CreateStream() =>
        new NamedPipeServerStream(_pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

    private async Task AcceptLoopAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            NamedPipeServerStream current = _waiting;
            try
            {
                await current.WaitForConnectionAsync(_cancellation.Token);
            }
            catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
            {
                current.Dispose();
                return;
            }
            catch (IOException)
            {
                current.Dispose();
                _waiting = CreateStream();
                continue;
            }

            // A new listener first, so a client arriving while this one is served still connects.
            _waiting = CreateStream();
            _ = Task.Run(() => ServeAsync(current));
        }
    }

    private async Task ServeAsync(NamedPipeServerStream stream)
    {
        using (stream)
        {
            try
            {
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                using StreamReader reader = new StreamReader(stream, _utf8, false, 4096, leaveOpen: true);
                using StreamWriter writer = new StreamWriter(stream, _utf8, 4096, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };

                await writer.WriteLineAsync(OpenRequestMessage.Serialize(new InstanceHello(OpenRequestMessage.Version, Environment.ProcessId)));
                string? line = await reader.ReadLineAsync(timeout.Token);
                OpenResponse response = Accept(line);
                await writer.WriteLineAsync(OpenRequestMessage.Serialize(response));
                // The client reads the reply before it closes; wait for it so the reply is not cut off.
                await reader.ReadLineAsync(timeout.Token);
            }
            catch (Exception exception) when (exception is IOException or OperationCanceledException or ObjectDisposedException)
            {
                // A client that vanished or stalled costs nothing; the next one is served.
            }
        }
    }

    private OpenResponse Accept(string? line)
    {
        string? error = null;
        OpenRequest? request = null;
        if (line == null || !OpenRequestMessage.TryParse(line, out request, out error))
        {
            return new OpenResponse(false, error ?? "The request is empty.");
        }

        if (Validate(request!) is { } refusal)
        {
            return new OpenResponse(false, refusal);
        }

        lock (_gate)
        {
            if (_handler is { } handler)
            {
                Deliver(handler, request!);
            }
            else
            {
                _queued.Enqueue(request!);
            }
        }

        return new OpenResponse(true, null);
    }

    // Called in arrival order, under the lock; the handler hops to the UI thread itself.
    private static void Deliver(Func<OpenRequest, Task> handler, OpenRequest request)
    {
        try
        {
            Task task = handler(request);
            _ = task.ContinueWith(
                failed => Console.Error.WriteLine("Handling an open request failed: " + failed.Exception),
                TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Handling an open request failed: " + exception);
        }
    }
}
