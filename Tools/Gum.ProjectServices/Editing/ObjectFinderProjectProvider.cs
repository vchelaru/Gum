using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Provides the project <see cref="ObjectFinder"/> resolves, for headless callers such as <c>gumcli edit</c>.
/// </summary>
public class ObjectFinderProjectProvider : IReferenceFinderProjectProvider
{
    /// <inheritdoc/>
    public GumProjectSave? GumProjectSave => ObjectFinder.Self.GumProjectSave;
}
