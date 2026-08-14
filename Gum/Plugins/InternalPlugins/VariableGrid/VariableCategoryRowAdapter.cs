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
    /// Wraps each member of a category, expanding composite rows (such as the single color row backed by
    /// Red/Green/Blue) into their underlying channels so copy/paste operates on real variables.
    /// </summary>
    public static List<IVariableCategoryRow> CreateRows(IEnumerable<InstanceMember> members)
    {
        List<IVariableCategoryRow> rows = new List<IVariableCategoryRow>();

        foreach (InstanceMember member in members)
        {
            if (member is CompositeInstanceMember composite)
            {
                rows.AddRange(CreateRows(composite.ChannelMembers));
            }
            else
            {
                rows.Add(new InstanceMemberRow(member));
            }
        }

        return rows;
    }

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

        public bool TrySetValue(object? value)
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
        /// The unqualified variable name. A multi-select row is a wrapper with no variable of its own, so
        /// it reports the name of the rows it fans out to (they all share one).
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

            return false;
        }
    }
}
