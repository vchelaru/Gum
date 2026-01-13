using Gum.DataTypes.Behaviors;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.Services;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.VariableGrid;

internal static class BehaviorShowingLogic
{
    private static readonly IFileCommands _fileCommands = Locator.GetRequiredService<IFileCommands>();
    public static List<MemberCategory> GetCategoriesFor(BehaviorSave behavior)
    {
        List<MemberCategory> toReturn = new List<MemberCategory>();

        var category = new MemberCategory();
        toReturn.Add(category);

        var defaultImplementationMember = AddMemberFor<string>(nameof(BehaviorSave.DefaultImplementation),
            () => behavior.DefaultImplementation,
            (newValue) => behavior.DefaultImplementation = (string)newValue);
        defaultImplementationMember.DetailText = "Code generation is required for this to work at runtime";

        return toReturn;
        
        InstanceMember AddMemberFor<T>(string name, Func<object> getter, Action<object> setter)
        {
            var instanceMember = new InstanceMember();

            instanceMember.Name = name;
            instanceMember.CustomGetEvent += (notUsed) => getter();
            instanceMember.CustomSetPropertyEvent += (sender, args) =>
            {
                setter(args.Value);

                _fileCommands.TryAutoSaveBehavior(behavior);
            };

            var componentsImplementingBehavior = GumState.Self.ProjectState.GumProjectSave.Components
                .Where(item => item.Behaviors.Any(behaviorSave => behaviorSave.BehaviorName == behavior.Name));

            var options = componentsImplementingBehavior
                .Select(item => (object)item.Name).ToList();

            options.Insert(0, null);
            instanceMember.CustomOptions = options;
            instanceMember.CustomGetTypeEvent += (notused) => typeof(T);

            category.Members.Add(instanceMember);
            return instanceMember;
        }
    }
}
