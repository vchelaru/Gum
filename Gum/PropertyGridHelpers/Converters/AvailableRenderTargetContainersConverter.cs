using Gum.DataTypes;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Gum.PropertyGridHelpers.Converters;

/// <summary>
/// Provides the selectable values for a Sprite's <c>RenderTargetTextureSource</c> variable: the
/// names of sibling instances in the selected element whose effective <c>IsRenderTarget</c> value is
/// <see langword="true"/>, plus a <c>&lt;NONE&gt;</c> entry to clear the source. Scoped to the
/// current element because the runtime resolves the reference by name within the top-parent visual
/// tree, so a cross-element reference could not render.
/// </summary>
public class AvailableRenderTargetContainersConverter : TypeConverter
{
    private readonly ISelectedState _selectedState;

    /// <inheritdoc/>
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
    {
        return true;
    }

    /// <inheritdoc/>
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)
    {
        return true;
    }

    public AvailableRenderTargetContainersConverter(ISelectedState selectedState)
    {
        _selectedState = selectedState ?? throw new ArgumentNullException(nameof(selectedState));
    }

    /// <inheritdoc/>
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
    {
        List<string> values = new();
        values.Add("<NONE>");

        ElementSave? element = _selectedState.SelectedElement;

        if (element != null)
        {
            foreach (InstanceSave instance in element.Instances)
            {
                if (instance == _selectedState.SelectedInstance)
                {
                    // The sprite cannot use itself as a render-target source.
                    continue;
                }

                RecursiveVariableFinder finder = new RecursiveVariableFinder(instance, element);
                if (finder.GetValue("IsRenderTarget") as bool? == true)
                {
                    values.Add(instance.Name);
                }
            }
        }

        return new StandardValuesCollection(values);
    }
}
