using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using ToolsUtilities;

namespace Gum.DataTypes.Variables
{
    [XmlInclude(typeof(VariableListSave<string>))]
    [XmlInclude(typeof(VariableListSave<float>))]
    [XmlInclude(typeof(VariableListSave<int>))]
    [XmlInclude(typeof(VariableListSave<long>))]
    [XmlInclude(typeof(VariableListSave<double>))]
    [XmlInclude(typeof(VariableListSave<bool>))]
    public abstract class VariableListSave
    {
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
        
        public string SourceObject
        {
            get;
            set;
        }

        public string Category
        {
            get;
            set;
        }
        
        public bool IsFile
        {
            get;
            set;
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

        [XmlIgnore]
        public abstract IList ValueAsIList
        {
            get;
            set;
        }

        public abstract void CreateNewList();

        public VariableListSave Clone()
        {
            VariableListSave toReturn = (VariableListSave)this.MemberwiseClone();

            if (ValueAsIList != null)
            {
                toReturn.CreateNewList();
                foreach (object value in this.ValueAsIList)
                {
                    toReturn.ValueAsIList.Add(value);
                }
            }
            return toReturn;
        }
    }


    public class VariableListSave<T> : VariableListSave
    {
        [XmlIgnore]
        public override IList ValueAsIList
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (List < T > )value;
            }
        }

        public List<T> Value
        {
            get;
            set;
        }
        
        public VariableListSave<T> Clone()
        {
            return FileManager.CloneSaveObject<VariableListSave<T>>(this);
        }
        
        public VariableListSave()
        {
            Value = new List<T>();
        }
        
        public override string ToString()
        {
            string returnValue = Type + " " + Name;

            if (Value != null)
            {
                returnValue = returnValue + " = " + Value;
            }

            return returnValue;
        }

        public override void CreateNewList()
        {
            Value = new List<T>();
        }
    }
}
