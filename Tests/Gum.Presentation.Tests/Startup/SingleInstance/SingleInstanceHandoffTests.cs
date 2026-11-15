using Gum.Startup.SingleInstance;
using Shouldly;

namespace Gum.Presentation.Tests.Startup.SingleInstance;

/// <summary>
/// The handoff over a real pipe and a real lock file, each test in its own folder under a unique
/// user name so parallel tests and a running Gum never share a name.
/// </summary>
public class SingleInstanceHandoffTests : IDisposable
{
    private readonly string _instanceDirectory;
    private readonly string _user;
    private readonly string _project;
    private readonly List<IDisposable> _disposables = new List<IDisposable>();

    public SingleInstanceHandoffTests()
    {
        _user = "test-" + Guid.NewGuid().ToString("N");
        _instanceDirectory = Path.Combine(Path.GetTempPath(), "GumHandoffTests", _user);
        _project = Path.Combine(Path.GetTempPath(), "GumHandoffTests", _user, "Ui", "Ui.gumx");
    }

    public void Dispose()
    {
        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }

        try
        {
            Directory.Delete(_instanceDirectory, true);
        }
        catch (IOException)
        {
        }
    }

    private SingleInstanceGate Enter(string? project = null, TimeSpan? wait = null)
    {
        SingleInstanceGate gate = SingleInstanceGate.Enter(
            new OpenRequest(project ?? _project, "Main"), _instanceDirectory, _user, caseSensitive: false, wait ?? TimeSpan.FromSeconds(5), onServerPid: null);
        _disposables.Add(gate);
        return gate;
    }

    [Fact]
    public void Enter_FirstLaunch_BecomesTheOwner()
    {
        SingleInstanceGate gate = Enter();

        gate.Outcome.ShouldBe(GateOutcome.Owner);
        gate.Inbox.ShouldNotBeNull();
    }

    [Fact]
    public async Task Enter_SecondLaunchOfTheSameProject_HandsOffAndDeliversTheRequest()
    {
        SingleInstanceGate owner = Enter();
        List<OpenRequest> received = new List<OpenRequest>();
        owner.Inbox!.Attach(request =>
        {
            lock (received)
            {
                received.Add(request);
            }
            return Task.CompletedTask;
        });

        SingleInstanceGate second = await Task.Run(() => Enter());

        second.Outcome.ShouldBe(GateOutcome.HandedOff);
        await WaitUntilAsync(() => received.Count == 1);
        received[0].ShouldBe(new OpenRequest(_project, "Main"));
    }

    [Fact]
    public async Task Enter_LaunchWhileOwnerIsStillStarting_QueuesUntilTheOwnerAttaches()
    {
        SingleInstanceGate owner = Enter();

        SingleInstanceGate second = await Task.Run(() => Enter());
        second.Outcome.ShouldBe(GateOutcome.HandedOff);
        SingleInstanceGate third = await Task.Run(() => Enter());
        third.Outcome.ShouldBe(GateOutcome.HandedOff);

        List<OpenRequest> received = new List<OpenRequest>();
        owner.Inbox!.Attach(request =>
        {
            received.Add(request);
            return Task.CompletedTask;
        });

        received.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Enter_DifferentProject_IsItsOwnOwner()
    {
        Enter();
        string other = Path.Combine(Path.GetDirectoryName(_project)!, "Other", "Other.gumx");

        SingleInstanceGate second = await Task.Run(() => Enter(other));

        second.Outcome.ShouldBe(GateOutcome.Owner);
    }

    [Fact]
    public async Task Enter_AfterTheOwnerExits_TheNextLaunchOwnsTheProject()
    {
        SingleInstanceGate owner = Enter();
        owner.Dispose();

        SingleInstanceGate next = await Task.Run(() => Enter());

        next.Outcome.ShouldBe(GateOutcome.Owner);
    }

    [Fact]
    public async Task Enter_OwnerRefusesTheRequest_ReportsRejectedWithoutBecomingOwner()
    {
        SingleInstanceGate owner = Enter();
        owner.Inbox!.Validate = _ => "busy";

        SingleInstanceGate second = await Task.Run(() => Enter());

        second.Outcome.ShouldBe(GateOutcome.Unclaimed);
        second.Message!.ShouldContain("busy");
    }

    [Fact]
    public async Task Enter_LockHeldButNobodyListening_TimesOutAndStartsUnclaimed()
    {
        // A process that holds the lock but never opens the pipe (hung before it could).
        using ProjectInstanceLock held = ProjectInstanceLock.TryAcquire(_instanceDirectory,
            InstanceIdentity.KeyFor(_project, _user, false))!;

        SingleInstanceGate second = await Task.Run(() => Enter(wait: TimeSpan.FromMilliseconds(400)));

        second.Outcome.ShouldBe(GateOutcome.Unclaimed);
        second.Message!.ShouldContain("did not answer");
    }

    [Fact]
    public async Task OwnerServer_ReportsItsPidToTheClientBeforeTheRequestIsSent()
    {
        SingleInstanceGate owner = Enter();
        int? pid = null;

        SingleInstanceGate second = await Task.Run(() => SingleInstanceGate.Enter(
            new OpenRequest(_project, null), _instanceDirectory, _user, false, TimeSpan.FromSeconds(5), p => pid = p));
        _disposables.Add(second);

        pid.ShouldBe(Environment.ProcessId);
    }

    [Fact]
    public async Task Server_MalformedLine_AnswersNotOkAndKeepsServing()
    {
        SingleInstanceGate owner = Enter();
        string key = InstanceIdentity.KeyFor(_project, _user, false);

        using (System.IO.Pipes.NamedPipeClientStream client = new System.IO.Pipes.NamedPipeClientStream(".", InstanceIdentity.PipeName(key),
            System.IO.Pipes.PipeDirection.InOut, System.IO.Pipes.PipeOptions.CurrentUserOnly))
        {
            await client.ConnectAsync(2000);
            using StreamReader reader = new StreamReader(client);
            using StreamWriter writer = new StreamWriter(client) { AutoFlush = true };
            (await reader.ReadLineAsync()).ShouldContain("hello");
            await writer.WriteLineAsync("garbage");
            OpenRequestMessage.TryParse((await reader.ReadLineAsync())!, out OpenResponse? response).ShouldBeTrue();
            response!.Ok.ShouldBeFalse();
        }

        (await Task.Run(() => Enter())).Outcome.ShouldBe(GateOutcome.HandedOff);
    }

    [Fact]
    public void ProjectInstanceLock_SecondAcquireFailsUntilTheFirstIsReleased()
    {
        string key = InstanceIdentity.KeyFor(_project, _user, false);
        ProjectInstanceLock? first = ProjectInstanceLock.TryAcquire(_instanceDirectory, key);
        first.ShouldNotBeNull();

        ProjectInstanceLock.TryAcquire(_instanceDirectory, key).ShouldBeNull();
        ProjectInstanceLock.IsHeld(_instanceDirectory, key).ShouldBeTrue();

        first.Dispose();
        ProjectInstanceLock.IsHeld(_instanceDirectory, key).ShouldBeFalse();
        using ProjectInstanceLock? again = ProjectInstanceLock.TryAcquire(_instanceDirectory, key);
        again.ShouldNotBeNull();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The condition was not met in 5 seconds.");
            }

            await Task.Delay(20);
        }
    }
}
