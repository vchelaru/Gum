using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// Attaches an in-memory <see cref="EngineProfile"/> to the head's real <see cref="IProjectRestrictions"/>
/// for the length of a test, without touching disk.
/// </summary>
internal sealed class EngineProfileScope : IDisposable
{
    private readonly ProjectRestrictions _restrictions;

    public EngineProfileScope(IServiceProvider services)
    {
        _restrictions = (ProjectRestrictions)services.GetRequiredService<IProjectRestrictions>();
        Profile = new EngineProfile();
        _restrictions.UseProfile(Profile);
    }

    /// <summary>The profile the tool reads; edit it to change what the project restricts.</summary>
    public EngineProfile Profile { get; }

    public void Dispose() => _restrictions.UseProfile(null);
}
