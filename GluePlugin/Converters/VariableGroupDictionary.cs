using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GluePlugin.Converters
{
    public class GlueVariableData
    {
        public InstructionSave InstructionSave;
        public NamedObjectSave NamedObjectSave;
        public string RootName;
    }

    public class VariableGroupDictionary
    {
        Dictionary<string, List<GlueVariableData>> variableDictionary =
            new Dictionary<string, List<GlueVariableData>>();
        
        public void AddVariable(InstructionSave instructionSave, 
            NamedObjectSave namedObjectSave, string rootName, string category)
        {
            if(variableDictionary.ContainsKey(category) == false)
            {
                variableDictionary[category] = new List<GlueVariableData>();
            }

            var data = new GlueVariableData();
            data.InstructionSave = instructionSave;
            data.NamedObjectSave = namedObjectSave;
            data.RootName = rootName;
            variableDictionary[category].Add(data);
        }

        public bool HasCategory(string category)
        {
            return variableDictionary.ContainsKey(category);
        }

        public IReadOnlyCollection<GlueVariableData> GetVariablesInCategory(string category)
        {
            return variableDictionary[category].ToArray();
        }
    }
}
