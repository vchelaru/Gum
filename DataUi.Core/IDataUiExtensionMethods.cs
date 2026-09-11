using System;
using System.Collections.Generic;
using WpfDataUi.DataTypes;

namespace WpfDataUi;

/// <summary>One entry in a displayer's right-click menu.</summary>
public sealed class DataUiContextMenuEntry
{
    /// <summary>Creates an entry.</summary>
    public DataUiContextMenuEntry(string header, Action execute)
    {
        Header = header;
        Execute = execute;
    }

    /// <summary>The menu text.</summary>
    public string Header { get; }

    /// <summary>What clicking the entry does.</summary>
    public Action Execute { get; }
}

public static class IDataUiExtensionMethods
{
    public static bool HasEnoughInformationToWork(this IDataUi dataUi)
    {
        return dataUi.InstanceMember.IsDefined;
    }

    public static bool TryGetValueOnInstance(this IDataUi dataUi, out object value)
    {
        //////////////////Early Out/////////////////////////////////
        if (dataUi.HasEnoughInformationToWork() == false || dataUi.InstanceMember.IsWriteOnly)
        {
            value = null;
            return false;
        }
        ////////////////End Early Out///////////////////////////////

        value = dataUi.InstanceMember.Value;

        return true;

    }

    public static ApplyValueResult TrySetValueOnInstance(this IDataUi dataUi)
    {
        ApplyValueResult result;
        bool hasErrorOccurred;
        GetIfValuesCanBeSetOnInstance(dataUi, out result, out hasErrorOccurred);

        if (!hasErrorOccurred)
        {

            object? valueOnUi;

            result = dataUi.TryGetValueOnUi(out valueOnUi);

            if (result == ApplyValueResult.Success)
            {
                // Why not protect against spammed same-value assignments?
                if(dataUi.InstanceMember.Value != valueOnUi)
                {
                    dataUi.InstanceMember.OldValue = dataUi.InstanceMember.Value;
                    result = dataUi.InstanceMember.SetValue(valueOnUi, SetPropertyCommitType.Full);
                    if(result == ApplyValueResult.Success)
                    {
                        dataUi.InstanceMember.CallAfterSetByUi();
                    }
                }
                else
                {
                    result = ApplyValueResult.Skipped;
                }
            }
        }

        return result;
    }

    public static ApplyValueResult TrySetValueOnInstance(this IDataUi dataUi, object valueToSet, SetPropertyCommitType commitType = SetPropertyCommitType.Full)
    {
        ApplyValueResult result;
        bool hasErrorOccurred;
        GetIfValuesCanBeSetOnInstance(dataUi, out result, out hasErrorOccurred);

        if (!hasErrorOccurred)
        {
            if (AreEqual(dataUi.InstanceMember.Value, valueToSet) == false || commitType == SetPropertyCommitType.Full)
            {
                dataUi.InstanceMember.OldValue = dataUi.InstanceMember.Value;
                result = dataUi.InstanceMember.SetValue(valueToSet, commitType);
                dataUi.InstanceMember.CallAfterSetByUi();
            }
            else
            {
                result = ApplyValueResult.Skipped;
            }

        }

        return result;
    }

    static bool AreEqual(object object1, object object2)
    {
        if(object1 is float && object2 is float)
        {
            return (float)object1 == (float)object2;
        }
        else if (object1 is double && object2 is double)
        {
            return (double)object1 == (double)object2;
        }
        else if (object1 is decimal && object2 is decimal)
        {
            return (decimal)object1 == (decimal)object2;
        }

        else if (object1 is int && object2 is int)
        {
            return (int)object1 == (int)object2;
        }
        else if (object1 is long && object2 is long)
        {
            return (long)object1 == (long)object2;
        }
        else if (object1 is short && object2 is short)
        {
            return (short)object1 == (short)object2;
        }

        else if (object1 is bool && object2 is bool)
        {
            return (bool)object1 == (bool)object2;
        }
        else if (object1 is char && object2 is char)
        {
            return (char)object1 == (char)object2;
        }
        else if (object1 is string && object2 is string)
        {
            return (string)object1 == (string)object2;
        }
        else
        {
            return object1 == object2;
        }

    }

    private static void GetIfValuesCanBeSetOnInstance(IDataUi dataUi, out ApplyValueResult result, out bool hasErrorOccurred)
    {
        result = ApplyValueResult.UnknownError;
        hasErrorOccurred = false;

        if (dataUi.HasEnoughInformationToWork() == false)
        {
            result = ApplyValueResult.NotEnoughInformation;
            hasErrorOccurred = true;
        }
        if (dataUi.InstanceMember.IsReadOnly)
        {
            result = ApplyValueResult.NotSupported;
            hasErrorOccurred = true;
        }
        if (dataUi.SuppressSettingProperty)
        {
            result = ApplyValueResult.NotEnabled;
            hasErrorOccurred = true;
        }
    }

    public static Type GetPropertyType(this IDataUi dataUi)
    {

        return dataUi.InstanceMember.PropertyType;
    }

    public static Type GetPropertyType(string propertyName, Type instanceType)
    {
        Type type;

        type = null;
        var fieldInfo = instanceType.GetField(propertyName);

        if (fieldInfo != null)
        {
            type = fieldInfo.FieldType;
        }

        // if we haven't found it yet
        if (type == null)
        {
            var propertyInfo = instanceType.GetProperty(propertyName);

            if (propertyInfo != null)
            {
                type = propertyInfo.PropertyType;
            }
        }
        return type;
    }

    /// <summary>
    /// The "Make Default" action: resets the member to its default, refreshes the displayer, clears
    /// any in-progress edit state, and reports the change as a UI set.
    /// </summary>
    public static void MakeDefault(this IDataUi dataUi)
    {
        InstanceMember? member = dataUi.InstanceMember;
        if (member == null)
        {
            return;
        }

        member.OldValue = member.Value;
        member.IsDefault = true;
        dataUi.Refresh();

        // The instance member may have undone the IsDefault, so only clear edit state if it held.
        if (dataUi is ISetDefaultable setDefaultable && member.IsDefault)
        {
            setDefaultable.SetToDefault();
        }

        member.CallAfterSetByUi();
    }

    /// <summary>
    /// The displayer's right-click entries: "Make Default" when the member supports it, then the
    /// member's <see cref="InstanceMember.ContextMenuEvents"/> in order.
    /// </summary>
    public static List<DataUiContextMenuEntry> GetContextMenuEntries(this IDataUi dataUi)
    {
        List<DataUiContextMenuEntry> entries = new List<DataUiContextMenuEntry>();
        InstanceMember? member = dataUi.InstanceMember;

        bool shouldAddMakeDefault = member == null || member.SupportsMakeDefault;
        if (shouldAddMakeDefault)
        {
            entries.Add(new DataUiContextMenuEntry("Make Default", dataUi.MakeDefault));
        }

        if (member != null)
        {
            foreach (KeyValuePair<string, EventHandler> kvp in member.ContextMenuEvents)
            {
                EventHandler handler = kvp.Value;
                entries.Add(new DataUiContextMenuEntry(kvp.Key, () => handler(member, EventArgs.Empty)));
            }
        }

        return entries;
    }
}
