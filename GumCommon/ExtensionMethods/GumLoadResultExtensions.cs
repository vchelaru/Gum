using Gum.Wireframe;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Gum.DataTypes;

internal static class GumLoadResultExtensions
{
    /// <summary>
    /// Throws when a runtime project load failed: <paramref name="gumProject"/> is null, or
    /// <paramref name="loadResult"/> has an error message, or it has missing files and
    /// <see cref="GraphicalUiElement.MissingFileBehavior"/> is <see cref="MissingFileBehavior.ThrowException"/>.
    /// Warnings do not throw. Shared by every runtime's GumService so they fail the same way on a bad project.
    /// </summary>
    internal static void ThrowIfFailed(this GumLoadResult loadResult, [NotNull] GumProjectSave? gumProject)
    {
        bool throwForMissingFiles = loadResult.MissingFiles.Count > 0 &&
            GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException;
        if (gumProject != null && string.IsNullOrEmpty(loadResult.ErrorMessage) && !throwForMissingFiles)
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
