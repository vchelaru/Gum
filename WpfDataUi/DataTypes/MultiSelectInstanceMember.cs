using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfDataUi.DataTypes;

public class MultiSelectInstanceMember : InstanceMember
{
    #region Events

    /// <summary>
    /// Raised before setting values on multiple instances. Allows subscribers to prepare for the multi-set operation,
    /// such as requesting undo locks.
    /// </summary>
    public event Action<SetPropertyArgs> BeforeMultiSet;

    /// <summary>
    /// Raised after setting values on multiple instances. Allows subscribers to clean up after the multi-set operation,
    /// such as disposing undo locks and recording undo.
    /// </summary>
    public event Action<SetPropertyArgs> AfterMultiSet;

    #endregion

    public override bool IsDefault
    {
        get => InstanceMembers.All(item => item.IsDefault); 

        set
        {
            foreach(var item in InstanceMembers)
            {
                item.IsDefault = value;
            }
        }
    }

    public override bool IsIndeterminate
    {
        get
        {
            if(InstanceMembers.Count() < 2)
            {
                return false;
            }
            else
            {
                var firstValue = InstanceMembers[0].Value;
                foreach (var innerMember in InstanceMembers.Skip(1))
                {
                    if (!object.Equals(firstValue, innerMember.Value))
                    {
                        return true;
                    }
                }
                return false;
            }

        }
    }

    IReadOnlyList<InstanceMember> instanceMembers;
    public IReadOnlyList<InstanceMember> InstanceMembers 
    {
        get => instanceMembers;
        set
        {
            instanceMembers = value;
            ReactToInstanceMembersSet();
        }
    }

    private void ReactToInstanceMembersSet()
    {
        // we should only allow custom options based on the first instance member
        // This is faster than going through all of them to see what they all have
        if(instanceMembers?.Count > 0)
        {
            this.CustomOptions = instanceMembers[0].CustomOptions;
            this.PreferredDisplayer = instanceMembers[0].PreferredDisplayer;
            foreach(var kvp in instanceMembers[0].PropertiesToSetOnDisplayer)
            {
                this.PropertiesToSetOnDisplayer.Add(kvp.Key, kvp.Value);
            }
        }
        else
        {
            this.CustomOptions = new List<object>();
            this.PreferredDisplayer = null;
            this.PropertiesToSetOnDisplayer.Clear();
        }
    }

    public MultiSelectInstanceMember() 
    {
        //CustomSetEvent += HandleCustomSetEvent;
        CustomSetPropertyEvent += HandleCustomSetEvent;
        CustomGetEvent += HandleCustomGetEvent;
        CustomGetTypeEvent += HandleCustomGetTypeEvent;

        SetValueError = HandleValueError;
    }

    private void HandleCustomSetEvent(object owner, SetPropertyArgs value)
    {
        BeforeMultiSet?.Invoke(value);

        foreach(var innerMember in InstanceMembers)
        {
            //innerMember.Value = value;
            innerMember.SetValue(value.Value, value.CommitType);
        }

        AfterMultiSet?.Invoke(value);
    }

    private object HandleCustomGetEvent(object owner)
    {
        if(InstanceMembers.Count == 0) return null;
        else if (InstanceMembers.Count == 1) return InstanceMembers[0].Value;
        else
        {
            var firstValue = InstanceMembers[0].Value;
            foreach(var innerMember in InstanceMembers.Skip(1))
            {
                if(!object.Equals(firstValue, innerMember.Value))
                {
                    return null;
                }
            }
            return firstValue;
        }
    }

    private Type HandleCustomGetTypeEvent(object arg)
    {
        return InstanceMembers.FirstOrDefault()?.PropertyType;
    }

    private void HandleValueError(object obj)
    {
        foreach(var innerMember in InstanceMembers)
        {
            innerMember.SetValueError?.Invoke(obj);
        }
    }
}
