using Gum.ProjectServices;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// An <see cref="IExistingCodeProjectDetector"/> that ignores any project file found outside <c>root</c>. The real detector
/// walks up to the drive root, so a stray <c>.csproj</c> or <c>.sln</c> in the machine's temp folder, which is above every
/// <see cref="NewProjectUiFixture"/> folder, would otherwise show the existing-code-project banner in tests that expect none.
/// </summary>
internal sealed class RootBoundedExistingCodeProjectDetector : IExistingCodeProjectDetector
{
    private readonly IExistingCodeProjectDetector _inner;
    private readonly string _root;

    public RootBoundedExistingCodeProjectDetector(IExistingCodeProjectDetector inner, string root)
    {
        _inner = inner;
        _root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    public string? FindExistingCodeProject(string folder)
    {
        string? found = _inner.FindExistingCodeProject(folder);
        return found != null && Path.GetFullPath(found).StartsWith(_root, StringComparison.OrdinalIgnoreCase) ? found : null;
    }

    public bool TargetExists(string folder, string name) => _inner.TargetExists(folder, name);

    public bool TargetExistsAndIsNotEmpty(string folder, string name) => _inner.TargetExistsAndIsNotEmpty(folder, name);
}
