using System;
using System.IO;
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

        return stagedCount;
    }
}
