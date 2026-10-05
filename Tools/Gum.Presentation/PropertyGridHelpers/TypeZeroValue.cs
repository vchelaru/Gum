namespace Gum.PropertyGridHelpers;

/// <summary>
/// The value a numeric or bool variable holds when nothing else gives it one, such as a custom variable
/// whose default was cleared. Returns null for types with no such value (strings, enums, states).
/// </summary>
public static class TypeZeroValue
{
    public static object? For(string? type)
    {
        switch (type)
        {
            case "float": return 0f;
            case "double": return 0.0;
            case "int": return 0;
            case "bool": return false;
            default: return null;
        }
    }
}
