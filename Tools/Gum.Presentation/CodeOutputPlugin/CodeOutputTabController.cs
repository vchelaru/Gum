using CodeOutputPlugin.Manager;
using CodeOutputPlugin.ViewModels;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices.CodeGeneration;
using Gum.ToolStates;

namespace CodeOutputPlugin;

/// <summary>
/// Owns the Code tab's WPF-free decision logic that reads the WPF <c>Views.CodeWindow</c>/<c>PluginTab</c>
/// as a live input mid-method (a fallback settings read, a visibility/selection gate) rather than only
/// pushing a value at the end - the shape the design pass in issue #3917 called out as blocking a
/// drop-in <c>AnimationTabController</c>-style extraction until <see cref="ICodeOutputTabView"/> and
/// <see cref="ITabSelectionState"/> existed. Extracted from <c>MainCodeOutputPlugin</c> (issue #3917):
/// none of this touches a WPF type directly, it just used to be instance methods reading the plugin's
/// own private <c>control</c>/<c>pluginTab</c> fields.
///
/// <para>
/// <c>codeOutputProjectSettings</c> is not owned here - it is reassigned by the plugin whenever a
/// project loads, and read/written by several plugin methods this extraction deliberately leaves
/// alone - so every method below takes it as a parameter rather than caching it as a field.
/// </para>
/// </summary>
public class CodeOutputTabController
{
    private readonly ICodeOutputTabView _view;
    private readonly ITabVisibility _tabVisibility;
    private readonly ITabSelectionState _tabSelectionState;
    private readonly ISelectedState _selectedState;
    private readonly IProjectState _projectState;
    private readonly CodeGenerator _codeGenerator;
    private readonly CodeGenerationService _codeGenerationService;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly CodeOutputProjectSettingsManager _codeOutputProjectSettingsManager;
    private readonly CodeWindowViewModel _viewModel;

    public CodeOutputTabController(
        ICodeOutputTabView view,
        ITabVisibility tabVisibility,
        ITabSelectionState tabSelectionState,
        ISelectedState selectedState,
        IProjectState projectState,
        CodeGenerator codeGenerator,
        CodeGenerationService codeGenerationService,
        CodeOutputElementSettingsManager elementSettingsManager,
        CodeOutputProjectSettingsManager codeOutputProjectSettingsManager,
        CodeWindowViewModel viewModel)
    {
        _view = view;
        _tabVisibility = tabVisibility;
        _tabSelectionState = tabSelectionState;
        _selectedState = selectedState;
        _projectState = projectState;
        _codeGenerator = codeGenerator;
        _codeGenerationService = codeGenerationService;
        _elementSettingsManager = elementSettingsManager;
        _codeOutputProjectSettingsManager = codeOutputProjectSettingsManager;
        _viewModel = viewModel;
    }

