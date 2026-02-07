using System;
using System.Collections.Generic;
using System.Reflection;
using RenderingLibrary.Graphics;
using Gum.DataTypes;

namespace Gum.Reflection;

public class TypeManager
{
    List<Type> mTypes;

    static TypeManager mSelf;

    public static TypeManager Self
    {
        get
        {
            if (mSelf == null)
            {
                mSelf = new TypeManager();
            }
            return mSelf;
        }
    }

    public void AddType(Type type)
    {
        mTypes.Add(type);
    }

    public void Initialize()
    {
        List<Type> allTypes = new List<Type>();

        allTypes.AddRange(Assembly.GetExecutingAssembly().GetTypes());

        allTypes.AddRange(Assembly.GetAssembly(typeof(VerticalAlignment)).GetTypes());

        allTypes.AddRange(Assembly.GetAssembly(typeof(DimensionUnitType)).GetTypes());

        mTypes = allTypes;
    }


    public Type GetTypeFromString(string typeAsString)
    {
        if (mTypes == null)
        {
            throw new Exception("Must call TypeManager.Initialize first");
        }

        if(typeAsString == null)
        {
            throw new ArgumentNullException(nameof(typeAsString));
        }

        bool isQualified = typeAsString.Contains(".");

        if (isQualified)
        {
            foreach (Type type in mTypes)
            {
                if (type.FullName == typeAsString)
                {
                    return type;
                }
            }
        }
        else
        {
            if (typeAsString == "bool") return typeof(bool);
            if (typeAsString == "bool?") return typeof(bool?);
            if (typeAsString == "float") return typeof(float);
            if (typeAsString == "float?") return typeof(float?);
            if (typeAsString == "int") return typeof(int);
            if (typeAsString == "int?") return typeof(int?);
            if (typeAsString == "double") return typeof(double);
            if (typeAsString == "double?") return typeof(double?);
            if (typeAsString == "decimal") return typeof(decimal);
            if (typeAsString == "decimal?") return typeof(decimal?);
            if (typeAsString == "string") return typeof(string);
            if (typeAsString == "long") return typeof(long);
            if (typeAsString == "long?") return typeof(long?);
            if (typeAsString == "char") return typeof(char);

            if (typeAsString == "List<string>") return typeof(List<string>);
            if (typeAsString == "List<int>") return typeof(List<int>);
            if (typeAsString == "List<double>") return typeof(List<double>);
            if (typeAsString == "List<bool>") return typeof(List<bool>);
            if (typeAsString == "List<float>") return typeof(List<float>);
            if (typeAsString == "List<Vector2>") return typeof(List<System.Numerics.Vector2>);
            if (typeAsString == "List<System.Numerics.Vector2>") return typeof(List<System.Numerics.Vector2>);

            // avoid ambiguity

            // We'd never use the XNA-like blend part of GumCore in the tool:
            if (typeAsString == "Blend") return typeof(Gum.RenderingLibrary.Blend);

            foreach (Type type in mTypes)
            {
                if (type.Name == typeAsString)
                {
                    return type;
                }
            }
        }

        return null;
    }
}
