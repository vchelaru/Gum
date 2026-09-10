using System;
using System.Collections.Generic;
using System.ComponentModel;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;

namespace Gum.Avalonia.Services;

// Placeholders for the property-grid-coupled contracts until phase 70 re-authors the grid for
// this head. Each is the minimum that keeps the headless services working without a grid.

/// <summary>No grid yet, so there is no folder to relate file pickers to.</summary>
public class NullFilePickingFolderProvider : IFilePickingFolderProvider
{
    /// <inheritdoc/>
    public string FolderRelativeTo { set { } }
}

/// <summary>No custom converters until the grid exists; the default converter round-trips strings.</summary>
public class DefaultVariableTypeConverterProvider : IVariableTypeConverterProvider
{
    /// <inheritdoc/>
    public TypeConverter GetTypeConverter(VariableSave defaultVariable, ElementSave? container) => new TypeConverter();
}

/// <summary>No composite (multi-channel) members until the grid exists.</summary>
public class EmptyCompositeMemberRegistry : ICompositeMemberRegistry
{
    /// <inheritdoc/>
    public IReadOnlyList<CompositeMemberDescriptor> Descriptors => Array.Empty<CompositeMemberDescriptor>();
}

/// <summary>The standard-element grid customizations are grid-specific and wait for phase 70.</summary>
public class NullStandardElementsManagerGumTool : IStandardElementsManagerGumTool
{
    /// <inheritdoc/>
    public void Initialize() { }

    /// <inheritdoc/>
    public void FixCustomTypeConverters(GumProjectSave project) { }

    /// <inheritdoc/>
    public void FixCustomTypeConverters(ElementSave elementSave) { }

    /// <inheritdoc/>
    public void RefreshStateVariablesThroughPlugins() { }

    /// <inheritdoc/>
    public void SetPreferredDisplayers(StateSave state) { }
}

/// <summary>No grid to push the selected behavior variable into yet.</summary>
public class NullBehaviorVariablePropertyGridSink : IBehaviorVariablePropertyGridSink
{
    /// <inheritdoc/>
    public VariableSave SelectedBehaviorVariable { set { } }
}
