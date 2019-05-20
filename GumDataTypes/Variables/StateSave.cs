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

#if !UWP
        [Browsable(false)]
#endif
        public string Name
        {
            get;
            set;
        }

#if !UWP

        [Browsable(false)]
#endif
        [XmlElement("Variable")]
        public List<VariableSave> Variables
        {
            get;
            set;
        }

#if !UWP
        [Browsable(false)]
#endif
        [XmlElement("VariableList")]
        public List<VariableListSave> VariableLists
        {
            get;
            set;
        }

#if !UWP
        [Browsable(false)]
#endif
        [XmlIgnore]
        public ElementSave ParentContainer
        {
            get;
            set;
        }

        // If adding here, modify Clone

        #endregion

        #region Methods

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
            for (int i = 0; i < Variables.Count; i++ )
            {
                var variable = this.Variables[i];

                toReturn.Variables.Add(variable.Clone());
            }

            for (int i = 0; i < this.VariableLists.Count; i++ )
            {
                toReturn.VariableLists.Add(VariableLists[i].Clone());
            }

            toReturn.ParentContainer = this.ParentContainer;

            return toReturn;
        }

        public VariableSave GetVariableSave(string variableName)
        {
            return Variables.FirstOrDefault(v => v.Name == variableName || v.ExposedAsName == variableName);
        }

        public VariableListSave GetVariableListSave(string variableName)
        {
            return VariableLists.FirstOrDefault(list => list.Name == variableName);
        }

        public bool TryGetValue<T>(string variableName, out T result)
        {
            object value = GetValue(variableName);
            bool toReturn = false;

            if (value != null && value is T)
            {
                result = (T)value;
                value = true;
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
            ////////////////////Early Out////////////////
            if (ParentContainer == null)
            {
                return null;
            }
            //////////////////End Early Out//////////////

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

            if (ToolsUtilities.StringFunctions.ContainsNoAlloc(variableName, '.'))
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

        public VariableSave SetValue(string variableName, object valueToSet, string variableType)
        {
            VariableSave variableSave = GetVariableSave(variableName);

            if(variableSave == null)
            {
                variableSave = new VariableSave();
                variableSave.Name = variableName;
                variableSave.Type = variableType;
                this.Variables.Add(variableSave);
            }

            variableSave.Value = valueToSet;
            variableSave.SetsValue = true;

            return variableSave;
        }

        public override string ToString()
        {
            return this.Name + " in " + ParentContainer;
        }

        #endregion

    }
}
