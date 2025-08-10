using System.Collections.Generic;
using System.Xml.Serialization;
using System.Collections;
using ToolsUtilities;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using System;

namespace Gum.DataTypes.Variables
{
    [XmlInclude(typeof(VariableListSave<string>))]
    [XmlInclude(typeof(VariableListSave<float>))]
    [XmlInclude(typeof(VariableListSave<int>))]
    [XmlInclude(typeof(VariableListSave<long>))]
    [XmlInclude(typeof(VariableListSave<double>))]
    [XmlInclude(typeof(VariableListSave<bool>))]
    [XmlInclude(typeof(VariableListSave<Vector2>))]
    public abstract class VariableListSave
    {
        /// <summary>
        ///  The type of each individual item in the list. For example, this should be "int" rather than a list of int
        /// </summary>
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

        [XmlIgnore]
        public string SourceObject
        {
            get
            {
                return VariableSave.GetSourceObject(Name);
            }
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


        [XmlIgnore]
        public Type PreferredDisplayer { get; set; }

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

        public bool IsHiddenInPropertyGrid
        {
            get;
            set;
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
                Value = (List<T>)value;
            }
        }

        public List<T> Value
        {
            get;
            set;
        }

        public new VariableListSave<T> Clone()
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
