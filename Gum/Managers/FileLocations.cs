using ToolsUtilities;

namespace Gum.Managers;

public class FileLocations
{
    public string ScreensFolder => ProjectFolder + "Screens/";

    public string ComponentsFolder => ProjectFolder + "Components/";

    public string StandardsFolder => ProjectFolder + "Standards/";

    public string BehaviorsFolder => ProjectFolder + "Behaviors/";

    public virtual string ProjectFolder  => FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName);
}
