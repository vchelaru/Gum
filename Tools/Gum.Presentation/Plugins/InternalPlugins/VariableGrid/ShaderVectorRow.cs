using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Builds the one Variables tab row of a float vector shader parameter (<c>Tint</c>): its X, Y, Z and W fields sit side by
/// side, and the row is set, made default and exposed as a whole. Each field is still the ordinary row of one
/// saved component variable, so editing one saves every component of the parameter as one undo step.
/// </summary>
public class ShaderVectorRow
{
    private static readonly string[] ExposureSuffixes = { "__X", "__Y", "__Z", "__W" };

    private readonly IUndoManager _undoManager;
    private readonly ISelectedState _selectedState;
    private readonly IExposeVariableService _exposeVariableService;
    private readonly IDialogService _dialogService;
    private readonly INameVerifier _nameVerifier;

    /// <summary>Creates the builder over the services the row menu commands need.</summary>
    public ShaderVectorRow(IUndoManager undoManager, ISelectedState selectedState, IExposeVariableService exposeVariableService,
        IDialogService dialogService, INameVerifier nameVerifier)
    {
        _undoManager = undoManager;
        _selectedState = selectedState;
        _exposeVariableService = exposeVariableService;
        _dialogService = dialogService;
        _nameVerifier = nameVerifier;
    }

    /// <summary>The suffix a component exposed name ends with (<c>__X</c> to <c>__W</c>); code generation groups on it.</summary>
    public static string GetExposureSuffix(int componentIndex) => ExposureSuffixes[componentIndex];

    /// <summary>Creates the row over <paramref name="components"/>, one per component in X, Y, Z, W order.</summary>
    /// <param name="parameterName">The shader parameter name, shown as the row label.</param>
    /// <param name="components">The component rows; a reset of one clears the whole parameter.</param>
    /// <param name="bindings">The binding of each component, in the same order.</param>
    /// <param name="rootNames">The instance relative variable name of each component (<c>Outline_Tint__X</c>).</param>
    /// <param name="instance">The instance that owns the shader.</param>
    public CompositeInstanceMember Create(string parameterName, IReadOnlyList<InstanceMember> components,
        IReadOnlyList<ShaderParameterVariableBinding> bindings, IReadOnlyList<string> rootNames, InstanceSave instance)
    {
        CompositeInstanceMember row = new CompositeInstanceMember(
            parameterName,
            components,
            typeof(float[]),
            values => values.Select(value => value is float component ? component : 0f).ToArray(),
            value => ((float[])value).Select(component => (object?)component).ToArray())
        {
            PreferredDisplayer = typeof(GumDisplayers.ShaderVector),
            DisplayName = parameterName,
            // The reset below clears the whole parameter, so it is the row own command rather than the per-member one.
            SupportsMakeDefault = false,
        };

        row.ContextMenuEvents.Add("Make Default", (_, _) => MakeDefault(components));

        ElementSave? element = _selectedState.SelectedElement;
        List<VariableSave> exposed = new List<VariableSave>();
        if (element != null)
        {
            foreach (string name in rootNames)
            {
                VariableSave? variable = element.GetDefaultStateOrThrow().GetVariableSave(instance.Name + "." + name);
                if (variable != null && !string.IsNullOrEmpty(variable.ExposedAsName))
                {
                    exposed.Add(variable);
                }
            }
        }

        if (exposed.Count > 0)
        {
            string exposedBase = exposed[0].ExposedAsName!;
            foreach (string suffix in ExposureSuffixes)
            {
                if (exposedBase.EndsWith(suffix, StringComparison.Ordinal))
                {
                    exposedBase = exposedBase.Substring(0, exposedBase.Length - suffix.Length);
                }
            }
            row.ContextMenuEvents.Add($"Un-expose {exposedBase}", (_, _) => Unexpose(exposed));
        }
        else if (element != null)
        {
            row.ContextMenuEvents.Add("Expose Variable", (_, _) => Expose(parameterName, bindings, rootNames, instance));
        }
        return row;
    }

    private void MakeDefault(IReadOnlyList<InstanceMember> components)
    {
        // A component Make Default clears every component of the parameter (and what the legacy text holds), so one
        // that is not saved yet is skipped: a definition that saves only some components still resets.
        InstanceMember? saved = components.FirstOrDefault(component => !component.IsDefault);
        if (saved == null)
        {
            return;
        }

        using IDisposable undoLock = _undoManager.RequestLock();
        saved.IsDefault = true;
    }

    private void Unexpose(IReadOnlyList<VariableSave> exposed)
    {
        ElementSave? element = _selectedState.SelectedElement;
        if (element == null)
        {
            return;
        }

        using IDisposable undoLock = _undoManager.RequestLock();
        foreach (VariableSave variable in exposed)
        {
            _exposeVariableService.HandleUnexposeVariableClick(variable, element);
        }
    }

    // One prompt for a base name; each component is exposed as <base>__X to <base>__W, so older tools and runtimes see N
    // ordinary exposed floats and code generation can group them. The components carry their values, as exposing the
    // parameter pins the whole vector.
    private void Expose(string parameterName, IReadOnlyList<ShaderParameterVariableBinding> bindings, IReadOnlyList<string> rootNames, InstanceSave instance)
    {
        ElementSave? element = _selectedState.SelectedElement;
        if (element == null)
        {
            return;
        }

        ExposeColorDialogViewModel dialogViewModel = new ExposeColorDialogViewModel(
            instance.Name + parameterName,
            ExposureSuffixes.Take(rootNames.Count).ToList(),
            exposedName => _nameVerifier.IsVariableNameValid(exposedName, element, null, out string? whyNot) ? null : whyNot);
        if (!_dialogService.Show(dialogViewModel))
        {
            return;
        }

        StateSave defaultState = element.GetDefaultStateOrThrow();
        using IDisposable undoLock = _undoManager.RequestLock();
        HashSet<VariableSave> existingBefore = defaultState.Variables.ToHashSet();
        bindings[0].PinComponents(defaultState, instance, includeThisComponent: true);
        List<VariableSave> added = defaultState.Variables.Where(variable => !existingBefore.Contains(variable)).ToList();

        List<VariableSave> exposedNow = new List<VariableSave>();
        bool failed = false;
        for (int i = 0; i < rootNames.Count && !failed; i++)
        {
            OptionallyAttemptedGeneralResponse<VariableSave> response =
                _exposeVariableService.ExposeVariable(instance, rootNames[i], dialogViewModel.ExposedNames[i]);
            failed = response.DidAttempt && !response.Succeeded;
            if (response.Data != null && response.Succeeded)
            {
                exposedNow.Add(response.Data);
            }
        }

        if (failed)
        {
            foreach (VariableSave variable in exposedNow)
            {
                variable.ExposedAsName = null;
            }
            foreach (VariableSave variable in added)
            {
                defaultState.Variables.Remove(variable);
            }
            return;
        }

        // The value the legacy text held belongs to the variables now that something names the parameter.
        bindings[0].RemoveLegacyValue(defaultState);
    }
}
