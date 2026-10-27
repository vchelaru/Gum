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
        if (parameter.Elements.Count > 0)
        {
            return null;
        }

        if (parameter.ParameterClass == EffectParameterClass.Scalar)
        {
            switch (parameter.ParameterType)
            {
                case EffectParameterType.Single:
                    return new ShaderParameterInfo(parameter.Name, ShaderParameterKind.Float, 1,
                        ShaderParameterValue.FromFloat(parameter.GetValueSingle()));
                case EffectParameterType.Int32:
                    return new ShaderParameterInfo(parameter.Name, ShaderParameterKind.Int, 1,
                        ShaderParameterValue.FromInt(parameter.GetValueInt32()));
                case EffectParameterType.Bool:
                    return new ShaderParameterInfo(parameter.Name, ShaderParameterKind.Bool, 1,
                        ShaderParameterValue.FromBool(parameter.GetValueBoolean()));
            }
            return null;
        }

        if (parameter.ParameterClass == EffectParameterClass.Vector &&
            parameter.ParameterType == EffectParameterType.Single &&
            parameter.RowCount == 1)
        {
            switch (parameter.ColumnCount)
            {
                case 2:
                    {
                        Microsoft.Xna.Framework.Vector2 value = parameter.GetValueVector2();
                        return new ShaderParameterInfo(parameter.Name, ShaderParameterKind.Float, 2,
                            ShaderParameterValue.FromVector2(value.X, value.Y));
                    }
                case 3:
                    {
                        Microsoft.Xna.Framework.Vector3 value = parameter.GetValueVector3();
                        return new ShaderParameterInfo(parameter.Name, ShaderParameterKind.Float, 3,
                            ShaderParameterValue.FromVector3(value.X, value.Y, value.Z));
                    }
                case 4:
                    {
                        Microsoft.Xna.Framework.Vector4 value = parameter.GetValueVector4();
                        return new ShaderParameterInfo(parameter.Name, ShaderParameterKind.Float, 4,
                            ShaderParameterValue.FromVector4(value.X, value.Y, value.Z, value.W));
                    }
            }
        }

        return null;
    }
}
