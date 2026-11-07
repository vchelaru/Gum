using Gum.Managers;
using Moq;

namespace Gum.Presentation.Tests;

/// <summary>
/// Builds a real <see cref="ProjectRestrictions"/> that reads an in-memory <see cref="EngineProfile"/>
/// instead of a file.
/// </summary>
internal static class TestProjectRestrictions
{
    public static ProjectRestrictions Create(EngineProfile? profile = null)
    {
        ProjectRestrictions restrictions = new ProjectRestrictions(new EngineProfileLoader(), Mock.Of<IOutputManager>());
        restrictions.UseProfile(profile ?? new EngineProfile());
        return restrictions;
    }
}
