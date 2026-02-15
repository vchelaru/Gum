using Gum.DataTypes;
using Gum.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Managers;

public interface IProjectManager
{
    GumProjectSave? GumProjectSave { get; }
    GeneralSettingsFile GeneralSettingsFile { get; }
    bool HaveErrorsOccurredLoadingProject { get; }

    void LoadSettings();
    Task Initialize();
    void CreateNewProject();
    bool LoadProject();
    void LoadProject(FilePath fileName);
    bool SaveProject(bool forceSaveContainedElements = false);
    string MakeAbsoluteIfNecessary(string textureAsString);
    bool AskUserForProjectNameIfNecessary(out bool isProjectNew);
}
