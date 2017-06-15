using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RenderingLibrary.Graphics;
using Gum.DataTypes;

namespace Gum.Reflection
{
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
                if (typeAsString == "float") return typeof(float);
                if (typeAsString == "int") return typeof(int);
                if (typeAsString == "double") return typeof(double);
                if (typeAsString == "string") return typeof(string);
                if (typeAsString == "long") return typeof(long);
                if (typeAsString == "char") return typeof(char);


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
}
