using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.DataTypes;
using Gum.ToolStates;
using System;
using Gum.Commands;
using Gum.Plugins.InternalPlugins.Errors.Views;
using Gum.Reflection;
using Gum.Services;

namespace Gum.Plugins.Errors;

[Export(typeof(PluginBase))]
public class MainErrorsPlugin : InternalPlugin
{
    #region Fields/Properties

    AllErrorsViewModel viewModel;
    ErrorChecker errorChecker;
    ErrorDisplay control;
    PluginTab tabPage;
    private ErrorTabHeader _tabPageHeader;

    #endregion

    public override void StartUp()
    {
        viewModel = new AllErrorsViewModel();

        TypeManager typeManager = Locator.GetRequiredService<TypeManager>();

        errorChecker = new ErrorChecker(typeManager);

        CreateViews();

        AssignEvents();
    }

    private void CreateViews()
    {
        control = new ErrorDisplay();
        control.DataContext = viewModel;
        tabPage = _tabManager.AddControl(control, "Errors", TabLocation.RightBottom);

        _tabPageHeader = new ErrorTabHeader { DataContext = viewModel };
        tabPage.CustomHeaderContent = _tabPageHeader;
    }

    private void AssignEvents()
    {
        this.ElementSelected += HandleElementSelected;
        this.InstanceSelected += HandleInstanceSelected;

        this.InstanceAdd += HandleInstanceAdd;
        this.InstanceDelete += HandleInstanceDelete;
        this.VariableSet += HandleVariableSet;
        this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;
    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleBehaviorReferencesChanged(ElementSave element)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleInstanceDelete(ElementSave element, InstanceSave instance)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleInstanceAdd(ElementSave element, InstanceSave instance)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleElementSelected(ElementSave element)
    {
        UpdateErrorsForElement(element);
    }

    private void UpdateErrorsForElement(ElementSave element)
    {
        var errors = errorChecker.GetErrorsFor(element, ProjectState.Self.GumProjectSave);

        viewModel.Errors.Clear();
        foreach (var item in errors)
        {
            viewModel.Errors.Add(item);
        }
    }
}
