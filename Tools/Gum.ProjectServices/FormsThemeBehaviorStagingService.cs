using System;
using System.IO;
using System.Xml.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class FormsThemeBehaviorStagingService : IFormsThemeBehaviorStagingService
{
    /// <inheritdoc/>
    public int Stage(string projectGumxPath, string destinationBehaviorsDirectory)
    {
        ProjectLoadResult result = new ProjectLoader().Load(projectGumxPath);

        if (!result.Success || result.Project == null)
        {
            throw new InvalidOperationException(
                $"Could not load project '{projectGumxPath}': {result.ErrorMessage}");
        }

        Directory.CreateDirectory(destinationBehaviorsDirectory);

        bool useCompact = result.Project.Version >= (int)GumProjectSave.GumxVersions.AttributeVersion;
        int stagedCount = 0;

        foreach (BehaviorSave behavior in result.Project.Behaviors)
        {
            if (behavior.IsSourceFileMissing)
            {
                continue;
            }

            string destPath = Path.Combine(destinationBehaviorsDirectory, $"{behavior.Name}.{BehaviorReference.Extension}");
            behavior.Save(destPath, useCompact);
            stagedCount++;
        }

        DropStagedProjectBehaviorSourcePaths(projectGumxPath, destinationBehaviorsDirectory);

        return stagedCount;
    }

    // The staged theme is a standalone project whose Behaviors folder now holds every behavior, so
    // a BehaviorReference's SourcePath into the repo's shared folder would only dangle there.
    private static void DropStagedProjectBehaviorSourcePaths(string projectGumxPath, string destinationBehaviorsDirectory)
    {
        string? stagedDirectory = Path.GetDirectoryName(Path.GetFullPath(destinationBehaviorsDirectory));
        if (stagedDirectory == null)
        {
            return;
        }

        string stagedProjectPath = Path.Combine(stagedDirectory, Path.GetFileName(projectGumxPath));
        if (!File.Exists(stagedProjectPath)
            || string.Equals(Path.GetFullPath(stagedProjectPath), Path.GetFullPath(projectGumxPath), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        XDocument document = XDocument.Load(stagedProjectPath);
        foreach (XElement reference in document.Descendants("BehaviorReference"))
        {
            reference.Attribute("SourcePath")?.Remove();
        }
        document.Save(stagedProjectPath);
    }
}
