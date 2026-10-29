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
/// shader file, and for every parameter a variable drives, the value it covered. A parameter is
/// recomputed from those whenever one of them changes, so every order gives the same result.
/// </para>
/// <para>
/// Per parameter the variable wins over the text. A parameter only the text names still applies. A
/// variable whose name does not carry the current shader's stem is kept but inert, and applies if
/// <c>SourceShaderFile</c> later changes to that shader. The stem is matched ignoring case (file names
/// and the tool's duplicate-name check are case-insensitive), so variable names that differ only in the
/// case of the stem address the same parameter. A vector parameter is assembled from its
/// component variables; a missing component is zero, and the count is the highest component present
/// (at least two). Values set in code with <c>SetShaderParameter</c> write the collection directly, so
/// the latest writer wins as before, and a code value written over a driven parameter is kept when the
/// variable goes away; <see cref="ShaderParameterText.Assign"/> is unchanged and is
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

        /// <summary>Every candidate variable value, keyed by full variable name as written, whatever its stem.</summary>
        public readonly Dictionary<string, Entry> Variables = new Dictionary<string, Entry>();

        /// <summary>Counts writes, so the latest of two names that address one parameter wins.</summary>
        public long WriteCount;

        /// <summary>The parameters currently written from variables, and what each one covered.</summary>
        public readonly Dictionary<string, Driver> Driven = new Dictionary<string, Driver>();
    }

    private struct Entry
    {
        public object Value;

        /// <summary>The <see cref="Layer.WriteCount"/> when the value was set.</summary>
        public long Sequence;
    }

    private struct Driver
    {
        /// <summary>The value this layer last wrote.</summary>
        public ShaderParameterValue Written;

        /// <summary>Whether the parameter had a value before the variable drove it.</summary>
        public bool HadBefore;

        /// <summary>That value, put back when the variable stops driving the parameter.</summary>
        public ShaderParameterValue Before;
    }

    /// <summary>
    /// Records a variable and applies it when its name carries the current shader's stem. Returns true
    /// when the variable belongs to the current shader, false when it is not a shader variable name for
    /// it (including when no shader file is set yet; it applies once one is). A null value clears the
    /// variable.
    /// </summary>
    public static bool TrySetVariable(IRenderTargetRenderable owner, string variableName, object? value)
    {
        if (!ShaderVariableNaming.IsPossibleVariableName(variableName))
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
            layer = new Layer();
            _layers.Add(owner, layer);
        }

        if (value == null)
        {
            layer.Variables.Remove(variableName);
        }
        else
        {
            layer.Variables[variableName] = new Entry { Value = value, Sequence = ++layer.WriteCount };
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
            layer = new Layer();
            _layers.Add(owner, layer);
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
            // The assignment replaced the collection, so nothing in it came from a variable any more.
            layer.Driven.Clear();
            RecomputeAll(owner, layer);
        }

        return error;
    }

    private static void RecomputeAll(IRenderTargetRenderable owner, Layer layer)
    {
        HashSet<string> parameters = new HashSet<string>(layer.Driven.Keys);
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
        ShaderParameterValue current = default;
        bool hasCurrent = owner.ShaderParameters != null && owner.ShaderParameters.TryGetValue(parameter, out current);
        bool isDriven = layer.Driven.TryGetValue(parameter, out Driver driver);

        if (layer.ShaderFile != null && TryAssemble(layer, parameter, out ShaderParameterValue value))
        {
            if (!isDriven)
            {
                driver = new Driver { HadBefore = hasCurrent, Before = current };
            }
            else if (!hasCurrent)
            {
                // Cleared in code since the last write: nothing to put back later.
                driver.HadBefore = false;
            }
            else if (!Same(current, driver.Written))
            {
                // Replaced in code since the last write: that value is what comes back later.
                driver.HadBefore = true;
                driver.Before = current;
            }

            driver.Written = value;
            layer.Driven[parameter] = driver;
            owner.ShaderParameters ??= new ShaderParameterCollection();
            owner.ShaderParameters.Set(parameter, value);
        }
        else if (isDriven)
        {
            layer.Driven.Remove(parameter);

            // Anything other than what this layer wrote was put there in code, and stays.
            if (hasCurrent && Same(current, driver.Written))
            {
                if (driver.HadBefore)
                {
                    owner.ShaderParameters!.Set(parameter, driver.Before);
                }
                else
                {
                    owner.ShaderParameters!.Remove(parameter);
                }
            }
        }
    }

    private static bool Same(ShaderParameterValue first, ShaderParameterValue second) =>
        first.Kind == second.Kind
        && first.Count == second.Count
        && first.X == second.X
        && first.Y == second.Y
        && first.Z == second.Z
        && first.W == second.W;

    private static bool TryAssemble(Layer layer, string parameter, out ShaderParameterValue value)
    {
        string? file = layer.ShaderFile;

        // Names are parsed with ShaderVariableNaming, so only canonical ones count: the stem matches
        // ignoring case, the parameter and the component suffix match exactly. When several names
        // address the same slot (stems differing only in case), the latest write wins.
        System.Span<float> components = stackalloc float[ShaderParameterMarker.MaximumComponentCount];
        System.Span<long> componentSequences = stackalloc long[ShaderParameterMarker.MaximumComponentCount];
        componentSequences.Fill(-1);
        int highest = -1;
        long scalarSequence = -1;
        object? scalar = null;

        foreach (KeyValuePair<string, Entry> pair in layer.Variables)
        {
            if (!ShaderVariableNaming.TryParseVariableName(file, pair.Key, out string name, out int? componentIndex)
                || !string.Equals(name, parameter, System.StringComparison.Ordinal))
            {
                continue;
            }

            Entry entry = pair.Value;
            if (componentIndex.HasValue)
            {
                int index = componentIndex.Value;
                if (entry.Sequence > componentSequences[index] && TryToFloat(entry.Value, out float component))
                {
                    components[index] = component;
                    componentSequences[index] = entry.Sequence;
                    if (index > highest)
                    {
                        highest = index;
                    }
                }
            }
            else if (entry.Sequence > scalarSequence)
            {
                scalar = entry.Value;
                scalarSequence = entry.Sequence;
            }
        }

        if (highest >= 0)
        {
            int count = System.Math.Max(highest + 1, ShaderParameterMarker.MinimumComponentCount);
            switch (count)
            {
                case 2: value = ShaderParameterValue.FromVector2(components[0], components[1]); break;
                case 3: value = ShaderParameterValue.FromVector3(components[0], components[1], components[2]); break;
                default: value = ShaderParameterValue.FromVector4(components[0], components[1], components[2], components[3]); break;
            }
            return true;
        }

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
