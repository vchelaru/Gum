using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using System.Collections.Generic;
using System.Linq;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid
{
    internal class StateSaveCategoryDisplayer
    {
        public static void DisplayMembersForCategoryInElement(InstanceSave instance, List<MemberCategory> categories, StateSaveCategory stateCategory)
        {
            // todo - inject this 
            var _variableInCategoryPropagationLogic = 
                Locator.GetRequiredService<VariableInCategoryPropagationLogic>();

            categories.Clear();

            List<string> commonMembers = new List<string>();

            var firstState = stateCategory.States.FirstOrDefault();

            if (firstState != null)
            {
                foreach (var variable in firstState.Variables)
                {
                    bool canAdd = variable.ExcludeFromInstances == false || instance == null;

                    if (canAdd)
                    {
                        commonMembers.Add(variable.Name);
                    }
                }

                foreach (var variableList in firstState.VariableLists)
                {
                    bool canAdd = true;

                    if (canAdd)
                    {
                        commonMembers.Add(variableList.Name);
                    }
                }
            }

            if (commonMembers.Any())
            {
                var memberCategory = new MemberCategory();
                memberCategory.Name = $"{stateCategory.Name} Variables";
                categories.Add(memberCategory);

                foreach (var commonMember in commonMembers)
                {
                    var instanceMember = new InstanceMember();

                    instanceMember.Name = commonMember;
                    instanceMember.CustomGetTypeEvent += (member) => typeof(string);
                    instanceMember.CustomGetEvent += (member) => commonMember;
                    instanceMember.CustomSetEvent += (not, used) =>
                    {
                        _variableInCategoryPropagationLogic
                            .AskRemoveVariableFromAllStatesInCategory(commonMember, stateCategory);
                    };

                    instanceMember.PreferredDisplayer = typeof(VariableRemoveButton);

                    memberCategory.Members.Add(instanceMember);
                }
            }
        }

        public static List<MemberCategory> GetCategoriesFor(BehaviorSave behavior, StateSaveCategory category)
        {
            List<MemberCategory> memberCategories = new List<MemberCategory>();
            var memberCategory = new MemberCategory($"{category.Name} Properties");
            memberCategories.Add(memberCategory);


            return memberCategories;
        }
    }
}
