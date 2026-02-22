using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Services;

using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using GumRuntime;
using HarfBuzzSharp;
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.CSharp;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

[Export(typeof(PluginBase))]
public class MainVariableGridPlugin : InternalPlugin
{
    PropertyGridManager _propertyGridManager;
    private readonly VariableReferenceLogic _variableReferenceLogic;
    private readonly ISelectedState _selectedState;

    public MainVariableGridPlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _propertyGridManager = PropertyGridManager.Self;
        _variableReferenceLogic = Locator.GetRequiredService<VariableReferenceLogic>();
        ElementSaveExtensions.CustomEvaluateExpression = EvaluateExpression;
    }

    public override void StartUp()
    {
        AssignEvents();
    }

    private void AssignEvents()
    {
        this.TreeNodeSelected += HandleTreeNodeSelected;
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.ReactToStateSaveCategorySelected += MainVariableGridPlugin_ReactToStateSaveCategorySelected;
        this.ReactToCustomStateSaveSelected += HandleCustomStateSelected;
        this.StateMovedToCategory += HandleStateMovedToCategory;
        this.InstanceSelected += HandleInstanceSelected;
        this.ElementSelected += HandleElementSelected;
        this.ElementDelete += HandleElementDeleted;
        this.ElementRename += HandleElementRenamed;
        this.BehaviorSelected += HandleBehaviorSelected;
        this.VariableSelected += HandleVariableSelected;
        this.RefreshVariableView += HandleRefreshVariableView;
        this.AfterUndo += HandleAfterUndo;
        this.VariableSet += HandleVariableSet;
    }


    private object EvaluateExpression(StateSave stateSave, string expression, string desiredType)
    {
        //    var syntax =_variableReferenceLogic.GetAssignmentSyntax(expression);

        expression = EvaluatedSyntax.ConvertToCSharpSyntax(expression);

        var syntax = CSharpSyntaxTree.ParseText(expression).GetCompilationUnitRoot();

        if(syntax != null)
        {
            var evaluatedSyntax = EvaluatedSyntax.FromSyntaxNode(syntax, stateSave);

            if(evaluatedSyntax?.CastTo(desiredType) == true)
            {
                return evaluatedSyntax?.Value;
            }
        }
        return null;
    }
    private void HandleElementRenamed(ElementSave save, string arg2)
    {
        _propertyGridManager.RefreshVariablesDataGridValues();
    }

    private void HandleVariableSet(ElementSave element, InstanceSave? instance, string strippedName, object? oldValue)
    {
        _propertyGridManager.HandleVariableSet(element, instance, strippedName, oldValue);
    }

    private void HandleElementDeleted(ElementSave save)
    {
        _propertyGridManager.RefreshEntireGrid(force:false);
    }

    private void HandleAfterUndo()
    {
        // An undo can result in variables added or removed, so let's
        // do a full refresh
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleVariableSelected(IStateContainer container, VariableSave save)
    {

    }

    private void HandleElementSelected(ElementSave? save)
    {
        // when an element is selected, so is a state. States
        // also refresh the grid so we don't need to also refresh
        // here.
        //_propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleBehaviorSelected(BehaviorSave? save)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleInstanceSelected(ElementSave save1, InstanceSave save2)
    {
        _propertyGridManager.RefreshEntireGrid(
            // When an instance is selected in a new component, the state and instance are both
            // selected. Don't force it here because if so, it forces a double select on the instance
            // which is raised after the state.
            force: false);
    }

    private void MainVariableGridPlugin_ReactToStateSaveCategorySelected(StateSaveCategory? obj)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);

    }

    private void HandleStateMovedToCategory(StateSave save, StateSaveCategory category1, StateSaveCategory category2)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleStateSelected(StateSave? save)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleCustomStateSelected(StateSave? save)
    {
        // custom states are states where an animation is playing. This slows down
        // the animation considerably so let's not do it:
        //PropertyGridManager.Self.RefreshVariablesDataGridValues();
    }

    private void HandleTreeNodeSelected(TreeNode? node)
    {
        var selectedState = _selectedState;
        var shouldShowButton = (selectedState.SelectedBehavior != null ||
            selectedState.SelectedComponent != null ||
            selectedState.SelectedScreen != null);
        if(shouldShowButton)
        {
            shouldShowButton = _selectedState.SelectedInstance == null;
        }
        _propertyGridManager.VariableViewModel.AddVariableButtonVisibility =
            shouldShowButton.ToVisibility();

        if(selectedState.SelectedBehavior == null && selectedState.SelectedInstance == null && selectedState.SelectedElement == null)
        {
            _propertyGridManager.RefreshEntireGrid(force: true);
        }
    }

    private void HandleRefreshVariableView(bool force)
    {
        _propertyGridManager.RefreshEntireGrid(force);
    }
}
