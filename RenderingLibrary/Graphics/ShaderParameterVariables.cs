using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Routes saved variables named like <c>Outline_Thickness</c> or <c>Outline_Tint__X</c> (see
/// <see cref="ShaderVariableNaming"/>) onto a container's <see cref="IRenderTargetRenderable.ShaderParameters"/>
/// (#7010), and decides who wins when the legacy <c>ShaderParameters</c> text names the same parameter.
/// </summary>
/// <remarks>
/// <para>
/// The runtime sees only a name and a value, and the variable, <c>SourceShaderFile</c> and the text can
/// arrive in any order (a state applies its variables sorted by name, and an exposed or referenced
/// variable arrives on its own). So each container keeps a small layer: the raw variable values, the
/// shader file, and a snapshot of the parameters that were there before any variable was applied. A
/// parameter is recomputed from those whenever one of them changes, so every order gives the same result.
/// </para>
/// <para>
/// Per parameter the variable wins over the text. A parameter only the text names still applies. A
/// variable whose name does not carry the current shader's stem is kept but inert, and applies if
/// <c>SourceShaderFile</c> later changes to that shader. A vector parameter is assembled from its
/// component variables; a missing component is zero, and the count is the highest component present
/// (at least two). Values set in code with <c>SetShaderParameter</c> write the collection directly, so
/// the latest writer wins as before; <see cref="ShaderParameterText.Assign"/> is unchanged and is
/// still what replaces the collection when the text is assigned.
/// </para>
/// </remarks>
public static class ShaderParameterVariables
{
    private static readonly ConditionalWeakTable<IRenderTargetRenderable, Layer> _layers =
        new ConditionalWeakTable<IRenderTargetRenderable, Layer>();

    private sealed class Layer
    {
        /// <summary>The shader file last assigned, or null for none.</summary>
        public string? ShaderFile;

        /// <summary>Every candidate variable value, keyed by full variable name, whatever its stem.</summary>
        public readonly Dictionary<string, object> Variables = new Dictionary<string, object>();

        /// <summary>
        /// A copy of the parameters as they were when the layer was created or the text last replaced
        /// them, used to put a value back when a variable stops covering it.
        /// </summary>
        public ShaderParameterCollection? Fallback;

        /// <summary>The parameter names currently written from variables.</summary>
        public readonly HashSet<string> Driven = new HashSet<string>();
    }

    /// <summary>
    /// Records a variable and applies it when its name carries the current shader's stem. Returns true
    /// when the variable belongs to the current shader, false when it is not a shader variable name for
    /// it (including when no shader file is set yet; it applies once one is). A null value clears the
    /// variable.
    /// </summary>
    public static bool TrySetVariable(IRenderTargetRenderable owner, string variableName, object? value)
    {
        // Every shader variable name is <stem>_<parameter>, so a name without an underscore after the
        // first character never is one. This keeps the check free for the common case.
        if (variableName == null || variableName.IndexOf('_') < 1)
        {
            return false;
        }

        Layer? layer;
        if (!_layers.TryGetValue(owner, out layer))
        {
            if (value == null)
            {
                return false;
            }
            layer = CreateLayer(owner);
        }

        if (value == null)
        {
            layer.Variables.Remove(variableName);
        }
        else
        {
            layer.Variables[variableName] = value;
        }

        if (layer.ShaderFile == null
            || !ShaderVariableNaming.TryParseVariableName(layer.ShaderFile, variableName, out string parameter, out _))
        {
            return false;
        }

        ApplyParameter(owner, layer, parameter);
        return true;
    }

    /// <summary>
    /// Records the shader file the container uses, which decides which variables belong to it, and
    /// recomputes the parameters. Call it before anything else in the handling of a
    /// <c>SourceShaderFile</c> change, including when the file is empty.
    /// </summary>
    public static void SetShaderFile(IRenderTargetRenderable owner, string? shaderFile)
    {
        string? file = string.IsNullOrEmpty(shaderFile) ? null : shaderFile;

        Layer? layer;
        if (!_layers.TryGetValue(owner, out layer))
        {
            if (file == null)
            {
                return;
            }
            layer = CreateLayer(owner);
        }

        layer.ShaderFile = file;
        RecomputeAll(owner, layer);
    }

    /// <summary>
    /// Assigns the legacy <c>ShaderParameters</c> text with <see cref="ShaderParameterText.Assign"/>, then
    /// lays the container's variables over it. Returns the same message <c>Assign</c> does.
    /// </summary>
    public static string? AssignText(IRenderTargetRenderable owner, string? text)
    {
        string? error = ShaderParameterText.Assign(owner, text);

        if (_layers.TryGetValue(owner, out Layer? layer))
        {
            layer.Fallback = Copy(owner.ShaderParameters);
            // The assignment replaced the collection, so nothing in it came from a variable any more.
            layer.Driven.Clear();
            RecomputeAll(owner, layer);
        }

        return error;
    }

    private static Layer CreateLayer(IRenderTargetRenderable owner)
    {
        Layer layer = new Layer();
        layer.Fallback = Copy(owner.ShaderParameters);
        _layers.Add(owner, layer);
        return layer;
    }

    private static ShaderParameterCollection? Copy(ShaderParameterCollection? source)
    {
        if (source == null)
        {
            return null;
        }

        ShaderParameterCollection copy = new ShaderParameterCollection();
        foreach (KeyValuePair<string, ShaderParameterValue> pair in source)
        {
            copy.Set(pair.Key, pair.Value);
        }
        return copy;
    }

    private static void RecomputeAll(IRenderTargetRenderable owner, Layer layer)
    {
        HashSet<string> parameters = new HashSet<string>(layer.Driven);
        if (layer.ShaderFile != null)
        {
            foreach (string variableName in layer.Variables.Keys)
            {
                if (ShaderVariableNaming.TryParseVariableName(layer.ShaderFile, variableName, out string parameter, out _))
                {
                    parameters.Add(parameter);
                }
            }
        }

        foreach (string parameter in parameters)
        {
            ApplyParameter(owner, layer, parameter);
        }
    }

    private static void ApplyParameter(IRenderTargetRenderable owner, Layer layer, string parameter)
    {
        if (layer.ShaderFile != null && TryAssemble(layer, parameter, out ShaderParameterValue value))
        {
            owner.ShaderParameters ??= new ShaderParameterCollection();
            owner.ShaderParameters.Set(parameter, value);
            layer.Driven.Add(parameter);
        }
        else if (layer.Driven.Remove(parameter))
        {
            if (layer.Fallback != null && layer.Fallback.TryGetValue(parameter, out ShaderParameterValue fallback))
            {
                owner.ShaderParameters ??= new ShaderParameterCollection();
                owner.ShaderParameters.Set(parameter, fallback);
            }
            else
            {
                owner.ShaderParameters?.Remove(parameter);
            }
        }
    }

    private static bool TryAssemble(Layer layer, string parameter, out ShaderParameterValue value)
    {
        string? file = layer.ShaderFile;
        float x = 0, y = 0, z = 0, w = 0;
        int highest = -1;

        for (int index = 0; index < ShaderParameterMarker.MaximumComponentCount; index++)
        {
            string componentName = ShaderVariableNaming.GetVariableName(
                file, parameter, index, ShaderParameterMarker.MaximumComponentCount);
            if (layer.Variables.TryGetValue(componentName, out object? raw) && TryToFloat(raw, out float component))
            {
                switch (index)
                {
                    case 0: x = component; break;
                    case 1: y = component; break;
                    case 2: z = component; break;
                    default: w = component; break;
                }
                highest = index;
            }
        }

        if (highest >= 0)
        {
            int count = System.Math.Max(highest + 1, ShaderParameterMarker.MinimumComponentCount);
            switch (count)
            {
                case 2: value = ShaderParameterValue.FromVector2(x, y); break;
                case 3: value = ShaderParameterValue.FromVector3(x, y, z); break;
                default: value = ShaderParameterValue.FromVector4(x, y, z, w); break;
            }
            return true;
        }

        string scalarName = ShaderVariableNaming.GetVariableName(file, parameter);
        if (layer.Variables.TryGetValue(scalarName, out object? scalar))
        {
            switch (scalar)
            {
                case float floatValue:
                    value = ShaderParameterValue.FromFloat(floatValue);
                    return true;
                case double doubleValue:
                    value = ShaderParameterValue.FromFloat((float)doubleValue);
                    return true;
                case int intValue:
                    value = ShaderParameterValue.FromInt(intValue);
                    return true;
                case bool boolValue:
                    value = ShaderParameterValue.FromBool(boolValue);
                    return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryToFloat(object value, out float result)
    {
        switch (value)
        {
            case float floatValue:
                result = floatValue;
                return true;
            case double doubleValue:
                result = (float)doubleValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case bool boolValue:
                result = boolValue ? 1f : 0f;
                return true;
        }

        result = 0f;
        return false;
    }
}
