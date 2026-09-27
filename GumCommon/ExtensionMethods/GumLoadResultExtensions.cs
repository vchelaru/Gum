using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Gum.DataTypes;

internal static class GumLoadResultExtensions
{
    /// <summary>
    /// Throws when a runtime project load failed: <paramref name="gumProject"/> is null, or
    /// <paramref name="loadResult"/> has an error message or missing files. Warnings do not throw.
    /// Shared by every runtime's GumService so they fail the same way on a bad project.
    /// </summary>
    internal static void ThrowIfFailed(this GumLoadResult loadResult, [NotNull] GumProjectSave? gumProject)
    {
        if (gumProject != null && string.IsNullOrEmpty(loadResult.ErrorMessage) && loadResult.MissingFiles.Count == 0)
        {
            return;
        }

        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(loadResult.ErrorMessage))
        {
            stringBuilder.AppendLine(loadResult.ErrorMessage);
        }
        foreach (var missingFile in loadResult.MissingFiles)
        {
            stringBuilder.AppendLine($"Missing file: {missingFile}");
        }
        throw new Exception(stringBuilder.ToString());
    }
}
