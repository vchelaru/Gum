using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Gum.DataTypes.Variables;

/// <summary>
/// Struct representation of VariableSave which can be used in situations where
/// heap allocation should not occur
/// </summary>
public struct VariableSaveValues
{
    public object Value;
    public string Name;
}

[System.Diagnostics.DebuggerDisplay("{Name} = {Value}")]
public class VariableSave
{
    object? mValue;

    public bool IsFile
    {
        get;
        set;
    }
    public bool ShouldSerializeIsFile()
    {
        return IsFile;
    }

    public bool IsFont
    {
        get;
        set;
    }
    public bool ShouldSerializeIsFont()
    {
        return IsFont;
    }

    public string Type
    {
        get;
        set;
    } = string.Empty;

    // rootName and sourceObject are used so frequently that storing them off 
    // should save in performance.
    string name;
    string rootName;
    string? sourceObject;
    public string Name
    {
        get => name;
        set
        {
            name = value;

            if(name != null)
            {
                int dotIndex = name.IndexOf('.');
                if (dotIndex == -1)
                {
                    rootName = name;
                }
                else
                {
                    rootName = name.Substring(1 + dotIndex);
                }

                if (dotIndex != -1)
                {
                    sourceObject = name.Substring(0, dotIndex);
                }
                else
                {
                    sourceObject = null;
                }
            }
        }
    }
    
    public string StandardizedName { get; set; } = string.Empty;
    public bool ShouldSerializeStandardizedName() => !string.IsNullOrEmpty(StandardizedName);

    public object? Value
    {
        get => mValue; 
        set => mValue = value;
    }

    /// <summary>
    /// The name of the object that this variable references. For example if the variable is "MyButton.Text", then the SourceObject is "MyButton"
    /// </summary>
    [XmlIgnore]
    public string? SourceObject => sourceObject;

    public static string? GetSourceObject(string variableName)
    {
        int dotIndex = variableName.IndexOf('.');
        if (dotIndex != -1)
        {
            return variableName.Substring(0, dotIndex);
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// If a Component contains an instance then the variable
    /// of that instance is only editable inside that component.
    /// The user must explicitly expose that variable.  If the variable
    /// is exposed then this variable is set.
    /// </summary>
    public string? ExposedAsName
    {
        get;
        set;
    }

    public string Category
    {
        get;
        set;
    } = string.Empty;
    public bool ShouldSerializeCategory() => !string.IsNullOrEmpty(Category);

    /// <summary>
    /// Determines whether a null value should be set, or whether the variable is
    /// an ignored value.  If this value is true, then null values will be set on the underlying data.
    /// </summary>
    public bool SetsValue
    {
        get;
        set;
    }
    // Update June 16, 2024
    // Making this true because it 
    // is super inconvenient to have 
    // this be false when creating states
    // in code:
    = true;

    public bool IsHiddenInPropertyGrid
    {
        get;
        set;
    }
    public bool ShouldSerializeIsHiddenInPropertyGrid()
    {
        return IsHiddenInPropertyGrid == true;
    }

    public bool IsCustomVariable { get; set; }
    public bool ShouldSerializeIsCustomVariable() => IsCustomVariable;

    [XmlIgnore]
    public List<object> ExcludedValuesForEnum
    {
        get;
        set;
    } = new List<object>();

    [XmlIgnore]
    public TypeConverter CustomTypeConverter
    {
        get;
        set;
    }

    [XmlIgnore]
    public bool CanOnlyBeSetInDefaultState { get; set; }

    [XmlIgnore]
    public bool ExcludeFromInstances { get; set; }

    [XmlIgnore]
    public int DesiredOrder
    {
        get;
        set;
    }

    [XmlIgnore]
    public Dictionary<string, object?> PropertiesToSetOnDisplayer { get; private set; } = new Dictionary<string, object?>();

    [XmlIgnore]
    public Type PreferredDisplayer { get; set; }
    // If adding stuff here, make sure to add to the Clone method!

    /// <summary>
    /// Authored documentation for this variable, persisted alongside the declaration in the
    /// containing save file (typically a <c>FormsProperty</c> entry inside a <c>.behx</c>, or
    /// a standard variable declaration). This describes <i>what the variable is</i> and does
    /// not change at runtime.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="DetailText"/>: <see cref="Description"/> is invariant authoring
    /// data that ships with the project file, whereas <see cref="DetailText"/> is a transient,
    /// state-dependent hint set by tool/plugin code at runtime. The variable grid surfaces
    /// <see cref="Description"/> by seeding the row's display text from it; later runtime
    /// hints can still overwrite or append to that display text via <see cref="DetailText"/>.
    /// </remarks>
    public string? Description { get; set; }

    public bool ShouldSerializeDescription() => !string.IsNullOrEmpty(Description);

    /// <summary>
    /// Transient, runtime-only display hint shown in the variable grid for this variable's
    /// row. Populated by tool/plugin code based on the current project state — e.g.
    /// <c>"bmfont cannot generate from .ttf files. Switch to KernSmith in Project Properties."</c>
    /// shown when the active font configuration is incompatible. Not serialized.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="Description"/>: <see cref="DetailText"/> describes
    /// <i>what's currently true given the project state</i>, while <see cref="Description"/>
    /// describes <i>what the variable is</i> and is persisted. The variable grid seeds this
    /// field from <see cref="Description"/> when surfacing FormsProperty declarations, so a
    /// row with no transient hints still shows the authored documentation.
    /// </remarks>
    [XmlIgnore]
    public string DetailText { get; set; } = string.Empty;

    [XmlIgnore]
    public string? ToolTipText { get; set; }


    public VariableSave Clone()
    {
        VariableSave toReturn = (VariableSave)this.MemberwiseClone();
        toReturn.CustomTypeConverter = this.CustomTypeConverter;
        toReturn.ExcludedValuesForEnum = new List<object>();
        toReturn.ExcludedValuesForEnum.AddRange(this.ExcludedValuesForEnum);


        return toReturn;
    }

    /// <summary>
    /// Returns the name of the variable on the instance. For example "Rectangle.X" would return "X".
    /// If this does not have a SourceObect, then the Name is returned.
    /// </summary>
    /// <returns>The root name (name on the instance)</returns>
    public string GetRootName() => rootName;

    public static string GetRootName(string variableName)
    {
        if (variableName != null && ToolsUtilities.StringFunctions.ContainsNoAlloc(variableName, '.'))
        {
            return variableName.Substring(1 + variableName.IndexOf('.'));
        }
        else
        {
            return variableName;
        }
    }


    public VariableSave()
    {
        ExcludedValuesForEnum = new List<object>();

        DesiredOrder = int.MaxValue;
    }


    public override string ToString()
    {
        string returnValue = Name + " (" + Type + ")";

        if (Value != null)
        {
            returnValue = returnValue + " = " + Value;
        }

        if (!string.IsNullOrEmpty(ExposedAsName))
        {
            returnValue += "[exposed as " + ExposedAsName + "]";
        }

        return returnValue;
    }
}
