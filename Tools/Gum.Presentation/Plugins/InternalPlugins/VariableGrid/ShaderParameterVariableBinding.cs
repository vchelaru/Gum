using Gum.DataTypes;
using Gum.DataTypes.Variables;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// What a Variables tab row needs to know when it is backed by a real saved shader parameter variable
/// (for example <c>Icon.Outline_Thickness</c>) instead of the container's <c>ShaderParameters</c> text.
/// A <see cref="VariableGridEntry"/> carrying one stamps the marker on the variable it writes, gives the
/// variable its type, and treats a value still stored in the legacy text as this parameter's value
/// until the row is edited (which moves that one parameter out of the text) or made default.
/// </summary>
public sealed class ShaderParameterVariableBinding
{
    private readonly ShaderParameterInfo _info;
    private readonly string _legacyTextVariableName;
    private readonly int _componentIndex;
    private readonly Func<int, string>? _getComponentVariableName;

    /// <param name="info">The parameter as the compiled shader declares it; must be a single value.</param>
    /// <param name="legacyTextVariableName">The instance qualified name of the <c>ShaderParameters</c> text variable.</param>
    public ShaderParameterVariableBinding(ShaderParameterInfo info, string legacyTextVariableName)
    {
        _info = info;
        _legacyTextVariableName = legacyTextVariableName;
        _componentIndex = -1;
        Marker = new ShaderParameterMarker(info.Name);
    }

    /// <summary>
    /// Binds the row of one component of a float vector parameter. Each component is its own saved variable
    /// (<c>Icon.Outline_Tint__X</c>), but the runtime needs the whole vector, so writing one component saves all of them.
    /// </summary>
    /// <param name="info">The parameter as the compiled shader declares it; a float with 2 to 4 components.</param>
    /// <param name="legacyTextVariableName">The instance qualified name of the <c>ShaderParameters</c> text variable.</param>
    /// <param name="componentIndex">The zero based component this row edits.</param>
    /// <param name="getComponentVariableName">The instance qualified variable name of a component, given its index.</param>
    public ShaderParameterVariableBinding(
        ShaderParameterInfo info, string legacyTextVariableName, int componentIndex, Func<int, string> getComponentVariableName)
    {
        _info = info;
        _legacyTextVariableName = legacyTextVariableName;
        _componentIndex = componentIndex;
        _getComponentVariableName = getComponentVariableName;
        Marker = new ShaderParameterMarker(info.Name, componentIndex, info.Count);
    }

    /// <summary>The marker whose <c>Format()</c> is saved in <c>VariableSave.ShaderParameter</c>.</summary>
    public ShaderParameterMarker Marker { get; }

    /// <summary>Whether this row edits one component of a vector parameter.</summary>
    public bool IsComponent => _componentIndex >= 0;

    /// <summary>The Gum variable type name (<c>float</c>, <c>int</c> or <c>bool</c>).</summary>
    public string VariableType => _info.Kind switch
    {
        ShaderParameterKind.Int => "int",
        ShaderParameterKind.Bool => "bool",
        _ => "float",
    };

    /// <summary>The CLR type of the value the row edits.</summary>
    public Type ValueType => _info.Kind switch
    {
        ShaderParameterKind.Int => typeof(int),
        ShaderParameterKind.Bool => typeof(bool),
        _ => typeof(float),
    };

    /// <summary>The value the row shows when nothing is saved: the legacy text entry, else the compiled default.</summary>
    public object? GetFallbackValue(StateSave state)
    {
        object? legacy = GetLegacyValue(state, Math.Max(_componentIndex, 0));
        return legacy ?? GetDefaultValue(Math.Max(_componentIndex, 0));
    }

    private object GetDefaultValue(int componentIndex) =>
        ShaderParameterRows.ToDisplayValue(_info.Kind, ShaderParameterRows.GetComponent(_info.DefaultValue, componentIndex));

    /// <summary>
    /// Saves the components of this vector parameter that <paramref name="state"/> does not set yet, each with the value it
    /// shows now (an inherited value, else the legacy text, else the compiled default), so the runtime gets a complete vector.
    /// Call it before the legacy text entry is removed. A scalar binding does nothing.
    /// </summary>
    /// <param name="state">The state the components are saved on.</param>
    /// <param name="instance">The instance that owns the shader.</param>
    /// <param name="includeThisComponent">False when the row's own component was just written.</param>
    /// <returns>The variables it wrote, with the value of each.</returns>
    public List<(VariableSave Variable, object Value)> PinComponents(StateSave state, InstanceSave? instance, bool includeThisComponent)
    {
        List<(VariableSave Variable, object Value)> pinned = new List<(VariableSave Variable, object Value)>();
        if (!IsComponent)
        {
            return pinned;
        }

        for (int i = 0; i < _info.Count; i++)
        {
            if (i == _componentIndex && !includeThisComponent)
            {
                continue;
            }

            string name = _getComponentVariableName!(i);
            VariableSave? existing = state.GetVariableSave(name);
            if (existing != null && existing.SetsValue && existing.Value != null)
            {
                continue;
            }

            object value = state.GetValueRecursive(name) ?? GetLegacyValue(state, i) ?? GetDefaultValue(i);
            state.SetValue(name, value, instance, VariableType);
            VariableSave written = state.GetVariableSave(name)!;
            ShaderParameterMarker componentMarker = new ShaderParameterMarker(_info.Name, i, _info.Count);
            written.ShaderParameter = componentMarker.Format();
            pinned.Add((written, value));
        }
        return pinned;
    }

