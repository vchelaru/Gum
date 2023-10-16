using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Gum.DataTypes.Variables
{
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
        object mValue;

        public bool IsFile
        {
            get;
            set;
        }
        public bool ShouldSerializeIsFile()
        {
            return IsFile == true;
        }

        public bool IsFont
        {
            get;
            set;
        }
        public bool ShouldSerializeIsFont()
        {
            return IsFont == true;
        }

        public string Type
        {
            get;
            set;
        }

        // rootName and sourceObject are used so frequently that storing them off 
        // should save in performance.
        string name;
        string rootName;
        string sourceObject;
        public string Name
        {
            get => name;
            set
            {
                name = value;

                if (name != null)
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

        public object Value
        {
            get { return mValue; }
            set
            {
                mValue = value;
            }
        }

        [XmlIgnore]
        public string SourceObject => sourceObject;

        public static string GetSourceObject(string variableName)
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
        public string ExposedAsName
        {
            get;
            set;
        }

        public string Category
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether a null value should be set, or whether the variable is
        /// an ignored value.  If this value is true, then null values will be set on the underlying data.
        /// </summary>
        public bool SetsValue
        {
            get;
            set;
        }

        public bool IsHiddenInPropertyGrid
        {
            get;
            set;
        }
        public bool ShouldSerializeIsHiddenInPropertyGrid()
        {
            return IsHiddenInPropertyGrid == true;
        }

        [XmlIgnore]
        public List<object> ExcludedValuesForEnum
        {
            get;
            set;
        }

#if !WINDOWS_8 && !UWP
        [XmlIgnore]
        public TypeConverter CustomTypeConverter
        {
            get;
            set;
        }
#endif

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

        // If adding stuff here, make sure to add to the Clone method!

        [XmlIgnore]
        public Dictionary<string, object> PropertiesToSetOnDisplayer { get; private set; } = new Dictionary<string, object>();


        public VariableSave Clone()
        {
            VariableSave toReturn = (VariableSave)this.MemberwiseClone();
#if !WINDOWS_8 && !UWP
            toReturn.CustomTypeConverter = this.CustomTypeConverter;
#endif
            toReturn.ExcludedValuesForEnum = new List<object>();
            toReturn.ExcludedValuesForEnum.AddRange(this.ExcludedValuesForEnum);


            return toReturn;
        }

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
}
