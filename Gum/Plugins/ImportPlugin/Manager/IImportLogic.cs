using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using ToolsUtilities;

namespace Gum.Plugins.ImportPlugin.Manager;

public interface IImportLogic
{
    ScreenSave? ImportScreen(FilePath filePath, string desiredDirectory = null, bool saveProject = true);
    ComponentSave? ImportComponent(FilePath filePath, string desiredDirectory = null, bool saveProject = true);
    BehaviorSave ImportBehavior(FilePath filePath, string desiredDirectory = null, bool saveProject = false);
}
