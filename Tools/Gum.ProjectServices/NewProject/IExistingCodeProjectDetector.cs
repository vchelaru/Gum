namespace Gum.ProjectServices;

/// <summary>
/// Looks at a folder before a project is created in it.
/// </summary>
public interface IExistingCodeProjectDetector
{
    /// <summary>
    /// Finds a <c>.csproj</c>, <c>.sln</c> or <c>.slnx</c> in <paramref name="folder"/> or the nearest parent that has one
    /// (up to the drive root), skipping <c>bin</c> and <c>obj</c> folders. The folder itself need not exist yet.
    /// </summary>
    /// <returns>The full path of the file found, or <see langword="null"/>.</returns>
    string? FindExistingCodeProject(string folder);

    /// <summary>Whether <c>&lt;folder&gt;/&lt;name&gt;</c> exists and has anything in it.</summary>
    bool TargetExistsAndIsNotEmpty(string folder, string name);
}
