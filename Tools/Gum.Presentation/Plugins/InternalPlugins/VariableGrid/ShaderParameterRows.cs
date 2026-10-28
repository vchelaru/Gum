using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Builds the Variables tab rows for a shader's parameters. A bool, int or float parameter gets one
/// row of its own type; a float vector gets one float row per component, named like
/// <c>OutlineColor X</c>. Every row reads and writes the container's saved <c>ShaderParameters</c>
/// text (see <see cref="ShaderParameterText"/>), so entries the shader does not declare are kept.
/// </summary>
public static class ShaderParameterRows
{
    private static readonly string[] ComponentNames = { "X", "Y", "Z", "W" };

    /// <summary>
    /// Creates the rows for <paramref name="parameters"/>. <paramref name="getText"/> reads the saved
    /// text and <paramref name="setText"/> stores the text after a row changes.
    /// </summary>
    public static List<SyntheticVariableRow> Build(
        IReadOnlyList<ShaderParameterInfo> parameters, Func<string?> getText, Action<string> setText)
    {
        List<SyntheticVariableRow> rows = new List<SyntheticVariableRow>();
        foreach (ShaderParameterInfo info in parameters)
        {
            // Gum fills this one in itself on every backend, so there is nothing for the user to set.
            if (info.Name == ShaderParameterCollection.TexelSizeName)
            {
                continue;
            }

            if (info.Count == 1)
            {
                rows.Add(CreateRow(info, componentIndex: 0, info.Name, getText, setText));
            }
            else
            {
                for (int i = 0; i < info.Count; i++)
                {
                    rows.Add(CreateRow(info, i, info.Name + " " + ComponentNames[i], getText, setText));
                }
            }
        }
        return rows;
    }

    private static SyntheticVariableRow CreateRow(
        ShaderParameterInfo info, int componentIndex, string rowName, Func<string?> getText, Action<string> setText)
    {
        Type valueType = info.Kind switch
        {
            ShaderParameterKind.Int => typeof(int),
            ShaderParameterKind.Bool => typeof(bool),
            _ => typeof(float),
        };

        return new SyntheticVariableRow
        {
            Name = rowName,
            ValueType = valueType,
            Get = () => ToDisplayValue(info.Kind, GetComponent(GetCurrent(info, getText), componentIndex)),
            Set = value =>
            {
                if (!TryConvert(info.Kind, value, out float component))
                {
                    return;
                }

                ShaderParameterCollection collection = ShaderParameterText.Parse(getText(), null);
                collection.Set(info.Name, WithComponent(info, GetCurrent(info, getText), componentIndex, component));
                setText(ShaderParameterText.Format(collection));
            },
        };
    }

    private static ShaderParameterValue GetCurrent(ShaderParameterInfo info, Func<string?> getText)
    {
        ShaderParameterCollection collection = ShaderParameterText.Parse(getText(), null);
        return collection.TryGetValue(info.Name, out ShaderParameterValue stored) ? stored : info.DefaultValue;
    }

    private static float GetComponent(ShaderParameterValue value, int index) => index switch
    {
        0 => value.X,
        1 => value.Y,
        2 => value.Z,
        _ => value.W,
    };

    private static object ToDisplayValue(ShaderParameterKind kind, float component) => kind switch
    {
        ShaderParameterKind.Int => (int)component,
        ShaderParameterKind.Bool => component != 0f,
        _ => component,
    };

    private static bool TryConvert(ShaderParameterKind kind, object? value, out float component)
    {
        component = 0f;
        try
        {
            switch (kind)
            {
                case ShaderParameterKind.Bool:
                    component = Convert.ToBoolean(value, CultureInfo.InvariantCulture) ? 1f : 0f;
                    return true;
                case ShaderParameterKind.Int:
                    component = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    return true;
                default:
                    component = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    return true;
            }
        }
        catch (Exception exception) when (exception is FormatException || exception is InvalidCastException || exception is OverflowException)
        {
            return false;
        }
    }

    private static ShaderParameterValue WithComponent(
        ShaderParameterInfo info, ShaderParameterValue current, int index, float component)
    {
        if (info.Kind == ShaderParameterKind.Bool)
        {
            return ShaderParameterValue.FromBool(component != 0f);
        }
        if (info.Kind == ShaderParameterKind.Int)
        {
            return ShaderParameterValue.FromInt((int)component);
        }

        float[] values = { current.X, current.Y, current.Z, current.W };
        values[index] = component;
        switch (info.Count)
        {
            case 2:
                return ShaderParameterValue.FromVector2(values[0], values[1]);
            case 3:
                return ShaderParameterValue.FromVector3(values[0], values[1], values[2]);
            case 4:
                return ShaderParameterValue.FromVector4(values[0], values[1], values[2], values[3]);
            default:
                return ShaderParameterValue.FromFloat(values[0]);
        }
    }
}
