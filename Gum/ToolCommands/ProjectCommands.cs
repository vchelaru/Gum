using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Managers;
using Gum.DataTypes.Behaviors;
using Gum.ToolStates;
using ToolsUtilities;
using CommonFormsAndControls;
using System.Windows.Forms;
using System.Windows.Controls;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using System.Linq;
using Gum.Commands;
using Gum.Services;
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

    internal void RemoveElement(ElementSave element)
    {
        GumProjectSave gps = _projectManager.GumProjectSave;
        string name = element.Name;
        var removed = false;
        if (element is ScreenSave asScreenSave)
        {
            RemoveElementReferencesFromList(name, gps.ScreenReferences);
            gps.Screens.Remove(asScreenSave);
            removed = true;
        }
        else if (element is ComponentSave asComponentSave)
        {
            RemoveElementReferencesFromList(name, gps.ComponentReferences);
            gps.Components.Remove(asComponentSave);
            removed = true;

        }

        if(removed)
        {
            if(_selectedState.SelectedElements.Contains(element))
            {
                _selectedState.SelectedElement = null;
            }
            Plugins.PluginManager.Self.ElementDelete(element);
            _fileCommands.TryAutoSaveProject();
        }
    }

    private static void RemoveElementReferencesFromList(string name, List<ElementReference> references)
    {
        for (int i = 0; i < references.Count; i++)
        {
            ElementReference reference = references[i];

            if (reference.Name == name)
            {
                references.RemoveAt(i);
                break;
            }
        }
    }

    #endregion

    #region Behaviors

    public void RemoveBehavior(BehaviorSave behavior)
    {
        string behaviorName = behavior.Name;

        GumProjectSave gps = _projectManager.GumProjectSave;
        List<BehaviorReference> references = gps.BehaviorReferences;

        references.RemoveAll(item => item.Name == behavior.Name);

        gps.Behaviors.Remove(behavior);

        List<ElementSave> elementsReferencingBehavior = new List<ElementSave>();

        foreach (var element in ObjectFinder.Self.GumProjectSave.AllElements)
        {
            var matchingBehavior = element.Behaviors.FirstOrDefault(item =>
                item.BehaviorName == behaviorName);

            if (matchingBehavior != null)
            {
                element.Behaviors.Remove(matchingBehavior);
                elementsReferencingBehavior.Add(element);
            }
        }

        _selectedState.SelectedBehavior = null;

        PluginManager.Self.BehaviorDeleted(behavior);

        _guiCommands.RefreshStateTreeView();
        _guiCommands.RefreshVariables();
        // I don't think we have to refresh the wireframe since nothing is being shown
        //Wireframe.WireframeObjectManager.Self.RefreshAll(true);

        _fileCommands.TryAutoSaveProject();

        foreach (var element in elementsReferencingBehavior)
        {
            _fileCommands.TryAutoSaveElement(element);
        }
    }


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
