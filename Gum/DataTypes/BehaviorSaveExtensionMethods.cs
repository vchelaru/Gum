using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System.Linq;
using ToolsUtilities;

namespace Gum.DataTypes
{
    public static class BehaviorSaveExtensionMethods
    {

        public static bool Initialize(this BehaviorSave behaviorSave)
        {
            bool wasModified = false;
            foreach (StateSave state in behaviorSave.AllStates)
            {
                state.ParentContainer = null;
                state.Initialize();

                FixStateVariableTypes(state, ref wasModified);
            }

            return wasModified;
        }

        private static void FixStateVariableTypes(StateSave state, ref bool wasModified)
        {
            foreach (var variable in state.Variables.Where(item => item.Type == "string" && item.Name.Contains("State")))
            {
                string name = variable.Name;

                var withoutState = name.Substring(0, name.Length - "State".Length);
                if (variable.Name == "State")
                {
                    variable.Type = "State";
                    wasModified = true;
                }
            }
        }

#if GUM
        public static FilePath GetFullPathXmlFile(this BehaviorSave behaviorSave)
        {
            return behaviorSave.GetFullPathXmlFile(behaviorSave.Name);
        }

        public static string GetFullPathXmlFile(this BehaviorSave behaviorSave, string behaviorName)
        {
            if (string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                return null;
            }

            string directory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

            return directory + BehaviorReference.Subfolder + "\\" + behaviorName + "." + BehaviorReference.Extension;
        }


#endif
    }
}
