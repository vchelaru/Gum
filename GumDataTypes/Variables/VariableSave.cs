using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Gum.DataTypes.Variables
{
    public class VariableSave
    {
        object mValue;
        string mSourceObject;

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

        public string Name
        {
            get;
            set;
        }

        public object Value
        {
            get { return mValue; }
            set
            {
                mValue = value;
            }
        }

        public string SourceObject
        {
            get { return mSourceObject; }
            set 
            { 
                mSourceObject = value; 
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

        [XmlIgnore]
        public List<object> ExcludedValuesForEnum
        {
            get;
            set;
        }

        [XmlIgnore]
        public TypeConverter CustomTypeConverter
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool CanOnlyBeSetInDefaultState { get; set; }

        // If adding stuff here, make sure to add to the Clone method!


        public VariableSave Clone()
        {
            VariableSave toReturn = (VariableSave)this.MemberwiseClone();
            toReturn.CustomTypeConverter = this.CustomTypeConverter;
            return toReturn;
        }

        public string GetRootName()
        {

            if (string.IsNullOrEmpty(SourceObject))
            {
                return Name;
            }
            else
            {
                return Name.Substring(1 + Name.IndexOf('.'));
            }
        }


        public VariableSave()
        {
            ExcludedValuesForEnum = new List<object>();
        }




    }
}
