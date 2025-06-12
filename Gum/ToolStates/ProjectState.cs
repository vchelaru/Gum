using Gum.DataTypes;
using Gum.Managers;
using Gum.Settings;
using ToolsUtilities;

namespace Gum.ToolStates;

public class ProjectState
{
    static ProjectState mSelf = new ProjectState();

    public static ProjectState Self
    {
        get
        {
            return mSelf;
        }
    }

    public GumProjectSave GumProjectSave => ProjectManager.Self.GumProjectSave;
    public GeneralSettingsFile GeneralSettings => ProjectManager.Self.GeneralSettingsFile;

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
        ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName);
}
