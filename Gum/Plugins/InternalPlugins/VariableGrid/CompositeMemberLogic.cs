using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Collapses sets of sibling channel variables (declared by <see cref="ICompositeMemberRegistry"/>) into
/// single composite rows in the variable grid; color (Red/Green/Blue -&gt; one swatch) is just the first
/// registered descriptor. Invoked from <c>PropertyGridManager.CustomizeVariables</c> after the categories
/// have been built (and after exclusion has already removed irrelevant channels).
/// </summary>
public class CompositeMemberLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IExposeVariableService _exposeVariableService;
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly ObjectFinder _objectFinder;
    private readonly ICompositeMemberRegistry _registry;

    public CompositeMemberLogic(
        ISelectedState selectedState,
        IExposeVariableService exposeVariableService,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        ObjectFinder objectFinder,
        ICompositeMemberRegistry registry)
    {
        _selectedState = selectedState;
        _exposeVariableService = exposeVariableService;
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _objectFinder = objectFinder;
        _registry = registry;
    }

    /// <summary>
    /// Applies every registered descriptor to every category, collapsing complete, unexposed channel
    /// triples into composite rows.
    /// </summary>
    public void Apply(List<MemberCategory> categories, ElementSave element, InstanceSave? instance)
    {
        foreach (CompositeMemberDescriptor descriptor in _registry.Descriptors)
        {
            foreach (MemberCategory category in categories)
            {
                if (category != null)
                {
                    ApplyDescriptorToCategory(descriptor, category, element, instance);
                }
            }
        }
    }

    private void ApplyDescriptorToCategory(CompositeMemberDescriptor descriptor, MemberCategory category,
        ElementSave element, InstanceSave? instance)
    {
        Dictionary<string, InstanceMember> membersByRootName = new();
        foreach (InstanceMember member in category.Members)
        {
            string? rootName = GetRootVariableName(member, element, instance);
            if (rootName != null && !membersByRootName.ContainsKey(rootName))
            {
                membersByRootName[rootName] = member;
            }
        }

        List<CompositeTriple> triples = GroupTriples(descriptor, membersByRootName);

        foreach (CompositeTriple triple in triples)
        {
            // Collapse condition is exposure-only: if any channel is individually exposed, leave the raw
            // channel rows so the exposure stays visible/manageable.
            bool anyExposed = triple.ChannelRootNames.Any((rootName) =>
                IsChannelExposed(rootName, element, instance));
            if (!anyExposed)
            {
                BuildAndInsertComposite(descriptor, category, triple, instance);
            }
        }
    }

    /// <summary>
    /// Pure grouping of channel members into complete triples. Each root name containing the first channel
    /// token (e.g. "Red") defines a candidate triple keyed by the affix surrounding the token (prefix
    /// "Stroke" from "StrokeRed", suffix "1" from "Red1"); the remaining channels must share that exact
    /// affix. Incomplete sets (a missing channel) produce no triple. Exposed in internal scope for testing.
    /// </summary>
    internal List<CompositeTriple> GroupTriples(CompositeMemberDescriptor descriptor,
        IReadOnlyDictionary<string, InstanceMember> membersByRootName)
    {
        string firstToken = descriptor.ChannelRootNames[0];

        List<CompositeTriple> triples = new();
        foreach (KeyValuePair<string, InstanceMember> kvp in membersByRootName)
        {
            if (!TrySplitAroundToken(kvp.Key, firstToken, out string prefix, out string suffix))
            {
                continue;
            }

            List<InstanceMember> channelMembers = new();
            List<string> channelRootNames = new();
            bool allChannelsPresent = true;
            foreach (string token in descriptor.ChannelRootNames)
            {
                string siblingRootName = prefix + token + suffix;
                if (membersByRootName.TryGetValue(siblingRootName, out InstanceMember? siblingMember))
                {
                    channelMembers.Add(siblingMember);
                    channelRootNames.Add(siblingRootName);
                }
                else
                {
                    allChannelsPresent = false;
                    break;
                }
            }

            if (allChannelsPresent)
            {
                triples.Add(new CompositeTriple(prefix, suffix, channelMembers, channelRootNames));
            }
        }

        return triples;
    }

    private void BuildAndInsertComposite(CompositeMemberDescriptor descriptor, MemberCategory category,
        CompositeTriple triple, InstanceSave? instance)
    {
        string compositeName = descriptor.CompositeNameFormat
            .Replace("{prefix}", triple.Prefix)
            .Replace("{suffix}", triple.Suffix);

        CompositeInstanceMember composite = new(
            compositeName,
            triple.ChannelMembers,
            descriptor.CompositeType,
            descriptor.Compose,
            descriptor.Decompose);

        composite.PreferredDisplayer = descriptor.Displayer;
        composite.SupportsMakeDefault = false;
        composite.DisplayName = ToolsUtilities.StringFunctions.InsertSpacesInCamelCaseString(compositeName);

        // Single undo for the whole composite write. The undo lock must live here (Gum-side) because the
        // WpfDataUi substrate has no access to the undo manager.
        IDisposable? undoLock = null;
        composite.BeforeComposite += (args) =>
        {
            if (args.CommitType == SetPropertyCommitType.Full)
            {
                undoLock = _undoManager.RequestLock();
            }
        };
        composite.AfterComposite += (args) =>
        {
            if (undoLock != null)
            {
                undoLock.Dispose();
                undoLock = null;
            }
            if (args.CommitType == SetPropertyCommitType.Full)
            {
                _guiCommands.RefreshVariables();
            }
        };

        composite.ContextMenuEvents.Add("Make Default", (_, _) => HandleMakeDefault(triple.ChannelMembers));

        TryAddExposeMenu(composite, descriptor, triple, instance);

        int insertIndex = triple.ChannelMembers.Min((member) => category.Members.IndexOf(member));
        foreach (InstanceMember channelMember in triple.ChannelMembers)
        {
            category.Members.Remove(channelMember);
        }
        insertIndex = Math.Min(insertIndex, category.Members.Count);
        category.Members.Insert(insertIndex, composite);
    }

    private void HandleMakeDefault(IReadOnlyList<InstanceMember> channelMembers)
    {
        // Route through each channel's IsDefault so the standard reset path runs (selected state,
        // categories, variable references). One undo lock coalesces the per-channel resets.
        using IDisposable undoLock = _undoManager.RequestLock();
        foreach (InstanceMember channelMember in channelMembers)
        {
            channelMember.IsDefault = true;
        }
    }

    private void TryAddExposeMenu(CompositeInstanceMember composite, CompositeMemberDescriptor descriptor,
        CompositeTriple triple, InstanceSave? instance)
    {
        if (instance == null)
        {
            return;
        }

        string channelList = string.Join(", ", descriptor.ChannelRootNames);
        string compositeBaseName = descriptor.CompositeNameFormat
            .Replace("{prefix}", triple.Prefix)
            .Replace("{suffix}", triple.Suffix);
        List<string> channelRootNames = triple.ChannelRootNames.ToList();

        composite.ContextMenuEvents.Add($"Expose {compositeBaseName} ({channelList})",
            (_, _) => HandleExpose(channelRootNames, instance));
    }

    private void HandleExpose(IReadOnlyList<string> channelRootNames, InstanceSave instance)
    {
        ElementSave? element = _selectedState.SelectedElement;

        ////////////////////////Early Out////////////////////////
        if (element == null)
        {
            return;
        }
        //////////////////////End Early Out/////////////////////

        using IDisposable undoLock = _undoManager.RequestLock();

        List<VariableSave> toRevert = new();
        bool shouldRevert = false;

        foreach (string rootName in channelRootNames)
        {
            if (shouldRevert)
            {
                break;
            }

            OptionallyAttemptedGeneralResponse<VariableSave> response =
                _exposeVariableService.HandleExposeVariableClick(instance, rootName);

            if (response.DidAttempt && response.Succeeded == false)
            {
                shouldRevert = true;
            }
            if (response.Data != null)
            {
                toRevert.Add(response.Data);
            }
        }

        if (shouldRevert)
        {
            foreach (VariableSave variableToRevert in toRevert)
            {
                _exposeVariableService.HandleUnexposeVariableClick(variableToRevert, element);
            }
        }
    }

    private bool IsChannelExposed(string channelRootName, ElementSave element, InstanceSave? instance)
    {
        VariableSave? variable;
        if (instance != null)
        {
            variable = element.DefaultState.GetVariableSave($"{instance.Name}.{channelRootName}");
        }
        else
        {
            variable = element.DefaultState.GetVariableSave(channelRootName);
        }

        return !string.IsNullOrEmpty(variable?.ExposedAsName);
    }

    private string? GetRootVariableName(InstanceMember member, ElementSave element, InstanceSave? instance)
    {
        VariableSave? rootVariable = instance != null
            ? _objectFinder.GetRootVariable(member.Name, instance)
            : _objectFinder.GetRootVariable(member.Name, element);

        return rootVariable?.Name;
    }

    private static bool TrySplitAroundToken(string rootName, string token, out string prefix, out string suffix)
    {
        int index = rootName.IndexOf(token, StringComparison.Ordinal);
        if (index < 0)
        {
            prefix = "";
            suffix = "";
            return false;
        }

        prefix = rootName.Substring(0, index);
        suffix = rootName.Substring(index + token.Length);
        return true;
    }

    internal record CompositeTriple(
        string Prefix,
        string Suffix,
        List<InstanceMember> ChannelMembers,
        List<string> ChannelRootNames);
}
