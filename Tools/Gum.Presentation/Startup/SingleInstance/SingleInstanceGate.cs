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

    /// <summary>The launch claimed nothing; this process starts as an ordinary Gum.</summary>
    Unclaimed,
}

/// <summary>
/// The single-instance decision at launch: claim the project with a lock file, or hand the request
/// to the Gum that already holds it. The claim and the pipe exist before any project loads, so a
/// launch that arrives while the owner is still starting connects and its request is queued. The
/// gate lives as long as the process; <see cref="Reclaim"/> moves the claim when the user loads
/// another project.
/// </summary>
public sealed class SingleInstanceGate : IDisposable
{
    private static readonly Encoding _utf8 = new UTF8Encoding(false);

    private readonly string _instanceDirectory;
    private readonly string _userName;
    private readonly bool _caseSensitive;
    private readonly object _claimGate = new object();
    private ProjectInstanceLock? _lock;
    private OpenRequestServer? _server;
    private string? _claimedKey;

    private SingleInstanceGate(GateOutcome outcome, string? message, OpenRequestInbox? inbox,
        string instanceDirectory, string userName, bool caseSensitive)
    {
        Outcome = outcome;
        Message = message;
        Inbox = inbox;
        _instanceDirectory = instanceDirectory;
        _userName = userName;
        _caseSensitive = caseSensitive;
    }

    /// <summary>What this launch should do.</summary>
    public GateOutcome Outcome { get; private set; }

    /// <summary>Why the launch is <see cref="GateOutcome.Unclaimed"/>; null when there is nothing to report.</summary>
    public string? Message { get; }

    /// <summary>
    /// Where requests from later launches arrive; null for <see cref="GateOutcome.HandedOff"/>. It stays
    /// the same object when the claimed project changes.
    /// </summary>
    public OpenRequestInbox? Inbox { get; }

    /// <summary>
    /// Claims <paramref name="request"/>'s project or hands the request to its owner, waiting up to
    /// <paramref name="ownerWait"/> for an owner that holds the lock but is not listening yet.
    /// <paramref name="onServerPid"/> receives the owner's process id before the request is sent, so
    /// the caller can let that process take the foreground (Windows).
    /// </summary>
    public static SingleInstanceGate Enter(OpenRequest request, string instanceDirectory, string userName, bool caseSensitive,
        TimeSpan ownerWait, Action<int>? onServerPid)
    {
        if (TryPrepareDirectory(instanceDirectory) is { } directoryProblem)
        {
            return StartUnclaimed(instanceDirectory, userName, caseSensitive, directoryProblem);
        }

        string key = InstanceIdentity.KeyFor(request.Project, userName, caseSensitive);
        string pipeName = InstanceIdentity.PipeName(key);
        DateTime deadline = DateTime.UtcNow + ownerWait;
        SingleInstanceGate gate = StartUnclaimed(instanceDirectory, userName, caseSensitive, null);

        while (true)
        {
            if (gate.TryClaim(key))
            {
                gate.Outcome = GateOutcome.Owner;
                return gate;
            }

            ExchangeResult result = TryExchange(pipeName, request, onServerPid);
            if (result.Delivered)
            {
                return new SingleInstanceGate(GateOutcome.HandedOff, null, null, instanceDirectory, userName, caseSensitive);
            }

            if (result.Refusal != null)
            {
                return StartUnclaimed(instanceDirectory, userName, caseSensitive, "The running Gum refused: " + result.Refusal);
            }

            if (DateTime.UtcNow >= deadline)
            {
                return StartUnclaimed(instanceDirectory, userName, caseSensitive,
                    "The running Gum did not answer" + (result.Problem == null ? "." : ": " + result.Problem));
            }

            Thread.Sleep(50);
        }
    }

    /// <summary>
    /// A gate that claims nothing yet but receives requests once <see cref="Reclaim"/> claims a
    /// project. Used by a launch that names no project, and by one that could not hand off.
    /// </summary>
    public static SingleInstanceGate StartUnclaimed(string instanceDirectory, string userName, bool caseSensitive, string? message = null) =>
        new SingleInstanceGate(GateOutcome.Unclaimed, message, new OpenRequestInbox(), instanceDirectory, userName, caseSensitive);

    /// <summary>
    /// Makes <paramref name="projectPath"/> the project this process owns, after letting go of the
    /// one it owned before; null owns none. Returns false, owning nothing, when another Gum already
    /// owns the project (the user loaded a project that is open elsewhere) or the claim cannot be
    /// made. Requests already queued and the attached handler carry over.
    /// </summary>
    public bool Reclaim(string? projectPath)
    {
        lock (_claimGate)
        {
            string? key = projectPath == null ? null : InstanceIdentity.KeyFor(projectPath, _userName, _caseSensitive);
            if (key != null && key == _claimedKey)
            {
                return true;
            }

            Release();
            return key == null || (TryPrepareDirectory(_instanceDirectory) == null && TryClaim(key));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_claimGate)
        {
            Release();
        }
    }

    private bool TryClaim(string key)
    {
        ProjectInstanceLock? instanceLock = ProjectInstanceLock.TryAcquire(_instanceDirectory, key);
        if (instanceLock == null)
        {
            return false;
        }

        OpenRequestServer server = new OpenRequestServer(InstanceIdentity.PipeName(key), Inbox!)
        {
            Validate = incoming => InstanceIdentity.KeyFor(incoming.Project, _userName, _caseSensitive) == key
                ? null
                : "This Gum has a different project open.",
        };
        server.Start();
        _lock = instanceLock;
        _server = server;
        _claimedKey = key;
        return true;
    }

    // The pipe closes before the lock opens the project to another Gum.
    private void Release()
    {
        _server?.Dispose();
        _lock?.Dispose();
        _server = null;
        _lock = null;
        _claimedKey = null;
    }

    // Null when the folder is ready; otherwise why it cannot be used.
    private static string? TryPrepareDirectory(string instanceDirectory)
    {
        try
        {
            InstanceIdentity.EnsurePrivateDirectory(instanceDirectory);
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return "The folder for single-instance claims cannot be used: " + exception.Message;
        }
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
