using Gum.DataTypes.Behaviors;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.VariableGrid
{
    internal static class BehaviorShowingLogic
    {
        public static List<MemberCategory> GetCategoriesFor(BehaviorSave behavior)
        {
            List<MemberCategory> toReturn = new List<MemberCategory>();

            var category = new MemberCategory();
            toReturn.Add(category);

            AddMemberFor<string>(nameof(BehaviorSave.DefaultImplementation),
                () => behavior.DefaultImplementation,
                (newValue) => behavior.DefaultImplementation = (string)newValue);

            
            return toReturn;
            
            void AddMemberFor<T>(string name, Func<object> getter, Action<object> setter)
            {
                var instanceMember = new InstanceMember();

                instanceMember.Name = name;
                instanceMember.CustomGetEvent += (notUsed) => getter();
                instanceMember.CustomSetEvent += (instance, value) =>
                {
                    setter(value);

                    GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);
                };
                var options = 
                    GumState.Self.ProjectState.GumProjectSave.Components.Select(item => (object)item.Name).ToList();
                options.Insert(0, null);
                instanceMember.CustomOptions = options;
                instanceMember.CustomGetTypeEvent += (notused) => typeof(T);

                category.Members.Add(instanceMember);
            }
        }

    }
}
