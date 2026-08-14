using System;
using System.Collections.Generic;
using System.Linq;
using Gum.PropertyGridHelpers;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Adapts the Variables tab's WPF rows to <see cref="IVariableCategoryRow"/> so the headless
/// <see cref="VariableCategoryCopyPasteService"/> can read and write them without referencing WPF.
/// </summary>
public static class VariableCategoryRowAdapter
{
    /// <summary>
    /// Wraps each member of a category as a copy/paste row.
    /// </summary>
    /// <remarks>
    /// Rows are taken exactly as the grid presents them, including composite rows (the color swatch backed
    /// by Red/Green/Blue) and multi-select wrappers. Expanding a composite into its channels here would
    /// name it differently depending on whether one or several objects are selected - the multi-select
    /// wrapper sits above the composite, not below it - so a copy in one mode would not match a paste in
    /// the other. Writing through the row as-is also keeps each wrapper's own behavior: the composite skips
    /// unchanged channels, and the multi-select wrapper fans the value out and refreshes afterwards.
    /// </remarks>
    public static List<IVariableCategoryRow> CreateRows(IEnumerable<InstanceMember> members) =>
        members.Select(member => (IVariableCategoryRow)new InstanceMemberRow(member)).ToList();

    private class InstanceMemberRow : IVariableCategoryRow
    {
        private readonly InstanceMember _member;

        public InstanceMemberRow(InstanceMember member)
        {
            _member = member;
        }

        public string RootVariableName => GetRootVariableName(_member);

        public bool IsReadOnly => _member.IsReadOnly;

        public bool IsAssignedByReference => GetIsAssignedByReference(_member);

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
            ApplyValueResult result = _member.SetValue(value, SetPropertyCommitType.Full);

            if (result != ApplyValueResult.Success)
            {
                return false;
            }

            _member.CallAfterSetByUi();
            return true;
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
