using Gum.ToolStates;
using Gum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Plugins.ImportPlugin.Manager;
using ToolsUtilities;
using Gum.Mvvm;

namespace GumFormsPlugin.ViewModels;

internal class AddFormsViewModel : ViewModel
{
    public bool IsIncludeDemoScreenGum
    {
        get => Get<bool>();
        set => Set(value);
    }

    // for now, we make this false, we can add screens later:
    bool saveScreens = false;

    public void DoIt()
    {
        var sourceDestinations = GetSourceDestinations();
        bool canSaveFiles = GetIfShouldSave(sourceDestinations);

        if (canSaveFiles)
        {
            SaveFilesToDestination(sourceDestinations);

            // now add all components and screens to the project
            AddAllElementsToProject(sourceDestinations);

            // reload standards:
            var fileName = GumState.Self.ProjectState.GumProjectSave.FullFileName;
            bool wasSaved = GumCommands.Self.FileCommands.TryAutoSaveProject();
            if (wasSaved)
            {
                GumCommands.Self.FileCommands.LoadProject(fileName);
            }
            else
            {
                GumCommands.Self.GuiCommands.ShowMessage("You must Save, then close/reopen the project.");
            }
        }
    }


    private void AddAllElementsToProject(Dictionary<string, FilePath> sourceDestinations)
    {
        foreach (var item in sourceDestinations)
        {
            var extension = item.Value.Extension;

            if (extension == "gusx")
            {
                // add screen
                ImportLogic.ImportScreen(item.Value, saveProject: false);
            }
            else if (extension == "gucx")
            {
                // add component
                ImportLogic.ImportComponent(item.Value, saveProject: false);
            }
            else if (extension == "behx")
            {
                // add behavior
                ImportLogic.ImportBehavior(item.Value, saveProject: false);
            }
            // standards are already added
        }

    }

    private void SaveFilesToDestination(Dictionary<string, FilePath> sourceDestinations)
    {
        var assembly = GetType().Assembly;
        foreach (var kvp in sourceDestinations)
        {
            var source = kvp.Key;
            var destination = kvp.Value;

            var extension = destination.Extension;

            // don't save the project, we don't want to overwrite it because that would wipe existing
            // projects which may have screens or other components referenced.

            var isGumx = extension == "gumx";

            var shouldSave = isGumx == false;

            if (shouldSave)
            {
                FileManager.SaveEmbeddedResource(assembly, kvp.Key, kvp.Value.FullPath);
            }
        }
    }


    private Dictionary<string, FilePath> GetSourceDestinations()
    {
        var assembly = GetType().Assembly;

        var resourceNames = assembly.GetManifestResourceNames();

        var destinationFolder = GumState.Self.ProjectState.ProjectDirectory;

        const string resourcePrefix =
            "GumFormsPlugin.Content.FormsGumProject.";

        var resourcesToSave = resourceNames.Where(item =>
            item.StartsWith(resourcePrefix));

        Dictionary<string, FilePath> sourceDestinations = new Dictionary<string, FilePath>();

        foreach (var resourceName in resourcesToSave)
        {
            var extension = FileManager.GetExtension(resourceName);

            if (extension == "gusx" && saveScreens == false)
            {
                var shouldInclude = resourceName.Contains("DemoScreenGum.gusx") && IsIncludeDemoScreenGum;

                if(!shouldInclude)
                {
                    continue;
                }
            }
            if (extension == "gumx")
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
    private static bool GetIfShouldSave(Dictionary<string, FilePath> sourceDestinations)
    {
        var existingFiles = sourceDestinations.Values.Where(item => item.Exists());

        var doStandardsExist = existingFiles.Any(item => item.Extension == "gutx");
        var nonStandardFiles = existingFiles.Where(item => item.Extension != "gutx").ToList();

        var nonStandardWhichBlockCopying = nonStandardFiles.Where(item =>
        {
            // don't block on gumx:
            if (item.Extension == "gumx")
            {
                return false;
            }
            return true;
        }).ToList();


        var shouldSave = false;
        if (nonStandardWhichBlockCopying.Count > 0)
        {
            var message = "Cannot create Gum project because the following file(s) would get overwritten:"
                + "\n\n" + string.Join("\n", nonStandardFiles);
            GumCommands.Self.GuiCommands.ShowMessage(message);
        }
        else if (doStandardsExist)
        {
            var filesWhichWouldGetOverwritten = existingFiles
                .Where(item =>
                {
                    return item.Extension != "gumx";
                })
                .Select(item => item.RelativeTo(GumState.Self.ProjectState.ProjectDirectory))
                .ToList();



            var message = "Creating Forms objects would result in overwriting the following files"
                + "\n\n" + string.Join("\n", filesWhichWouldGetOverwritten) + "\nProceed?";

            shouldSave = GumCommands.Self.GuiCommands.ShowYesNoMessageBox(message) == System.Windows.MessageBoxResult.Yes;
        }
        else
        {
            // I guess the user completely deleted everything?
            shouldSave = true;
        }

        return shouldSave;
    }

}
