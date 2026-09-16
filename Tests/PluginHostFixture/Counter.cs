namespace PluginHostFixture;

/// <summary>
/// A static counter used only to prove isolation in <c>IsolatedPluginHostTests</c>: each
/// AssemblyLoadContext that loads this assembly gets its own independent <see cref="Value"/>.
/// </summary>
public static class Counter
{
    public static int Value;

    public static int IncrementAndGet() => ++Value;
}
