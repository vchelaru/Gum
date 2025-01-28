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

    public MainVariableGridPlugin()
    {
        _propertyGridManager = PropertyGridManager.Self;
        _variableReferenceLogic = new VariableReferenceLogic(
            Builder.Get<GuiCommands>());
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


    private object EvaluateExpression(StateSave stateSave, string expression)
    {
        //    var syntax =_variableReferenceLogic.GetAssignmentSyntax(expression);

        expression = EvaluatedSyntax.ConvertToCSharpSyntax(expression);

        var syntax = CSharpSyntaxTree.ParseText(expression).GetCompilationUnitRoot();

        if(syntax != null)
        {
            var evaluatedSyntax = EvaluatedSyntax.FromSyntaxNode(syntax, stateSave);
            return evaluatedSyntax?.Value;
        }
        return null;
    }
    private void HandleElementRenamed(ElementSave save, string arg2)
    {
        PropertyGridManager.Self.RefreshVariablesDataGridValues();
    }

    private void HandleVariableSet(ElementSave element, InstanceSave instance, string strippedName, object oldValue)
    {
        if(strippedName == "VariableReferences")
        {
            // force refresh:
            _propertyGridManager.RefreshEntireGrid(force: true);
        }
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

    private void HandleElementSelected(ElementSave save)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleBehaviorSelected(BehaviorSave save)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleInstanceSelected(ElementSave save1, InstanceSave save2)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void MainVariableGridPlugin_ReactToStateSaveCategorySelected(StateSaveCategory obj)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);

    }

    private void HandleStateMovedToCategory(StateSave save, StateSaveCategory category1, StateSaveCategory category2)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleStateSelected(StateSave save)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleCustomStateSelected(StateSave save)
    {
        PropertyGridManager.Self.RefreshVariablesDataGridValues();
    }

    private void HandleTreeNodeSelected(TreeNode node)
    {
        var selectedState = GumState.Self.SelectedState;
        var shouldShowButton = (selectedState.SelectedBehavior != null ||
            selectedState.SelectedComponent != null ||
            selectedState.SelectedScreen != null);
        if(shouldShowButton)
        {
            shouldShowButton = GumState.Self.SelectedState.SelectedInstance == null;
        }
        PropertyGridManager.Self.VariableViewModel.AddVariableButtonVisibility =
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
