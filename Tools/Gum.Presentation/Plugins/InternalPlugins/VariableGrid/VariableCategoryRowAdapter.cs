using System;
using System.Collections.Generic;
using System.Linq;
using Gum.PropertyGridHelpers;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <inheritdoc cref="IVariableCategoryRowAdapter"/>
public class VariableCategoryRowAdapter : IVariableCategoryRowAdapter
{
    /// <inheritdoc/>
    /// <remarks>
    /// Rows are taken exactly as the grid presents them, including composite rows (the color swatch backed
    /// by Red/Green/Blue) and multi-select wrappers. Expanding a composite into its channels here would
    /// name it differently depending on whether one or several objects are selected - the multi-select
    /// wrapper sits above the composite, not below it - so a copy in one mode would not match a paste in
    /// the other. Writing through the row as-is also keeps each wrapper's own behavior: the composite skips
    /// unchanged channels, and the multi-select wrapper fans the value out to every selected instance.
    /// </remarks>
    public List<IVariableCategoryRow> CreateRows(IEnumerable<InstanceMember> members) =>
        members.Select(member => (IVariableCategoryRow)new InstanceMemberRow(member)).ToList();

    private class InstanceMemberRow : IVariableCategoryRow
    {
        private readonly InstanceMember _member;

        public InstanceMemberRow(InstanceMember member)
        {
            _member = member;
        }

        public string RootVariableName => GetRootVariableName(_member);

        // No recursion needed: DataUiGrid.TryCreateMultiGroup forces IsReadOnly onto multi-select wrappers
        // (Any of the wrapped rows), and CompositeInstanceMember overrides it over its channels.
        public bool IsReadOnly => _member.IsReadOnly;

        public bool IsAssignedByReference => GetIsAssignedByReference(_member);

        public bool IsIndeterminate => _member.IsIndeterminate;

        public object? Value => _member.Value;

        public Type? ValueType
        {
            get
            {
                // PropertyType throws when the member has neither an instance to reflect over nor a custom
                // type event. IsDefined is true only when one of those two is available.
                if (!_member.IsDefined)
                {
                    return null;
                }
                return _member.PropertyType;
            }
        }

        public bool TrySetValue(object value)
        {
            // A state name is only meaningful on the element that declares it. Writing one the target does
            // not have would pass the CLR type gate (it is just a string) and save a dangling state name
            // into the project, so validate against the row's available states.
            if (GetIsStateSelection(_member) && _member.CustomOptions?.Contains(value) != true)
            {
                return false;
            }

            // No CallAfterSetByUi here: nothing subscribes AfterSetByUi, and the category-level handler it
            // reaches (DataUiGrid.HandleInstanceMemberSetByUi) re-reads every row in the whole grid - per
            // pasted value - only for the caller's final full refresh to redo that work anyway.
            return _member.SetValue(value, SetPropertyCommitType.Full) == ApplyValueResult.Success;
        }

        /// <summary>
        /// The name a copied value is matched by. A multi-select row is a wrapper with no variable of its
        /// own, so it reports the name of the rows it fans out to (they all share one).
        /// </summary>
        private static string GetRootVariableName(InstanceMember member)
        {
            if (member is StateReferencingInstanceMember stateReferencingMember)
            {
                return stateReferencingMember.RootVariableName;
            }

            if (member is MultiSelectInstanceMember multiSelectMember && multiSelectMember.InstanceMembers.Count > 0)
            {
                return GetRootVariableName(multiSelectMember.InstanceMembers[0]);
            }

            // Composite rows fall through to here. Their name is the composite's own (for example "Color"),
            // which is stable across single- and multi-select because both build it the same way.
            return member.Name;
        }

        private static bool GetIsStateSelection(InstanceMember member)
        {
            if (member is StateReferencingInstanceMember stateReferencingMember)
            {
                return stateReferencingMember.IsStateSelection;
            }

            if (member is MultiSelectInstanceMember multiSelectMember && multiSelectMember.InstanceMembers.Count > 0)
            {
                return GetIsStateSelection(multiSelectMember.InstanceMembers[0]);
            }

            return false;
        }

        private static bool GetIsAssignedByReference(InstanceMember member)
        {
            if (member is StateReferencingInstanceMember stateReferencingMember)
            {
                return stateReferencingMember.IsAssignedByReference;
            }

            if (member is MultiSelectInstanceMember multiSelectMember)
            {
                return multiSelectMember.InstanceMembers.Any(GetIsAssignedByReference);
            }

            if (member is CompositeInstanceMember compositeMember)
            {
                return compositeMember.ChannelMembers.Any(GetIsAssignedByReference);
            }

            return false;
        }
    }
}
