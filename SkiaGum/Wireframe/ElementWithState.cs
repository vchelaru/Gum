using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.Wireframe
{
    public class ElementWithState
    {
        public ElementSave Element
        {
            get;
            set;
        }

        public string StateName
        {
            get;
            set;
        }

        public Dictionary<string, string> CategorizedStates
        {
            get;
            set;
        }



        public string InstanceName
        {
            get;
            set;
        }

        public ElementWithState(ElementSave elementSave)
        {
            CategorizedStates = new Dictionary<string, string>();
            Element = elementSave;
        }

        public override string ToString()
        {
            if (Element != null)
            {
                return Element.ToString() + " (" + StateName + ")";
            }
            else
            {
                return "NO ELEMENT";
            }
        }

        public DataTypes.Variables.StateSave StateSave
        {
            get
            {
                var toReturn = Element.AllStates.FirstOrDefault(item => item.Name == StateName);
                if (toReturn == null)
                {
                    toReturn = Element.DefaultState;
                }
                return toReturn;
            }
        }

        public IEnumerable<DataTypes.Variables.StateSave> AllStates
        {
            get
            {
                var regular = StateSave;
                if (regular != null)
                {
                    yield return regular;
                }

                foreach (var kvp in CategorizedStates)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        var foundCategory = Element.Categories.FirstOrDefault(item => item.Name == kvp.Key);

                        if (foundCategory != null)
                        {
                            var state = foundCategory.States.FirstOrDefault(item => item.Name == kvp.Value);
                            if (state != null)
                            {
                                yield return state;
                            }
                        }
                    }
                }
            }
        }
    }

    #region Extension Methods

    public static class ElementWithStateExtensionMethods
    {
        public static void Add(this List<ElementWithState> toAddTo, ElementSave elementSave)
        {
            toAddTo.Add(new ElementWithState(elementSave));
        }

        public static void Remove(this List<ElementWithState> toAddTo, ElementSave elementSave)
        {
            toAddTo.Remove(toAddTo.FirstOrDefault(item => item.Element == elementSave));
        }
    }

    #endregion
}
