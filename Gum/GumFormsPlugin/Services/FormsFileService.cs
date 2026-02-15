using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumFormsPlugin.Services;
public class FormsFileService
{
    // for now, we make this false, we can add screens later:
    //bool saveScreens = false;

    public Dictionary<string, FilePath> GetSourceDestinations(bool isIncludeDemoScreenGum)
    {
        var projectState = Locator.GetRequiredService<ProjectState>();
        var destinationFolder = projectState.ProjectDirectory;

        Dictionary<string, FilePath> sourceDestinations = new Dictionary<string, FilePath>();

        //////////////////////Early Out////////////////////////////////////
        if (string.IsNullOrEmpty(destinationFolder)) return sourceDestinations;
        ////////////////////End Early Out//////////////////////////////////

        var assembly = GetType().Assembly;

        var resourceNames = assembly.GetManifestResourceNames();

        const string resourcePrefix =
            "GumFormsPlugin.Content.FormsGumProject.";

        var resourcesToSave = resourceNames.Where(item =>
            item.StartsWith(resourcePrefix));

        foreach (var resourceName in resourcesToSave)
        {
            var extension = FileManager.GetExtension(resourceName);

            if (extension == "gusx")
            {
                var shouldInclude = resourceName.Contains("DemoScreenGum.gusx") && isIncludeDemoScreenGum;

                if (!shouldInclude)
                {
                    continue;
                }
            }
            if (extension == "gumx" || extension == "gumfcs")
            {
                continue;
            }

            var stripped = resourceName.Substring(resourcePrefix.Length);

            var name = FileManager.RemoveExtension(stripped).Replace(".", "/")
                + "." + extension;

            var absoluteDestination = destinationFolder + name;

            sourceDestinations.Add(resourceName, absoluteDestination);
        }

        return sourceDestinations;
    }

}
