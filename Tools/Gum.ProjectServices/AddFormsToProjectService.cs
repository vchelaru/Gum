using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class AddFormsToProjectService : IAddFormsToProjectService
{
    private static readonly string[] AdditionalFilesToCopy = { "UISpriteSheet.png" };

    /// <inheritdoc/>
    public AddFormsResult AddFormsTo(string projectFilePath)
    {
        var result = new AddFormsResult();

        if (!File.Exists(projectFilePath))
        {
            result.ErrorMessage = $"Project file not found: {projectFilePath}";
            return result;
        }

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult targetLoadResult = loader.Load(projectFilePath);
        if (!targetLoadResult.Success || targetLoadResult.Project == null)
        {
            result.ErrorMessage = $"Failed to load project: {targetLoadResult.ErrorMessage}";
            return result;
        }

        GumProjectSave target = targetLoadResult.Project;
        string targetDirectory = Path.GetDirectoryName(Path.GetFullPath(projectFilePath)) ?? string.Empty;
        bool isJsonFormat = GumProjectSave.IsJsonFormat(projectFilePath);
        bool useCompact = target.Version >= (int)GumProjectSave.GumxVersions.AttributeVersion;

        string tempDirectory = Path.Combine(Path.GetTempPath(), "GumAddForms_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            // The template is always extracted as XML into a throwaway scratch project, regardless
            // of the target's own format - it's cheap and discarded, and lets every added element
            // be re-saved once, below, directly in the target's actual format.
            string tempProjectPath = Path.Combine(tempDirectory, "GumProject.gumx");
            IFormsTemplateCreator formsCreator = new FormsTemplateCreator();
            formsCreator.Create(tempProjectPath);

            ProjectLoadResult templateLoadResult = loader.Load(tempProjectPath);
            if (!templateLoadResult.Success || templateLoadResult.Project == null)
            {
                throw new InvalidOperationException(
                    $"Failed to load the extracted Forms template: {templateLoadResult.ErrorMessage}");
            }

            GumProjectSave template = templateLoadResult.Project;

            List<ComponentSave> addedComponents = AddMissing(
                template.Components, c => c.Name,
                target.Components, target.ComponentReferences,
                template.ComponentReferences);

            List<StandardElementSave> addedStandards = AddMissing(
                template.StandardElements, s => s.Name,
                target.StandardElements, target.StandardElementReferences,
                template.StandardElementReferences);

            List<BehaviorSave> addedBehaviors = AddMissingBehaviors(template, target);

            result.AddedComponents.AddRange(addedComponents.Select(c => c.Name));
            result.AddedStandards.AddRange(addedStandards.Select(s => s.Name));
            result.AddedBehaviors.AddRange(addedBehaviors.Select(b => b.Name));

            // Save the project shell (updated reference lists) without touching any already-existing
            // element file - only the newly-added elements are written below.
            target.Save(projectFilePath, saveElements: false);

            string componentExtension = isJsonFormat ? GumProjectSave.ComponentJsonExtension : GumProjectSave.ComponentExtension;
            foreach (var componentSave in addedComponents)
            {
                componentSave.Save(
                    Path.Combine(targetDirectory, ElementReference.ComponentSubfolder, componentSave.Name + "." + componentExtension),
                    useCompact);
            }

            string standardExtension = isJsonFormat ? GumProjectSave.StandardJsonExtension : GumProjectSave.StandardExtension;
            foreach (var standardSave in addedStandards)
            {
                standardSave.Save(
                    Path.Combine(targetDirectory, ElementReference.StandardSubfolder, standardSave.Name + "." + standardExtension),
                    useCompact);
            }

            string behaviorExtension = isJsonFormat ? BehaviorReference.JsonExtension : BehaviorReference.Extension;
            foreach (var behaviorSave in addedBehaviors)
            {
                behaviorSave.Save(
                    Path.Combine(targetDirectory, BehaviorReference.Subfolder, behaviorSave.Name + "." + behaviorExtension),
                    useCompact);
            }

            CopyAdditionalFilesIfMissing(tempDirectory, targetDirectory);

            result.Success = true;
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }

        return result;
    }

    // Shared logic for Components and Standards: both are ElementSave lists paired with an
    // ElementReference list keyed by Name. Behaviors are handled separately (BehaviorReference is
    // not an ElementReference and needs its own SourcePath handling - see AddMissingBehaviors).
    private static List<TElement> AddMissing<TElement>(
        List<TElement> templateElements, Func<TElement, string> getName,
        List<TElement> targetElements, List<ElementReference> targetReferences,
        List<ElementReference> templateReferences)
    {
        var added = new List<TElement>();

        foreach (var templateElement in templateElements)
        {
            string name = getName(templateElement);
            bool alreadyPresent = targetReferences.Any(r => r.Name == name);
            if (alreadyPresent)
            {
                continue;
            }

            var templateReference = templateReferences.First(r => r.Name == name);
            targetReferences.Add(new ElementReference
            {
                Name = templateReference.Name,
                ElementType = templateReference.ElementType
            });
            targetElements.Add(templateElement);
            added.Add(templateElement);
        }

        targetReferences.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        targetElements.Sort((a, b) => string.CompareOrdinal(getName(a), getName(b)));

        return added;
    }

    private static List<BehaviorSave> AddMissingBehaviors(GumProjectSave template, GumProjectSave target)
    {
        var added = new List<BehaviorSave>();

        foreach (var behaviorSave in template.Behaviors)
        {
            bool alreadyPresent = target.BehaviorReferences.Any(r => r.Name == behaviorSave.Name);
            if (alreadyPresent)
            {
                continue;
            }

            target.BehaviorReferences.Add(new BehaviorReference { Name = behaviorSave.Name });
            target.Behaviors.Add(behaviorSave);
            added.Add(behaviorSave);
        }

        target.BehaviorReferences.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        target.Behaviors.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        return added;
    }

    // Fonts and UISpriteSheet.png aren't tracked by any reference list - components/standards just
    // look them up by relative path at runtime - so copying them is a plain skip-if-exists file
    // copy rather than anything reference-list driven.
    private static void CopyAdditionalFilesIfMissing(string tempDirectory, string targetDirectory)
    {
        string tempFontsDirectory = Path.Combine(tempDirectory, "Fonts");
        if (Directory.Exists(tempFontsDirectory))
        {
            string targetFontsDirectory = Path.Combine(targetDirectory, "Fonts");
            Directory.CreateDirectory(targetFontsDirectory);
            foreach (string fontFile in Directory.GetFiles(tempFontsDirectory))
            {
                string destination = Path.Combine(targetFontsDirectory, Path.GetFileName(fontFile));
                if (!File.Exists(destination))
                {
                    File.Copy(fontFile, destination);
                }
            }
        }

        foreach (string fileName in AdditionalFilesToCopy)
        {
            string source = Path.Combine(tempDirectory, fileName);
            string destination = Path.Combine(targetDirectory, fileName);
            if (File.Exists(source) && !File.Exists(destination))
            {
                File.Copy(source, destination);
            }
        }
    }
}
