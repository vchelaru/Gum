using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Commands;
using Gum.Services;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices.Editing;

namespace Gum.ToolCommands;

public class ProjectCommands : ICopyPasteProjectCommands
{
    #region Fields

    private readonly ISelectedState _selectedState;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IProjectManager _projectManager;
    private readonly IProjectState _projectState;
    private readonly IStandardElementsManagerGumTool _standardElementsManagerGumTool;
    private readonly IPluginManager _pluginManager;
    private readonly IElementCreationLogic _elementCreationLogic;

    #endregion

    public ProjectCommands(ISelectedState selectedState,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IProjectManager projectManager,
        IProjectState projectState,
        IStandardElementsManagerGumTool standardElementsManagerGumTool,
        IPluginManager pluginManager,
        IElementCreationLogic elementCreationLogic)
    {
        _selectedState = selectedState;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _projectManager = projectManager;
        _projectState = projectState;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
        _pluginManager = pluginManager;
        _elementCreationLogic = elementCreationLogic;
    }
    
    #region Screens
    /// <summary>
    /// Creates a new Screen using the argument as the name. This saves the newly created screen to disk and saves the project.
    /// </summary>
    /// <param name="screenName"></param>
    /// <returns></returns>
    public ScreenSave AddScreen(string screenName)
    {
        ScreenSave screenSave = new ScreenSave();
        screenSave.Name = screenName;

        AddScreen(screenSave);

        return screenSave;
    }

    public void AddScreen(ScreenSave screenSave)
    {
        _elementCreationLogic.AddScreen(_projectManager.GetLoadedProject(), screenSave);
        _standardElementsManagerGumTool.FixCustomTypeConverters(screenSave);

        _fileCommands.TryAutoSaveProject();
        _fileCommands.TryAutoSaveElement(screenSave);

        _pluginManager.ElementAdd(screenSave);
    }

    #endregion

    #region Component

    public void AddComponent(ComponentSave componentSave)
    {
        _elementCreationLogic.AddComponent(_projectState.GetLoadedProject(), componentSave);

        _fileCommands.TryAutoSaveProject();
        _fileCommands.TryAutoSaveElement(componentSave);
        _pluginManager.ElementAdd(componentSave);

        _selectedState.SelectedComponent = componentSave;
    }

    public void PrepareNewComponentSave(ComponentSave componentSave, string componentName, string baseType = "Container")
    {
        _elementCreationLogic.PrepareNewComponent(componentSave, componentName, baseType);
        _standardElementsManagerGumTool.FixCustomTypeConverters(componentSave);
    }

    #endregion
}
