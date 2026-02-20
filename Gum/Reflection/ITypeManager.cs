using System;

namespace Gum.Reflection;

public interface ITypeManager
{
    void AddType(Type type);
    Type GetTypeFromString(string typeAsString);
    void Initialize();
}
