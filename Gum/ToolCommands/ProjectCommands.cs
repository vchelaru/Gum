using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Commands;
using Gum.Services;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;

namespace Gum.ToolCommands;

public class ProjectCommands
{
    #region Fields

    private readonly ISelectedState _selectedState;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IProjectManager _projectManager;
    private readonly IProjectState _projectState;
    private readonly StandardElementsManagerGumTool _standardElementsManagerGumTool;

    #endregion

    public ProjectCommands(ISelectedState selectedState,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IProjectManager projectManager,
        IProjectState projectState,
        StandardElementsManagerGumTool standardElementsManagerGumTool)
    {
        _selectedState = selectedState;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _projectManager = projectManager;
        _projectState = projectState;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
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
        screenSave.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Screen"));
        _standardElementsManagerGumTool.FixCustomTypeConverters(screenSave);
        _projectManager.GumProjectSave.ScreenReferences.Add(new ElementReference { Name = screenSave.Name, ElementType = ElementType.Screen });
        _projectManager.GumProjectSave.ScreenReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
        _projectManager.GumProjectSave.Screens.Add(screenSave);
        _projectManager.GumProjectSave.Screens.Sort((first, second) => first.Name.CompareTo(second.Name));


        _fileCommands.TryAutoSaveProject();
        _fileCommands.TryAutoSaveElement(screenSave);

        Plugins.PluginManager.Self.ElementAdd(screenSave);
    }

    #endregion

    #region Element (Screen/Component/Standard)

    #endregion

    #region Behaviors

    #endregion

    #region Component


    public void AddComponent(ComponentSave componentSave)
    {
        var gumProject = _projectState.GumProjectSave;
        gumProject.ComponentReferences.Add(new ElementReference { Name = componentSave.Name, ElementType = ElementType.Component });
        gumProject.ComponentReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
        gumProject.Components.Add(componentSave);
        gumProject.Components.Sort((first, second) => first.Name.CompareTo(second.Name));


        _fileCommands.TryAutoSaveProject();
        _fileCommands.TryAutoSaveElement(componentSave);
        Plugins.PluginManager.Self.ElementAdd(componentSave);

        _selectedState.SelectedComponent = componentSave;
    }

    public void PrepareNewComponentSave(ComponentSave componentSave, string componentName)
    {
        componentSave.Name = componentName;

        componentSave.BaseType = "Container";

        componentSave.InitializeDefaultAndComponentVariables();
        _standardElementsManagerGumTool.FixCustomTypeConverters(componentSave);


        // components shouldn't set their positions to 0 by default, so if the
        // default state sets those values, we should null them out:
        var xVariable = componentSave.DefaultState.GetVariableSave("X");
        var yVariable = componentSave.DefaultState.GetVariableSave("Y");

        if (xVariable != null)
        {
            xVariable.Value = null;
            xVariable.SetsValue = false;
        }
        if (yVariable != null)
        {
            yVariable.Value = null;
            yVariable.SetsValue = false;
        }

        var hasEventsVariable = componentSave.DefaultState.GetVariableSave("HasEvents");
        if (hasEventsVariable != null)
        {
            hasEventsVariable.Value = true;
        }
    }


    #endregion
}
