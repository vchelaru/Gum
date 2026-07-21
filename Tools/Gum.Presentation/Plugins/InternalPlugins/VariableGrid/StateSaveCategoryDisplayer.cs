using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.PropertyGridHelpers;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Relocated from Gum.csproj into headless Gum.Presentation (ADR-0005 Phase 3, part of #3860):
/// builds the category-state "common members" row (one remove-button row per variable shared by
/// every state in a category) and the (currently always-empty - see <see cref="GetCategoriesFor"/>)
/// per-category behavior properties header. Returns <see cref="SyntheticCategoryDescriptor"/>
/// (wrapping <see cref="SyntheticVariableRow"/>) instead of WpfDataUi's <c>MemberCategory</c>/
/// <c>InstanceMember</c> - a WPF-side mapper materializes the real WPF types from these descriptors.
/// </summary>
public class StateSaveCategoryDisplayer
{
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;

    public StateSaveCategoryDisplayer(IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic)
    {
        _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;
    }

    /// <summary>
    /// Builds the "common members" category for <paramref name="stateCategory"/> - one
    /// <see cref="VariableDisplayerKind.RemoveButton"/> row per variable/variable-list shared by
    /// every state in the category - or null when the category has none.
    /// </summary>
    public SyntheticCategoryDescriptor? BuildCommonMembersCategory(InstanceSave? instance, StateSaveCategory stateCategory)
    {
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
                commonMembers.Add(variableList.Name);
            }
        }

        if (!commonMembers.Any())
        {
            return null;
        }

        var descriptor = new SyntheticCategoryDescriptor($"{stateCategory.Name} Variables");

        foreach (var commonMember in commonMembers)
        {
            descriptor.Members.Add(new SyntheticVariableRow
            {
                Name = commonMember,
                ValueType = typeof(string),
                Get = () => commonMember,
                Set = (_) => _variableInCategoryPropagationLogic
                    .AskRemoveVariableFromAllStatesInCategory(commonMember, stateCategory),
                PreferredDisplayerKindOverride = VariableDisplayerKind.RemoveButton
            });
        }

        return descriptor;
    }

    /// <summary>
    /// Preserved as-ported from the original WPF-typed class: always returns a single, permanently
    /// empty "{category.Name} Properties" category - no member is ever added to it. Left unchanged
    /// (behavior-preserving relocation); flagged as likely-dead functionality in the accompanying PR.
    /// </summary>
    public static List<SyntheticCategoryDescriptor> GetCategoriesFor(BehaviorSave behavior, StateSaveCategory category)
    {
        List<SyntheticCategoryDescriptor> categories = new List<SyntheticCategoryDescriptor>();
        categories.Add(new SyntheticCategoryDescriptor($"{category.Name} Properties"));

        return categories;
    }
}
