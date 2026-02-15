using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.Settings;
using ToolsUtilities;

namespace Gum.ToolStates;

public class ProjectState
{
    private IProjectManager _projectManager;

    public ProjectState(IProjectManager projectManager)
    {
        _projectManager = projectManager;
    }

    public GumProjectSave GumProjectSave => _projectManager.GumProjectSave;
    public GeneralSettingsFile GeneralSettings => _projectManager.GeneralSettingsFile;

    public string ProjectDirectory
    {
        get
        {
            if(string.IsNullOrEmpty(GumProjectSave?.FullFileName))
            {
                return null;
            }
            else
            {
                return ToolsUtilities.FileManager.GetDirectory(GumProjectSave.FullFileName);
            }
        }
    }

    public FilePath ComponentFilePath => ProjectDirectory + "Components/";
    public FilePath ScreenFilePath => ProjectDirectory + "Screens/";
    public FilePath BehaviorFilePath => ProjectDirectory + "Behaviors/";

    public bool NeedsToSaveProject =>
        ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(_projectManager.GumProjectSave.FullFileName);
}
