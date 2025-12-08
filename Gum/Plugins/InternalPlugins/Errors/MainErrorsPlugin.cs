using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Messages;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.Errors.Views;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

namespace Gum.Plugins.Errors;

[Export(typeof(PluginBase))]
public class MainErrorsPlugin : InternalPlugin
{
    #region Fields/Properties

    AllErrorsViewModel viewModel;
    private PluginManager _pluginManager;
    ErrorChecker errorChecker;
    private IMessenger _messenger;
    ErrorDisplay control;
    PluginTab tabPage;
    private ErrorTabHeader _tabPageHeader;
    private ISelectedState _selectedState;

    #endregion

    public override void StartUp()
    {
        viewModel = new AllErrorsViewModel();

        TypeManager typeManager = Locator.GetRequiredService<TypeManager>();

        _pluginManager = Locator.GetRequiredService<PluginManager>();

        errorChecker = new ErrorChecker(typeManager, _pluginManager);

        _messenger = Locator.GetRequiredService<IMessenger>();

        _messenger.Register<RequestErrorRefreshMessage>(
            this,
            (_, message) => HandleErrorRefreshRequest(message));

        _selectedState = Locator.GetRequiredService<ISelectedState>();

        CreateViews();

        AssignEvents();
    }

    private void HandleErrorRefreshRequest(RequestErrorRefreshMessage message)
    {
        var element = _selectedState.SelectedElement;

        /////////////////////Early Out/////////////////////
        if(element == null)
        {
            return;
        }
        ///////////////////End Early Out///////////////////

        if (message.RequestingPlugin != null)
        {
            viewModel.Errors.RemoveAll(item => item.OwnerPlugin == message.RequestingPlugin);

            var errors = errorChecker.GetErrorsFor(element, message.RequestingPlugin);

            viewModel.Errors.AddRange(errors);
        }
        else
        {
            UpdateErrorsForElement(element);
        }
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

    private void HandleVariableSet(ElementSave element, InstanceSave? instance, string variableName, object? oldValue)
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

    private void HandleElementSelected(ElementSave? element)
    {
        UpdateErrorsForElement(element);
    }

    private void UpdateErrorsForElement(ElementSave? element)
    {
        var errors = errorChecker.GetErrorsFor(element, ProjectState.Self.GumProjectSave);

        viewModel.Errors.Clear();
        foreach (var item in errors)
        {
            viewModel.Errors.Add(item);
        }
    }
}