    /// <summary>
    /// Removes the other components of this vector parameter from <paramref name="state"/>, as Make Default on one component
    /// resets them all; a component that is exposed keeps its entry but loses its value. True when anything changed.
    /// </summary>
    public bool ResetOtherComponents(StateSave state)
    {
        bool changed = false;
        if (!IsComponent)
        {
            return changed;
        }

        for (int i = 0; i < _info.Count; i++)
        {
            VariableSave? other = i == _componentIndex ? null : state.GetVariableSave(_getComponentVariableName!(i));
            if (other == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(other.ExposedAsName))
            {
                state.Variables.Remove(other);
            }
            else
            {
                other.Value = null;
                other.SetsValue = false;
            }
            changed = true;
        }
        return changed;
    }

    /// <summary>Converts an edited value to the CLR type of the row; false when it cannot be converted.</summary>
    public bool TryCoerce(object? value, out object? coerced)
    {
        coerced = null;
        if (!ShaderParameterRows.TryConvert(_info.Kind, value, out float component))
        {
            return false;
        }
        coerced = ShaderParameterRows.ToDisplayValue(_info.Kind, component);
        return true;
    }

    /// <summary>Whether the legacy text on <paramref name="state"/> itself stores a value for this parameter.</summary>
    public bool HasLegacyValue(StateSave state) => GetLegacyCollection(state)?.TryGetValue(_info.Name, out _) == true;

    /// <summary>
    /// Called after the row wrote its variable: stamps the marker and moves this parameter out of the legacy
    /// text of the same state, so the text no longer says something the variable overrides.
    /// </summary>
    public void AfterWrite(StateSave state, string variableName)
    {
        VariableSave? written = state.GetVariableSave(variableName);
        if (written != null)
        {
            if (string.IsNullOrEmpty(written.ShaderParameter))
            {
                written.ShaderParameter = Marker.Format();
            }
            if (string.IsNullOrEmpty(written.Type))
            {
                written.Type = VariableType;
            }
        }
        RemoveLegacyValue(state);
    }

    /// <summary>
    /// Saves a marked placeholder (no value) for this parameter on <paramref name="defaultState"/> when none exists, so
    /// something that names the parameter, such as exposing it, can find it. Returns the placeholder it added, or null
    /// when the variable was already saved.
    /// </summary>
    public VariableSave? AddNamedPlaceholder(StateSave defaultState, string variableName)
    {
        if (defaultState.GetVariableSave(variableName) != null)
        {
            return null;
        }

        VariableSave placeholder = new VariableSave
        {
            Name = variableName,
            Type = VariableType,
            ShaderParameter = Marker.Format(),
            SetsValue = false,
        };
        defaultState.Variables.Add(placeholder);
        return placeholder;
    }

    /// <summary>Removes this parameter from the legacy text on <paramref name="state"/>; true when it was there.</summary>
    public bool RemoveLegacyValue(StateSave state)
    {
        ShaderParameterCollection? collection = GetLegacyCollection(state);
        if (collection == null || !collection.Remove(_info.Name))
        {
            return false;
        }

        // The text variable stays (it may be exposed or carry other parameters); only its value shrinks.
        state.GetVariableSave(_legacyTextVariableName)!.Value = ShaderParameterText.Format(collection);
        return true;
    }

    private object? GetLegacyValue(StateSave state, int componentIndex)
    {
        ShaderParameterCollection collection = ShaderParameterText.Parse(state.GetValueRecursive(_legacyTextVariableName) as string, null);
        if (!collection.TryGetValue(_info.Name, out ShaderParameterValue stored))
        {
            return null;
        }
        return ShaderParameterRows.ToDisplayValue(_info.Kind, ShaderParameterRows.GetComponent(stored, componentIndex));
    }

    private ShaderParameterCollection? GetLegacyCollection(StateSave state)
    {
        if (state.GetVariableSave(_legacyTextVariableName)?.Value is not string text)
        {
            return null;
        }
        return ShaderParameterText.Parse(text, null);
    }
}
