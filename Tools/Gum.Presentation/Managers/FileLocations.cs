using ToolsUtilities;
using Gum.ToolStates;

namespace Gum.Managers;

public class FileLocations : IFileLocations
{
    public string ScreensFolder => ProjectFolder + "Screens/";

    public string ComponentsFolder => ProjectFolder + "Components/";

    public string StandardsFolder => ProjectFolder + "Standards/";

    public string BehaviorsFolder => ProjectFolder + "Behaviors/";

    /// <summary>The folder of the saved project; throws if no project is loaded or it was never saved.</summary>
    public virtual string ProjectFolder  => FileManager.GetDirectory(
        (ObjectFinder.Self.GumProjectSave ?? throw new System.InvalidOperationException("No Gum project is loaded.")).GetSavedFileName());
}
