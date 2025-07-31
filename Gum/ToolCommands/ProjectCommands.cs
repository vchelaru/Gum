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

namespace Gum.ToolCommands;

public class ProjectCommands
{
    #region Fields

    static ProjectCommands mSelf;
    private readonly ISelectedState _selectedState;
    private readonly GuiCommands _guiCommands;
    private readonly FileCommands _fileCommands;

    #endregion
    
    public ProjectCommands(ISelectedState selectedState, GuiCommands guiCommands, FileCommands fileCommands)
    {
        _selectedState = selectedState;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
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
        StandardElementsManagerGumTool.Self.FixCustomTypeConverters(screenSave);
        ProjectManager.Self.GumProjectSave.ScreenReferences.Add(new ElementReference { Name = screenSave.Name, ElementType = ElementType.Screen });
        ProjectManager.Self.GumProjectSave.ScreenReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
        ProjectManager.Self.GumProjectSave.Screens.Add(screenSave);
        ProjectManager.Self.GumProjectSave.Screens.Sort((first, second) => first.Name.CompareTo(second.Name));


        _fileCommands.TryAutoSaveProject();
        _fileCommands.TryAutoSaveElement(screenSave);

        Plugins.PluginManager.Self.ElementAdd(screenSave);
    }

    #endregion

    #region Element (Screen/Component/Standard)

    internal void RemoveElement(ElementSave element)
    {
        GumProjectSave gps = ProjectManager.Self.GumProjectSave;
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

        GumProjectSave gps = ProjectManager.Self.GumProjectSave;
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
        var gumProject = ProjectState.Self.GumProjectSave;
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
        StandardElementsManagerGumTool.Self.FixCustomTypeConverters(componentSave);


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
