using System;
using System.Collections.Generic;
using WpfDataUi;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace Gum.Presentation.Tests;

/// <summary>An <see cref="IDataUiGrid"/> over a bare model, as either head's grid is.</summary>
internal sealed class ModelBackedGrid : IDataUiGrid
{
    public DataUiGridModel Model { get; } = new DataUiGridModel();
    public int RefreshCount { get; private set; }
    public object? Instance { get => Model.Instance; set => Model.Instance = value; }
    public bool IsEnabled { get; set; } = true;
    public BulkObservableCollection<MemberCategory> Categories => Model.Categories;
    public event Action<string, PropertyChangedArgs>? PropertyChange
    {
        add => Model.PropertyChange += value;
        remove => Model.PropertyChange -= value;
    }
    public void SetCategories(IList<MemberCategory> newCategories) => Model.SetCategories(newCategories);
    public void SetMultipleCategoryLists(List<List<MemberCategory>> listOfCategoryLists) => Model.SetMultipleCategoryLists(listOfCategoryLists);
    public void ApplyMemberFilter(Func<InstanceMember, bool>? isMatch) => Model.ApplyMemberFilter(isMatch);
    public InstanceMember? GetInstanceMember(string memberName) => Model.GetInstanceMember(memberName);
    public void InsertSpacesInCamelCaseMemberNames() => Model.InsertSpacesInCamelCaseMemberNames();
    public void Refresh() => RefreshCount++;
}
