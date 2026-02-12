using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Gum.DataTypes.Variables;

public class StateSave
{
    #region Properties

    public string Name
    {
        get;
        set;
    }

    [XmlElement("Variable")]
    public List<VariableSave> Variables
    {
        get;
        set;
    }

    [XmlElement("VariableList")]
    public List<VariableListSave> VariableLists
    {
        get;
        set;
    }

    [XmlIgnore]
    public ElementSave ParentContainer
    {
        get;
        set;
    }

    // If adding here, modify Clone

    #endregion


    [XmlIgnore]
    public Action Apply;


    public StateSave()
    {
        Variables = new List<VariableSave>();
        VariableLists = new List<VariableListSave>();
    }

    public void Clear()
    {
        Apply = null;
        Variables.Clear();
        VariableLists.Clear();
    }

    public StateSave Clone()
    {
        StateSave toReturn = new StateSave();

        toReturn.Name = this.Name;
        toReturn.Variables = new List<VariableSave>();
        for (int i = 0; i < Variables.Count; i++ )
        {
            var variable = this.Variables[i];

            toReturn.Variables.Add(variable.Clone());
        }

        for (int i = 0; i < this.VariableLists.Count; i++ )
        {
            toReturn.VariableLists.Add(VariableLists[i].Clone());
        }
        toReturn.Apply = this.Apply;
        toReturn.ParentContainer = this.ParentContainer;

        return toReturn;
    }

    /// <summary>
    /// Performs a non-recursive search for the variable with the given name. 
    /// This will return the first variable found with the given name or null if 
    /// not found.
    /// </summary>
    /// <param name="variableName">The variable name</param>
    /// <returns>The found variable, or null if not found.</returns>
    public VariableSave? GetVariableSave(string variableName)
    {
        for(int i = Variables.Count-1; i > -1; i--)
        {
            var variable = Variables[i];
            if(variable.Name == variableName || variable.ExposedAsName == variableName)
            {
                return variable;
            }
        }
        return null;
    }

    public VariableListSave GetVariableListSave(string variableName)
    {
        var count = VariableLists.Count;
        for (int i = 0; i < count; i++)
        {
            var variableList = VariableLists[i];
            if (variableList.Name == variableName)
            {
                return variableList;
            }
        }
        return null;
    }

    public bool TryGetValue<T>(string variableName, out T result)
    {
        object value = GetValue(variableName);
        bool toReturn = false;

        if (value != null && value is T)
        {
            result = (T)value;
            toReturn = true;
        }
        else
        {
            result = default(T);
        }
        return toReturn;
    }

    public T GetValueOrDefault<T>(string variableName)
    {
        object toReturn = GetValue(variableName);

        if (toReturn == null || (toReturn is T) == false)
        {
            return default(T);
        }
        else
        {
            return (T)toReturn;
        }
    }

    /// <summary>
    /// Attempts to get the value for the argument variableName, or null if not found.
    /// </summary>
    /// <param name="variableName">The qualified variable name</param>
    /// <returns>The value found, or null</returns>
    public object GetValue(string variableName)
    {
        // Check for reserved stuff
        if (variableName == "Name" && ParentContainer != null)
        {
            return ParentContainer.Name;
        }
        else if ((variableName == "BaseType" || variableName == "Base Type") && ParentContainer != null)
        {
            if (string.IsNullOrEmpty(ParentContainer.BaseType))
            {
                return null;
            }
            else
            {
                string baseType = ParentContainer.BaseType;
                StandardElementTypes returnValue;

                if (Enum.TryParse<StandardElementTypes>(baseType, out returnValue))
                {
                    return returnValue;
                }
                else
                {
                    return baseType;
                }
            }
        }

        if (ToolsUtilities.StringFunctions.ContainsNoAlloc(variableName, '.') && ParentContainer != null)
        {
            string instanceName = variableName.Substring(0, variableName.IndexOf('.'));

            ElementSave elementSave = ParentContainer;
            InstanceSave instanceSave = null;

            if (elementSave != null)
            {
                instanceSave = elementSave.GetInstance(instanceName);
            }

            if (instanceSave != null)
            {
                // This is a variable on an instance
                if (variableName.EndsWith(".Name"))
                {
                    return instanceSave.Name;
                }
                else if (variableName.EndsWith(".Base Type") || variableName.EndsWith(".BaseType"))
                {
                    return instanceSave.BaseType;
                }
                else if (variableName.EndsWith(".Locked"))
                {
                    return instanceSave.Locked;
                }
                else if (variableName.EndsWith(".IsSlot"))
                {
                    return instanceSave.IsSlot;
                }
            }
        }

        VariableSave variableState = GetVariableSave(variableName);


        // If the user hasn't set this variable on this state, it'll be null. So let's just display null
        // for now.  Eventually we'll display a variable plus some kind of indication that it's an unset variable.
        if(variableState == null || variableState.SetsValue == false)
        {
            VariableListSave variableListSave = GetVariableListSave(variableName);
            if (variableListSave != null)
            {
                return variableListSave.ValueAsIList;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return variableState.Value;
        }
    }

    public VariableSave SetValue(string variableName, object? valueToSet, string? variableType = null)
    {
        VariableSave variableSave = GetVariableSave(variableName);

        if (variableSave == null)
        {
            variableSave = new VariableSave();
            variableSave.Name = variableName;
            variableSave.Type = variableType;
            Variables.Add(variableSave);
        }

        variableSave.Value = valueToSet;
        variableSave.SetsValue = true;

        return variableSave;
    }

    public override string ToString()
    {
        return this.Name + " in " + ParentContainer;
    }



}
