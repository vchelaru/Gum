using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections;

namespace Gum.DataTypes.Variables
{
    public class StateSave
    {
        #region Properties

        [Browsable(false)]
        public string Name
        {
            get;
            set;
        }

        [Browsable(false)]
        [XmlElement("Variable")]
        public List<VariableSave> Variables
        {
            get;
            set;
        }

        [Browsable(false)]
        [XmlElement("VariableList")]
        public List<VariableListSave> VariableLists
        {
            get;
            set;
        }

        [Browsable(false)]
        [XmlIgnore]
        public ElementSave ParentContainer
        {
            get;
            set;
        }

        // If adding here, modify Clone

        #endregion

        public StateSave()
        {
            Variables = new List<VariableSave>();
            VariableLists = new List<VariableListSave>();
        }

        public StateSave Clone()
        {
            StateSave toReturn = new StateSave();

            toReturn.Name = this.Name;
            toReturn.Variables = new List<VariableSave>();
            foreach (var variable in this.Variables)
            {
                toReturn.Variables.Add(variable.Clone());
            }

            foreach (var variableList in this.VariableLists)
            {
                toReturn.VariableLists.Add(variableList.Clone());
            }

            toReturn.ParentContainer = this.ParentContainer;

            return toReturn;
        }

        public VariableSave GetVariableSave(string variableName)
        {
            foreach(var variableSave in Variables)
            {
                if(variableSave.Name == variableName || variableSave.ExposedAsName == variableName)
                {
                    return variableSave;
                }
            }



            return null;
        }

        public VariableListSave GetVariableListSave(string variableName)
        {
            foreach (var variableList in VariableLists)
            {
                if (variableList.Name == variableName)
                {
                    return variableList;
                }
            }
            return null;
        }

        public T GetValueOrDefault<T>(string variableName)
        {
            object toReturn = GetValue(variableName);

            if (toReturn == null)
            {
                return default(T);
            }
            else
            {
                return (T)toReturn;
            }
        }

        public object GetValue(string variableName)
        {
            // Check for reserved stuff
            if (variableName == "Name")
            {
                return ParentContainer.Name;
            }
            else if (variableName == "Base Type")
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

            if (variableName.Contains('.'))
            {
                string instanceName = variableName.Substring(0, variableName.IndexOf('.'));

                ElementSave elementSave = ParentContainer;
                InstanceSave instanceSave = elementSave.GetInstance(instanceName);

                if (instanceSave != null)
                {
                    // This is a variable on an instance
                    if (variableName.EndsWith(".Name"))
                    {
                        return instanceSave.Name;
                    }
                    else if (variableName.EndsWith(".Base Type"))
                    {
                        return instanceSave.BaseType;
                    }
                    else if (variableName.EndsWith(".Locked"))
                    {
                        return instanceSave.Locked;
                    }
                }

            }

            VariableSave variableState = GetVariableSave(variableName);


            // If the user hasn't set this variable on this state, it'll be null. So let's just display null
            // for now.  Eventually we'll display a variable plus some kind of indication that it's an unset variable.
            if(variableState == null)
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

        public override string ToString()
        {
            return this.Name + " in " + ParentContainer;
        }

        public void MergeIntoThis(StateSave other)
        {
            foreach (var variableSave in other.Variables)
            {
                VariableSave whatToSet = Variables.FirstOrDefault(item => item.Name == variableSave.Name);

                if (whatToSet == null)
                {
                    whatToSet = variableSave.Clone();
                    this.Variables.Add(whatToSet);
                }

                whatToSet.Value = variableSave.Value;                
            }

            // todo:  Handle lists?

        }

    }
}
