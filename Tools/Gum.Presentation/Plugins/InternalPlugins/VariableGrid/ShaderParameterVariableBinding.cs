using Gum.DataTypes.Variables;
using RenderingLibrary.Graphics;
using System;

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

    /// <param name="info">The parameter as the compiled shader declares it; must be a single value.</param>
    /// <param name="legacyTextVariableName">The instance qualified name of the <c>ShaderParameters</c> text variable.</param>
    public ShaderParameterVariableBinding(ShaderParameterInfo info, string legacyTextVariableName)
    {
        _info = info;
        _legacyTextVariableName = legacyTextVariableName;
        Marker = new ShaderParameterMarker(info.Name);
    }

    /// <summary>The marker whose <c>Format()</c> is saved in <c>VariableSave.ShaderParameter</c>.</summary>
    public ShaderParameterMarker Marker { get; }

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
        object? legacy = GetLegacyValue(state);
        return legacy ?? ShaderParameterRows.ToDisplayValue(_info.Kind, _info.DefaultValue.X);
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

    private object? GetLegacyValue(StateSave state)
    {
        ShaderParameterCollection collection = ShaderParameterText.Parse(state.GetValueRecursive(_legacyTextVariableName) as string, null);
        if (!collection.TryGetValue(_info.Name, out ShaderParameterValue stored))
        {
            return null;
        }
        return ShaderParameterRows.ToDisplayValue(_info.Kind, stored.X);
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
