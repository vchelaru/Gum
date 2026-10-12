namespace Gum.Plugins.ImportPlugin.Services;

/// <summary>
/// The one version check shared by every path that copies Gum files into the open project
/// (Import from .gumx, Add Forms Components).
/// </summary>
public static class ImportVersionGate
{
    /// <summary>
    /// Returns the message to show when content saved at <paramref name="sourceVersion"/> cannot be
    /// imported into a project at <paramref name="destinationVersion"/>, or null when the import is allowed.
    /// </summary>
    public static string? GetRejection(int sourceVersion, int destinationVersion, string sourceDescription)
    {
        if (sourceVersion <= destinationVersion)
        {
            return null;
        }

        return $"{sourceDescription} is version {sourceVersion}, newer than this project's version {destinationVersion}. " +
            "Upgrade this project first: " + UpgradeDocsUrl;
    }

    private const string UpgradeDocsUrl = "https://docs.flatredball.com/gum/gum-tool/upgrading/upgrading-file-gumx-version";
}
