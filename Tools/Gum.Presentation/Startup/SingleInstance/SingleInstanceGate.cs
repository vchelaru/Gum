using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gum.Startup.SingleInstance;

/// <summary>What <see cref="SingleInstanceGate.Enter"/> decided for this launch.</summary>
public enum GateOutcome
{
    /// <summary>No Gum owns the project; this process now does and listens for requests.</summary>
    Owner,

    /// <summary>A Gum that owns the project accepted the request; this process should exit.</summary>
    HandedOff,

    /// <summary>The owner did not take the request; this process starts as an ordinary, unclaimed Gum.</summary>
    Unclaimed,
}

/// <summary>
/// The single-instance decision at launch: claim the project with a lock file, or hand the request
/// to the Gum that already holds it. The claim and the pipe exist before any project loads, so a
/// launch that arrives while the owner is still starting connects and its request is queued.
/// </summary>
public sealed class SingleInstanceGate : IDisposable
{
    private static readonly Encoding _utf8 = new UTF8Encoding(false);

    private readonly ProjectInstanceLock? _lock;

    private SingleInstanceGate(GateOutcome outcome, string? message, ProjectInstanceLock? instanceLock, OpenRequestServer? inbox)
    {
        Outcome = outcome;
        Message = message;
        _lock = instanceLock;
        Inbox = inbox;
    }

    /// <summary>What this launch should do.</summary>
    public GateOutcome Outcome { get; }

    /// <summary>Why the launch is <see cref="GateOutcome.Unclaimed"/>; null otherwise.</summary>
    public string? Message { get; }

    /// <summary>The request inbox, for <see cref="GateOutcome.Owner"/> only.</summary>
    public OpenRequestServer? Inbox { get; }

    /// <summary>
    /// Claims <paramref name="request"/>'s project or hands the request to its owner, waiting up to
    /// <paramref name="ownerWait"/> for an owner that holds the lock but is not listening yet.
    /// <paramref name="onServerPid"/> receives the owner's process id before the request is sent, so
    /// the caller can let that process take the foreground (Windows).
    /// </summary>
    public static SingleInstanceGate Enter(OpenRequest request, string instanceDirectory, string userName, bool caseSensitive,
        TimeSpan ownerWait, Action<int>? onServerPid)
    {
        string key = InstanceIdentity.KeyFor(request.Project, userName, caseSensitive);
        string pipeName = InstanceIdentity.PipeName(key);
        DateTime deadline = DateTime.UtcNow + ownerWait;
        string? lastProblem = null;

        while (true)
        {
            ProjectInstanceLock? instanceLock = ProjectInstanceLock.TryAcquire(instanceDirectory, key);
            if (instanceLock != null)
            {
                OpenRequestServer server = new OpenRequestServer(pipeName)
                {
                    Validate = incoming => InstanceIdentity.KeyFor(incoming.Project, userName, caseSensitive) == key
                        ? null
                        : "This Gum has a different project open.",
                };
                server.Start();
                return new SingleInstanceGate(GateOutcome.Owner, null, instanceLock, server);
            }

            ExchangeResult result = TryExchange(pipeName, request, onServerPid);
            if (result.Delivered)
            {
                return new SingleInstanceGate(GateOutcome.HandedOff, null, null, null);
            }

            if (result.Refusal != null)
            {
                return new SingleInstanceGate(GateOutcome.Unclaimed, "The running Gum refused: " + result.Refusal, null, null);
            }

            lastProblem = result.Problem;
            if (DateTime.UtcNow >= deadline)
            {
                return new SingleInstanceGate(GateOutcome.Unclaimed,
                    "The running Gum did not answer" + (lastProblem == null ? "." : ": " + lastProblem), null, null);
            }

            Thread.Sleep(50);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Inbox?.Dispose();
        _lock?.Dispose();
    }

    private readonly record struct ExchangeResult(bool Delivered, string? Refusal, string? Problem);

    private static ExchangeResult TryExchange(string pipeName, OpenRequest request, Action<int>? onServerPid)
    {
        try
        {
            using NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.CurrentUserOnly);
            client.Connect(250);
            using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using StreamReader reader = new StreamReader(client, _utf8, false, 4096, leaveOpen: true);
            using StreamWriter writer = new StreamWriter(client, _utf8, 4096, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };

            string? helloLine = reader.ReadLineAsync(timeout.Token).AsTask().GetAwaiter().GetResult();
            if (helloLine == null || !OpenRequestMessage.TryParse(helloLine, out InstanceHello? hello) || hello!.Version != OpenRequestMessage.Version)
            {
                return new ExchangeResult(false, null, "it is not a compatible Gum");
            }

            onServerPid?.Invoke(hello.Pid);
            writer.WriteLine(OpenRequestMessage.Serialize(request));
            string? responseLine = reader.ReadLineAsync(timeout.Token).AsTask().GetAwaiter().GetResult();
            if (responseLine == null || !OpenRequestMessage.TryParse(responseLine, out OpenResponse? response))
            {
                return new ExchangeResult(false, null, "it sent no reply");
            }

            writer.WriteLine("bye");
            return response!.Ok
                ? new ExchangeResult(true, null, null)
                : new ExchangeResult(false, response.Error ?? "no reason given", null);
        }
        catch (Exception exception) when (exception is TimeoutException or IOException or OperationCanceledException or UnauthorizedAccessException)
        {
            return new ExchangeResult(false, null, exception.Message);
        }
    }
}
