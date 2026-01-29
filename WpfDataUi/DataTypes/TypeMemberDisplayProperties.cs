using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfDataUi.DataTypes;

public class TypeMemberDisplayProperties
{
    #region Properties


    public List<InstanceMemberDisplayProperties> DisplayProperties
    {
        get;
        set;
    }

    #endregion

    #region Constructor

    public TypeMemberDisplayProperties()
    {
        DisplayProperties = new List<InstanceMemberDisplayProperties>();
    }

    #endregion

    public TypeMemberDisplayProperties Clone()
    {
        TypeMemberDisplayProperties toReturn = new TypeMemberDisplayProperties();

        foreach (var item in this.DisplayProperties)
        {
            toReturn.DisplayProperties.Add(item.Clone());
        }

        return toReturn;
    }

    public void AddIgnore(string name)
    {
        InstanceMemberDisplayProperties newImdp = GetOrCreateImdp(name);
        newImdp.IsHidden = true;
    }

    public void SetCategory(string name, string category)
    {
        InstanceMemberDisplayProperties newImdp = GetOrCreateImdp(name);
        newImdp.Category = category; 
    }

    public void SetPreferredDisplayer(string name, Type displayerType)
    {
        InstanceMemberDisplayProperties newImdp = GetOrCreateImdp(name);
        newImdp.PreferredDisplayer = displayerType; 
    }

    public void SetDisplay(string name, string displayName)
    {
        InstanceMemberDisplayProperties newImdp = GetOrCreateImdp(name);
        newImdp.DisplayName = displayName;
    }

    public InstanceMemberDisplayProperties GetOrCreateImdp(string name)
    {
        InstanceMemberDisplayProperties? newImdp = DisplayProperties.FirstOrDefault(item => item.Name == name);
        if (newImdp == null)
        {
            newImdp = new InstanceMemberDisplayProperties();
            DisplayProperties.Add(newImdp);
            newImdp.Name = name;
        }

        return newImdp;
    }
}
