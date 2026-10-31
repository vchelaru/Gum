using Gum.ProjectServices;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// An <see cref="IExistingCodeProjectDetector"/> whose folder checks take time on the worker thread, to make a test that
/// reads their result without waiting fail on a fast machine: <see cref="Delay"/> sleeps in every check pass, and
/// <see cref="Gate"/>, while reset, holds the pass until the test sets it. Set <c>GUM_SLOW_CHECKS_MS</c> to slow every
/// <see cref="NewProjectUiFixture"/>.
/// </summary>
internal sealed class SlowExistingCodeProjectDetector : IExistingCodeProjectDetector
{
    /// <summary>The environment variable that makes every New Project UI fixture's checks take this many milliseconds.</summary>
    public const string EnvironmentVariable = "GUM_SLOW_CHECKS_MS";

    private static readonly TimeSpan GateLimit = TimeSpan.FromSeconds(60);

    private readonly IExistingCodeProjectDetector _inner;

    public SlowExistingCodeProjectDetector(IExistingCodeProjectDetector inner, TimeSpan delay)
    {
        _inner = inner;
        Delay = delay;
    }

    public TimeSpan Delay { get; set; }

    /// <summary>Held reset to block the checks; starts set.</summary>
    public ManualResetEventSlim Gate { get; } = new ManualResetEventSlim(initialState: true);

    /// <summary>The delay the environment asks for, or null when it asks for none.</summary>
    public static TimeSpan? DelayFromEnvironment()
    {
        string? text = Environment.GetEnvironmentVariable(EnvironmentVariable);
        return int.TryParse(text, out int milliseconds) && milliseconds > 0 ? TimeSpan.FromMilliseconds(milliseconds) : null;
    }

    public string? FindExistingCodeProject(string folder)
    {
        Slow();
        return _inner.FindExistingCodeProject(folder);
    }

    public bool TargetExists(string folder, string name) => _inner.TargetExists(folder, name);

    public bool TargetExistsAndIsNotEmpty(string folder, string name) => _inner.TargetExistsAndIsNotEmpty(folder, name);

    private void Slow()
    {
        Gate.Wait(GateLimit);
        if (Delay > TimeSpan.Zero)
        {
            Thread.Sleep(Delay);
        }
    }
}
