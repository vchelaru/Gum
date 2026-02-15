using Gum.DataTypes;
using Gum.Settings;
using ToolsUtilities;

namespace Gum.ToolStates;

public interface IProjectState
{
    GumProjectSave GumProjectSave { get; }
    GeneralSettingsFile GeneralSettings { get; }
    string? ProjectDirectory { get; }
    FilePath ComponentFilePath { get; }
    FilePath ScreenFilePath { get; }
    FilePath BehaviorFilePath { get; }
    bool NeedsToSaveProject { get; }
}
