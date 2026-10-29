using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Lists the parameters of an XNA, MonoGame or KNI <see cref="Effect"/> the way
/// <see cref="ShaderParameterInfo"/> describes them. Only the kinds <c>SetShaderParameter</c> can set
/// are listed: bool, int and float scalars, and float vectors of two to four components. Textures,
/// matrices, arrays and structs are left out.
/// </summary>
public static class EffectShaderParameters
{
    /// <summary>Returns the settable parameters of <paramref name="effect"/>.</summary>
    public static IReadOnlyList<ShaderParameterInfo> Read(Effect effect)
    {
        List<ShaderParameterInfo> infos = new List<ShaderParameterInfo>();
        foreach (EffectParameter parameter in effect.Parameters)
        {
            ShaderParameterInfo? info = ToInfo(parameter);
            if (info != null)
            {
                infos.Add(info);
            }
        }
        return infos;
    }

    private static ShaderParameterInfo? ToInfo(EffectParameter parameter)
    {
        if (!TryReadValue(parameter, out ShaderParameterValue value))
        {
            return null;
        }
        return new ShaderParameterInfo(parameter.Name, value.Kind, value.Count, value);
    }

    /// <summary>
    /// Reads the value <paramref name="parameter"/> holds right now, as the kind and size it was declared with.
    /// Returns false for the kinds <c>SetShaderParameter</c> cannot set (textures, matrices, arrays, structs).
    /// Does not allocate.
    /// </summary>
    public static bool TryReadValue(EffectParameter parameter, out ShaderParameterValue value)
    {
        value = default;
        if (parameter.Elements.Count > 0)
        {
            return false;
        }

        if (parameter.ParameterClass == EffectParameterClass.Scalar)
        {
            switch (parameter.ParameterType)
            {
                case EffectParameterType.Single:
                    value = ShaderParameterValue.FromFloat(parameter.GetValueSingle());
                    return true;
                case EffectParameterType.Int32:
                    value = ShaderParameterValue.FromInt(parameter.GetValueInt32());
                    return true;
                case EffectParameterType.Bool:
                    value = ShaderParameterValue.FromBool(parameter.GetValueBoolean());
                    return true;
            }
            return false;
        }

        if (parameter.ParameterClass == EffectParameterClass.Vector &&
            parameter.ParameterType == EffectParameterType.Single &&
            parameter.RowCount == 1)
        {
            switch (parameter.ColumnCount)
            {
                case 2:
                    {
                        Microsoft.Xna.Framework.Vector2 v = parameter.GetValueVector2();
                        value = ShaderParameterValue.FromVector2(v.X, v.Y);
                        return true;
                    }
                case 3:
                    {
                        Microsoft.Xna.Framework.Vector3 v = parameter.GetValueVector3();
                        value = ShaderParameterValue.FromVector3(v.X, v.Y, v.Z);
                        return true;
                    }
                case 4:
                    {
                        Microsoft.Xna.Framework.Vector4 v = parameter.GetValueVector4();
                        value = ShaderParameterValue.FromVector4(v.X, v.Y, v.Z, v.W);
                        return true;
                    }
            }
        }

        return false;
    }
}
