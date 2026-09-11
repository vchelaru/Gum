using System;
using System.Collections.Generic;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace WpfDataUi;

/// <summary>
/// What code driving a data grid needs from it, independent of the UI framework. The WPF and
/// Avalonia <c>DataUiGrid</c> controls implement it over a <see cref="DataUiGridModel"/>.
/// </summary>
public interface IDataUiGrid
{
    /// <summary>The object whose members are shown; see <see cref="DataUiGridModel.Instance"/>.</summary>
    object? Instance { get; set; }

    /// <summary>Whether the user can edit the grid.</summary>
    bool IsEnabled { get; set; }

    /// <summary>The categories currently shown.</summary>
    BulkObservableCollection<MemberCategory> Categories { get; }

    /// <summary>Raised when a member is set by the UI.</summary>
    event Action<string, PropertyChangedArgs>? PropertyChange;

    /// <inheritdoc cref="DataUiGridModel.SetCategories"/>
    void SetCategories(IList<MemberCategory> newCategories);

    /// <inheritdoc cref="DataUiGridModel.SetMultipleCategoryLists"/>
    void SetMultipleCategoryLists(List<List<MemberCategory>> listOfCategoryLists);

    /// <inheritdoc cref="DataUiGridModel.ApplyMemberFilter"/>
    void ApplyMemberFilter(Func<InstanceMember, bool>? isMatch);

    /// <inheritdoc cref="DataUiGridModel.GetInstanceMember"/>
    InstanceMember? GetInstanceMember(string memberName);

    /// <inheritdoc cref="DataUiGridModel.InsertSpacesInCamelCaseMemberNames"/>
    void InsertSpacesInCamelCaseMemberNames();

    /// <summary>Re-reads every shown value from its member without rebuilding the rows.</summary>
    void Refresh();
}
