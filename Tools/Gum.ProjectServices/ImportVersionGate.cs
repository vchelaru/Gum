namespace Gum.ProjectServices;

/// <summary>
/// The one version check shared by every path that copies Gum files into the open project
/// (Import from .gumx, Add Forms Components).
/// </summary>
public interface IImportVersionGate
{
    /// <summary>
    /// Returns the message to show when content saved at <paramref name="sourceVersion"/> cannot be
    /// imported into a project at <paramref name="destinationVersion"/>, or null when the import is allowed.
    /// </summary>
    string? GetRejection(int sourceVersion, int destinationVersion, string sourceDescription);
}

/// <inheritdoc/>
public class ImportVersionGate : IImportVersionGate
{
    private const string UpgradeDocsUrl = "https://docs.flatredball.com/gum/gum-tool/upgrading/upgrading-file-gumx-version";

    /// <inheritdoc/>
    public string? GetRejection(int sourceVersion, int destinationVersion, string sourceDescription)
    {
        if (sourceVersion <= destinationVersion)
        {
            return null;
        }

        return $"{sourceDescription} is version {sourceVersion}, newer than this project's version {destinationVersion}. " +
            "Upgrade this project first: " + UpgradeDocsUrl;
    }
}