    /// <summary>
    /// Pushes the on-disk (or default) element settings for <paramref name="element"/> onto the view.
    /// </summary>
    public void LoadCodeSettingsFile(ElementSave? element)
    {
        if (element != null && _projectState.GumProjectSave?.FullFileName != null)
        {
            _view.CodeOutputElementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);
        }
        else
        {
            _view.CodeOutputElementSettings = new CodeOutputElementSettings();
        }
    }

    /// <summary>
    /// Recomputes the tab's visibility/selection-gated code display: shows/hides the tab based on
    /// whether a non-Standard element is selected, early-outs if the tab isn't the active one, then
    /// (re)generates the displayed code for whichever of Selected Element/Selected State the view
    /// model is currently showing.
    /// </summary>
    public void RefreshCodeDisplay(CodeOutputProjectSettings codeOutputProjectSettings)
    {
        var shouldShow = _selectedState.SelectedElement != null &&
            _selectedState.SelectedElement is not StandardElementSave;

        if (shouldShow)
        {
            _tabVisibility.Show();
        }
        else
        {
            _tabVisibility.Hide();
        }

        ///////////////////////early out////////////////////
        if (!_tabSelectionState.IsSelected)
        {
            return;
        }

        // SelectedElement can be null if the user has selected something other than
        // a Screen/Component (e.g. a behavior, a folder, nothing), or if a delete just
        // cleared the selection. Without an owning element, there is nothing to
        // generate code for - the .Element-dereferences below (and inside the
        // generator) would NRE.
        if (_selectedState.SelectedElement == null)
        {
            _viewModel.Code = "// Select a Screen, Component, or Standard to see generated code";
            return;
        }
        /////////////////////end early out/////////////////

        // Defensive: never feed the generator an unsupported combination (e.g. a Raylib project whose
        // .codsj still carries FullyInCode). CoerceToSupportedCombination keeps the display path safe
        // regardless of how the settings reached this state.
        CodeGenerator.CoerceToSupportedCombination(codeOutputProjectSettings);

        _view.CodeOutputProjectSettings = codeOutputProjectSettings;
        _view.CodeOutputElementSettings ??= new CodeOutputElementSettings();

        var instance = _selectedState.SelectedInstance;
        var selectedElement = _selectedState.SelectedElement;

        _viewModel.IsViewingStandardElement = selectedElement is StandardElementSave;

        var settings = _view.CodeOutputElementSettings;

        if (settings.GenerationBehavior != GenerationBehavior.NeverGenerate)
        {
            ObjectFinder.Self.EnableCache();
            try
            {
                switch (_viewModel.WhatToView)
                {
                    case WhatToView.SelectedElement:

                        if (instance != null)
                        {
                            string code = _codeGenerator.GetCodeForInstance(instance, selectedElement, codeOutputProjectSettings);
                            _viewModel.Code = code;
                        }
                        else if (selectedElement is not StandardElementSave)
                        {
                            string gumCode = _codeGenerator.GetGeneratedCodeForElement(selectedElement, settings, codeOutputProjectSettings);
                            _viewModel.Code = $"//Code for {selectedElement}\r\n{gumCode}";
                        }
                        break;
                    case WhatToView.SelectedState:
                        var state = _selectedState.SelectedStateSave;

                        if (state != null)
                        {
                            string gumCode = _codeGenerator.GetCodeForState(selectedElement, state, codeOutputProjectSettings);
                            _viewModel.Code = $"//State Code for {state.Name ?? "Default"}:\r\n{gumCode}";
                        }
                        break;
                }
            }
            finally
            {
                ObjectFinder.Self.DisableCache();
            }
        }
        else
        {
            _viewModel.Code = "// code generation disabled for this object";
        }
    }

    /// <summary>
    /// Refreshes the display for the currently-selected element, then auto-regenerates its code file
    /// if the view's current element settings have <see cref="CodeOutputElementSettings.AutoGenerateOnChange"/> set.
    /// </summary>
    public void HandleRefreshAndExport(CodeOutputProjectSettings codeOutputProjectSettings)
    {
        RefreshCodeDisplay(codeOutputProjectSettings);

        _view.CodeOutputElementSettings ??= new CodeOutputElementSettings();

        var elementSettings = _view.CodeOutputElementSettings;

        if (elementSettings.AutoGenerateOnChange)
        {
            GenerateCodeForElement(showPopups: false, _selectedState.SelectedElement, codeOutputProjectSettings);
        }
    }

    /// <summary>
    /// Refresh + auto-regenerate for an explicit owning element rather than the live
    /// SelectedElement. Used by instance add/delete events because the affected instance
    /// may live in an element that is not currently selected (e.g. delete fired from a
    /// tree-view selection that doesn't match what the codegen tab is viewing), in
    /// which case regenerating for SelectedElement writes the wrong file or NREs.
    /// </summary>
    public void HandleRefreshAndExportForElement(ElementSave element, CodeOutputProjectSettings codeOutputProjectSettings)
    {
        RefreshCodeDisplay(codeOutputProjectSettings);

        var elementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);

        if (elementSettings.AutoGenerateOnChange)
        {
            GenerateCodeForElement(showPopups: false, element, codeOutputProjectSettings, elementSettings);
        }
    }

    /// <summary>
    /// Persists the view's current element settings (if any) to disk, refreshes the display, and
    /// always persists the project-wide settings.
    /// </summary>
    public void HandleCodeOutputPropertyChanged(CodeOutputProjectSettings codeOutputProjectSettings)
    {
        // Switching OutputLibrary to Raylib while ObjectInstantiationType is still FullyInCode is an
        // unsupported combination that would throw during generation. Normalize before saving/regenerating
        // so the user can switch libraries in any order without ever hitting that crash.
        CodeGenerator.CoerceToSupportedCombination(codeOutputProjectSettings);

        var element = _selectedState.SelectedElement;
        if (element != null && _view.CodeOutputElementSettings != null)
        {
            _elementSettingsManager.WriteSettingsForElement(element, _view.CodeOutputElementSettings);

            RefreshCodeDisplay(codeOutputProjectSettings);
        }
        _codeOutputProjectSettingsManager.WriteSettingsForProject(codeOutputProjectSettings);
    }

    /// <summary>
    /// Generates the code file for <paramref name="element"/>, using <paramref name="settings"/> if
    /// provided, otherwise falling back to the view's current element settings. No-ops for a null or
    /// Standard element, or when no settings are available from either source.
    /// </summary>
    public void GenerateCodeForElement(bool showPopups, ElementSave? element, CodeOutputProjectSettings codeOutputProjectSettings, CodeOutputElementSettings? settings = null)
    {
        if (element != null && element is not StandardElementSave)
        {
            settings ??= _view.CodeOutputElementSettings;

            // If user is using automatic generation, generate everything
            // If it's manual, don't check for missing files

            if (settings != null)
            {
                var checkForMissing = settings.GenerationBehavior == GenerationBehavior.GenerateAutomaticallyOnPropertyChange;
                _codeGenerationService.GenerateCodeForElement(element, settings, codeOutputProjectSettings, showPopups, checkForMissing: checkForMissing);
            }
        }
    }
}
