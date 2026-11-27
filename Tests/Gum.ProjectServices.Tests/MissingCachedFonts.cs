using Gum.Bundle;
using Gum.DataTypes;
using Gum.Managers;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The fonts a project's own content uses that have no baked file in its FontCache. A project gets no runtime font creator
/// (KernSmith) unless it is added, so each one renders as the runtime's default font.
/// </summary>
internal static class MissingCachedFonts
{
    public static List<string> Find(string gumProjectPath)
    {
        string fontCache = Path.Combine(Path.GetDirectoryName(gumProjectPath)!, "FontCache");
        GumProjectSave project = new ProjectLoader().Load(gumProjectPath).Project!;
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            IEnumerable<ElementSave> elements = project.StandardElements.Cast<ElementSave>().Concat(project.Components).Concat(project.Screens);
            return new FontReferenceCollector(instance => ObjectFinder.Self.GetElementSave(instance))
                .Collect(project, elements)
                .Keys
                .Select(key => Path.GetFileName(key))
                .Where(fileName => !File.Exists(Path.Combine(fontCache, fileName)))
                .ToList();
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
