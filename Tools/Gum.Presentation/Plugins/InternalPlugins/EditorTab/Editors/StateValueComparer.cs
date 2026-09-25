using System.Collections;

namespace Gum.Wireframe.Editors;

/// <summary>
/// Decides whether a variable value changed between the grab snapshot and the end of a canvas edit,
/// so end-of-edit logic raises change events only for values that actually changed.
/// </summary>
public static class StateValueComparer
{
    public static bool DoValuesDiffer(object? oldValue, object? newValue)
    {
        if (oldValue == null || newValue == null)
        {
            return oldValue != newValue;
        }
        if (oldValue is IList oldList && newValue is IList newList)
        {
            return !AreListsSame(oldList, newList);
        }
        return !oldValue.Equals(newValue);
    }

    private static bool AreListsSame(IList oldList, IList newList)
    {
        if (oldList.Count != newList.Count)
        {
            return false;
        }
        for (int i = 0; i < oldList.Count; i++)
        {
            if (!Equals(oldList[i], newList[i]))
            {
                return false;
            }
        }
        return true;
    }
}
