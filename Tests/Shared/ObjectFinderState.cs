#nullable enable
using Gum.Managers;

namespace Gum.Tests.Shared;

/// <summary>
/// Resets the project and fallback Standard Elements on <see cref="ObjectFinder.Self"/>, a process-wide
/// singleton. A test base class calls <see cref="Reset"/> when a test ends, so a project one test loads
/// or a fallback it registers does not change how a later test resolves elements and font paths (#6502).
/// </summary>
public static class ObjectFinderState
{
    public static void Reset()
    {
        ObjectFinder.Self.GumProjectSave = null;
        ObjectFinder.Self.ClearFallbackStandardElements();
    }
}
