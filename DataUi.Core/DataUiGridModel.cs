using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace WpfDataUi;

/// <summary>
/// The framework-neutral state and logic of a data grid: the categories it shows, the members it
/// reflects off <see cref="Instance"/>, delegate-driven optional visibility, the member filter,
/// category expansion memory, and multi-select grouping. The WPF and Avalonia <c>DataUiGrid</c>
/// controls each own one of these and only render it.
/// </summary>
public class DataUiGridModel
{
    #region Fields

    // Some members are optionally visible based off of a delegate. These are stored so the
    // delegate can be re-evaluated every time a member changes, as the member may be based off of
    // the current state of the instance.
    private readonly Dictionary<InstanceMember, Func<InstanceMember, bool>> _membersWithOptionalVisibility;

    private readonly MemberCategoryFilter _memberFilter;

    private Func<InstanceMember, bool>? _memberFilterPredicate;

    private object? _instance;

    // Shared by every grid in the process and keyed only by category name, so a category keeps the
    // user's expansion choice across selections (see the gum-tool-variable-grid skill).
    private static readonly Dictionary<string, bool> _expansionStates = new();

    #endregion

    #region Properties

    /// <summary>
    /// The displayed instance. Setting it re-populates <see cref="Categories"/> by reflection when
    /// <see cref="IsAutoPopulateCategoriesEnabled"/> is true, so changes made directly to
    /// Categories, or applied through <see cref="Apply"/>, only persist until it is set again.
    /// </summary>
    public object? Instance
    {
        get => _instance;
        set
        {
            if (Equals(_instance, value))
            {
                return;
            }

            object? oldValue = _instance;
            _instance = value;
            HandleInstanceChanged(oldValue);
        }
    }

    /// <summary>Whether setting <see cref="Instance"/> reflects its public fields and properties into rows.</summary>
    public bool IsAutoPopulateCategoriesEnabled { get; set; }

    public ObservableCollection<Type> TypesToIgnore { get; }

    public ObservableCollection<string> MembersToIgnore { get; }

    public BulkObservableCollection<MemberCategory> Categories { get; }

    #endregion

    #region Events

    public event Action<string, BeforePropertyChangedArgs>? BeforePropertyChange;

    /// <summary>
    /// Raised whenever an instance member is set by the UI, such as the user typing a value in a text box.
    /// </summary>
    public event Action<string, PropertyChangedArgs>? PropertyChange;

    #endregion

    #region Constructor

    public DataUiGridModel()
    {
        _membersWithOptionalVisibility = new Dictionary<InstanceMember, Func<InstanceMember, bool>>();
        _memberFilter = new MemberCategoryFilter();
        IsAutoPopulateCategoriesEnabled = true;
        TypesToIgnore = new ObservableCollection<Type>();
        MembersToIgnore = new ObservableCollection<string>();
        Categories = new BulkObservableCollection<MemberCategory>();

        Categories.CollectionChanged += HandleCategoriesChanged;
        TypesToIgnore.CollectionChanged += (_, _) => PopulateCategories();
        MembersToIgnore.CollectionChanged += HandleMembersToIgnoreChanged;
    }

    #endregion

    #region Instance

    private void HandleInstanceChanged(object? oldValue)
    {
        if (oldValue is INotifyPropertyChanged oldNpc)
        {
            oldNpc.PropertyChanged -= HandleInstancePropertyChanged;
        }

        _membersWithOptionalVisibility.Clear();

        if (IsAutoPopulateCategoriesEnabled)
        {
            PopulateCategories();
        }

        if (_instance is INotifyPropertyChanged newNpc)
        {
            newNpc.PropertyChanged += HandleInstancePropertyChanged;
        }
    }

    private void HandleInstancePropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        RefreshDelegateBasedElementVisibility();

    #endregion

    #region Categories

    private void HandleCategoriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // Every path that replaces the collection wholesale lands here, both SetCategories and
            // PopulateCategories, so this is where the filter's remembered rows stop describing
            // anything on screen. Invalidating per caller instead would leave the snapshot pinning
            // discarded categories whenever a new caller forgot to do it.
            _memberFilter.Invalidate();
            return; // subscriptions are managed manually by SetCategories when Reset is fired
        }

        Subscribe(e.NewItems);
        Unsubscribe(e.OldItems);

        // Categories are also swapped in one at a time rather than wholesale (a selection change
        // reconciles them in place), and a category arriving that way brings its full member list.
        _memberFilter.Apply(Categories, _memberFilterPredicate);
    }

    private void Subscribe(IList? newItems)
    {
        if (newItems == null) return;
        foreach (MemberCategory category in newItems)
        {
            category.MemberValueChangedByUi += HandleCategoryMemberChanged;
        }
    }

    private void Unsubscribe(IList? oldItems)
    {
        if (oldItems == null) return;
        foreach (MemberCategory category in oldItems)
        {
            category.MemberValueChangedByUi -= HandleCategoryMemberChanged;
        }
    }

    /// <summary>
    /// Replaces all categories at once, firing a single Reset notification instead of one
    /// notification per category. This is faster than calling Categories.Clear() followed
    /// by individual Categories.Add() calls when rebuilding the grid.
    /// </summary>
    public void SetCategories(IList<MemberCategory> newCategories)
    {
        // Runs before the replacement below, whose Reset drops the filter snapshot this reads from.
        StoreExpandedStates();

        foreach (MemberCategory category in newCategories)
        {
            if (_expansionStates.TryGetValue(category.Name, out bool expanded))
            {
                category.IsExpanded = expanded;
            }
        }
        Unsubscribe(Categories);
        Categories.ReplaceAll(newCategories);
        Subscribe((IList)newCategories);

        // Selecting a different object rebuilds the grid, which would otherwise silently drop a filter
        // the box still shows as active.
        _memberFilter.Apply(Categories, _memberFilterPredicate);
    }

    /// <summary>
    /// Shows only the members <paramref name="isMatch"/> accepts, expanding whichever categories hold
    /// them. Passing null clears the filter and restores every category's members, their order, and the
    /// expansion state the user had chosen. The predicate is remembered and re-applied whenever the grid
    /// rebuilds its categories.
    /// </summary>
    public void ApplyMemberFilter(Func<InstanceMember, bool>? isMatch)
    {
        _memberFilterPredicate = isMatch;
        _memberFilter.Apply(Categories, isMatch);
    }

    private void StoreExpandedStates()
    {
        foreach (var item in Categories)
        {
            // A filter force-expands the categories holding its matches. Persisting that would let a
            // search the user has already dismissed reorganize the grid for every later selection.
            _expansionStates[item.Name] = _memberFilter.GetPreFilterIsExpanded(item);
        }
    }

    private void HandleCategoryMemberChanged(InstanceMember member)
    {
        HandleInstanceMemberSetByUi(member);
    }

    #endregion

    #region Display properties

    public void Apply(TypeMemberDisplayProperties properties)
    {
        foreach (var property in properties.DisplayProperties)
        {
            bool found = TryGetInstanceMember(property.Name, out InstanceMember? member, out MemberCategory? category);

            if (found && member != null && category != null)
            {
                ApplyDisplayPropertyToInstanceMember(property, member, category);
            }
        }

        RefreshDelegateBasedElementVisibility();
    }

    public void IgnoreAllMembers()
    {
        if (this.Instance == null)
        {
            throw new InvalidOperationException("The Instance must be set before calling this");
        }
        else
        {
            Type type = Instance.GetType();

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                MembersToIgnore.Add(field.Name);
            }
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                MembersToIgnore.Add(property.Name);
            }
        }
    }

    private void RefreshDelegateBasedElementVisibility()
    {
        foreach (var kvp in _membersWithOptionalVisibility.ToList())
        {
            var member = kvp.Key;
            var category = member.Category;
            if (category == null)
            {
                continue;
            }
            bool shouldBeVisible = !kvp.Value(member);
            bool isVisible = category.Members.Contains(member);

            if (isVisible && !shouldBeVisible)
                category.Members.Remove(member);
            else if (!isVisible && shouldBeVisible)
                category.Members.Add(member);
        }
    }

    private void ApplyDisplayPropertyToInstanceMember(InstanceMemberDisplayProperties displayProperties, InstanceMember member, MemberCategory category)
    {
        if (displayProperties.IsHiddenDelegate != null && _membersWithOptionalVisibility.ContainsKey(member) == false)
        {
            _membersWithOptionalVisibility.Add(member, displayProperties.IsHiddenDelegate);
        }

        // Only the static IsHidden flag hides here; the delegates are applied afterward.
        if (displayProperties.IsHidden)
        {
            category.Members.Remove(member);
        }
        else
        {
            if (member.PreferredDisplayer != displayProperties.PreferredDisplayer)
            {
                member.PreferredDisplayer = displayProperties.PreferredDisplayer;
            }
            member.DisplayName = displayProperties.DisplayName;
            if (!string.IsNullOrEmpty(displayProperties.Category) && category.Name != displayProperties.Category)
            {
                category.Members.Remove(member);

                MemberCategory newCategory = GetOrInstantiateAndAddMemberCategory(displayProperties.Category);
                member.Category = newCategory;
                newCategory.Members.Add(member);
            }
        }
    }

    public bool TryGetInstanceMember(string name, out InstanceMember? member, out MemberCategory? category)
    {
        member = null;
        category = null;

        foreach (var possibleCategory in this.Categories)
        {
            if (member != null)
            {
                break;
            }
            foreach (var possibleMember in possibleCategory.Members)
            {
                if (possibleMember.Name == name)
                {
                    member = possibleMember;
                    category = possibleCategory;
                    break;
                }
            }
        }
        return member != null;
    }

    /// <summary>The first shown member named <paramref name="memberName"/>, or null.</summary>
    public InstanceMember? GetInstanceMember(string memberName)
    {
        if (TryGetInstanceMember(memberName, out InstanceMember? member, out MemberCategory? _))
        {
            return member;
        }
        return null;
    }

    public void MoveMemberToCategory(string memberName, string categoryName)
    {
        var member = Categories.SelectMany(item => item.Members).FirstOrDefault(item => item.Name == memberName);
        var desiredCategory = Categories.FirstOrDefault(item => item.Name == categoryName);

        if (desiredCategory == null)
        {
            desiredCategory = new MemberCategory(categoryName);
            Categories.Add(desiredCategory);
        }

        if (member != null && member.Category != desiredCategory)
        {
            member.Category?.Members.Remove(member);
            desiredCategory.Members.Add(member);
        }
    }

    #endregion

    #region Reflection population

    private void HandleMembersToIgnoreChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Rebuilding would wipe custom categories, so adds and removes are applied in place.
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:

                List<string> newItems = new();
                foreach (var item in e.NewItems!)
                {
                    newItems.Add((string)item);
                }

                foreach (var category in this.Categories)
                {
                    // ignore was added, so try to remove it:
                    for (int i = category.Members.Count - 1; i > -1; i--)
                    {
                        if (newItems.Contains(category.Members[i].Name))
                        {
                            category.Members.RemoveAt(i);
                        }
                    }

                }
                break;
            case NotifyCollectionChangedAction.Remove:

                List<string> oldItems = new();
                foreach (var item in e.OldItems!)
                {
                    oldItems.Add((string)item);
                }

                if (Instance != null)
                {
                    Type type = Instance.GetType();

                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (oldItems.Contains(field.Name))
                        {
                            TryCreateCategoryAndInstanceFor(field);
                        }
                    }
                    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (oldItems.Contains(property.Name))
                        {
                            TryCreateCategoryAndInstanceFor(property);
                        }
                    }
                }
                break;
            default:
                // A destructive rebuild that drops previously-added custom members; kept as the
                // fallback for collection changes other than add and remove.
                PopulateCategories();
                break;
        }
    }

    private void PopulateCategories()
    {
        StoreExpandedStates();

        this.Categories.Clear();

        if (Instance != null)
        {
            Type type = Instance.GetType();

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                TryCreateCategoryAndInstanceFor(field);
            }
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                TryCreateCategoryAndInstanceFor(property);
            }
        }

    }

    private void TryCreateCategoryAndInstanceFor(MemberInfo memberInfo)
    {
        if (ShouldCreateUiFor(memberInfo.GetMemberType(), memberInfo.Name))
        {

            string categoryName = GetCategoryAttributeFor(memberInfo);

            MemberCategory memberCategory = GetOrInstantiateAndAddMemberCategory(categoryName);

            InstanceMember newMember = new InstanceMember(memberInfo.Name, Instance!);
            AssignInstanceMemberEvents(newMember);
            newMember.Category = memberCategory;
            memberCategory.Members.Add(newMember);
        }
    }

    private void AssignInstanceMemberEvents(InstanceMember newMember)
    {
        // AfterSetByUi is not subscribed here because this can run before custom members are added;
        // MemberCategory.MemberValueChangedByUi covers every member instead.
        newMember.BeforeSetByUi += HandleInstanceMemberBeforeSetByUi;
    }

    private void HandleInstanceMemberBeforeSetByUi(object? sender, EventArgs e)
    {
        if (BeforePropertyChange != null && sender is InstanceMember senderMember)
        {
            BeforePropertyChangedArgs args = (BeforePropertyChangedArgs)e;
            args.Owner = this.Instance!;
            args.OldValue = senderMember.Value!;
            args.PropertyName = senderMember.Name;

            BeforePropertyChange(senderMember.Name, args);
        }
    }

    private void HandleInstanceMemberSetByUi(InstanceMember senderInstanceMember)
    {
        if (PropertyChange != null)
        {
            PropertyChangedArgs args = new PropertyChangedArgs();
            args.Owner = this.Instance!;
            args.OldValue = senderInstanceMember.OldValue;
            args.NewValue = senderInstanceMember.Value!;
            args.PropertyName = senderInstanceMember.Name;

            PropertyChange(senderInstanceMember.Name, args);
        }
        foreach (MemberCategory memberCategory in Categories)
        {
            foreach (var instanceMember in memberCategory.Members)
            {
                if (instanceMember.Name != senderInstanceMember.Name)
                {
                    instanceMember.SimulateValueChanged();
                }
            }
        }

        RefreshDelegateBasedElementVisibility();
    }

    private MemberCategory GetOrInstantiateAndAddMemberCategory(string categoryName)
    {
        MemberCategory? memberCategory = Categories.FirstOrDefault(item => item.Name == categoryName);
        if (memberCategory == null)
        {
            memberCategory = new MemberCategory(categoryName);
            Categories.Add(memberCategory);
        }
        return memberCategory;
    }

    private bool ShouldCreateUiFor(Type type, string memberName)
    {
        if (TypesToIgnore.Contains(type))
        {
            return false;
        }

        if (MembersToIgnore.Contains(memberName))
        {
            return false;
        }

        if (typeof(Delegate).IsAssignableFrom(type))
        {
            return false;
        }

        return true;
    }

    private static string GetCategoryAttributeFor(MemberInfo memberInfo)
    {
        var attributes = memberInfo.GetCustomAttributes(typeof(CategoryAttribute), true);

        string category = "Uncategorized";

        if (attributes != null && attributes.Length != 0 && attributes[0] is CategoryAttribute attribute)
        {
            category = attribute.Category;
        }
        return category;
    }

    #endregion

    #region Values and names

    /// <summary>
    /// Raises a value change on every shown member, so bound displayers re-read. Views that can
    /// reach their displayers refresh those directly and use this for the rest.
    /// </summary>
    public void SimulateValueChangedOnAllMembers()
    {
        foreach (MemberCategory category in Categories)
        {
            foreach (InstanceMember member in category.Members)
            {
                member.SimulateValueChanged();
            }
        }
    }

    /// <summary>Turns "CamelCase" display names into "Camel Case".</summary>
    public void InsertSpacesInCamelCaseMemberNames()
    {
        foreach (var category in Categories)
        {
            foreach (var member in category.Members)
            {
                if (string.IsNullOrEmpty(member.DisplayName))
                {
                    throw new Exception("This member does not have a display name, so it cannot have camel cases inserted");
                }
                member.DisplayName = DataUiText.InsertSpacesInCamelCase(member.DisplayName);
            }
        }
    }

    #endregion

    #region Multi-select

    /// <summary>
    /// Shows several objects' categories as one grid: members that share a display name across the
    /// lists become one <see cref="MultiSelectInstanceMember"/> that edits them all. Only a category's
    /// <see cref="MemberCategory.Name"/> and <see cref="MemberCategory.HeaderColor"/> carry over.
    /// </summary>
    public void SetMultipleCategoryLists(List<List<MemberCategory>> listOfCategoryLists)
    {
        Dictionary<string, InstanceMember> alreadyAddedMembers = new();

        List<MemberCategory> effectiveCategory = new();

        foreach (var instance in listOfCategoryLists)
        {
            foreach (var category in instance)
            {
                var currentCategory = effectiveCategory.FirstOrDefault(existing => existing.Name == category.Name);
                if(currentCategory == null)
                {
                    currentCategory = new MemberCategory();
                    currentCategory.Name = category.Name;
                    currentCategory.HeaderColor = category.HeaderColor;
                    effectiveCategory.Add(currentCategory);
                }

                foreach (var member in category.Members)
                {
                    var isExisting = alreadyAddedMembers.TryGetValue(member.DisplayName, out var foundMember);
                    if (!isExisting)
                    {
                        alreadyAddedMembers.Add(member.DisplayName, member);

                        var multiSelectInstanceMember = TryCreateMultiGroup(listOfCategoryLists, member);
                        if (multiSelectInstanceMember != null)
                        {
                            currentCategory.Members.Add(multiSelectInstanceMember);
                        }
                    }
                }
            }
        }

        SetCategories(effectiveCategory);
    }

    private MultiSelectInstanceMember? TryCreateMultiGroup(List<List<MemberCategory>> source, InstanceMember templateMember)
    {
        List<InstanceMember> membersToAdd = new();
        foreach (var categoryList in source)
        {
            foreach (var category in categoryList)
            {
                membersToAdd.AddRange(category.Members.Where(item => item.DisplayName == templateMember.DisplayName));
            }
        }

        var shouldExclude = GetIfShouldExclude(membersToAdd);

        if (!shouldExclude)
        {
            var multiSelectInstanceMember = new MultiSelectInstanceMember();
            multiSelectInstanceMember.Name = templateMember.Name;
            multiSelectInstanceMember.DisplayName = templateMember.DisplayName;
            multiSelectInstanceMember.PreferredDisplayer = templateMember.PreferredDisplayer;
            multiSelectInstanceMember.InstanceMembers = membersToAdd;
            multiSelectInstanceMember.IsReadOnly = membersToAdd.Any(item => item.IsReadOnly);
            return multiSelectInstanceMember;
        }
        else
        {
            return null;
        }
    }

    private bool GetIfShouldExclude(List<InstanceMember> membersToAdd)
    {
        var shouldExcludeFromCustomOptions = false;
        // They're all null
        if (membersToAdd.All(item => item.CustomOptions == null))
        {
            shouldExcludeFromCustomOptions = false;
        }
        // They all have none
        else if (membersToAdd.All(item => item.CustomOptions?.Count == 0))
        {
            shouldExcludeFromCustomOptions = false;
        }
        else if (membersToAdd.Any(item => item.CustomOptions == null || item.CustomOptions.Count == 0))
        {
            shouldExcludeFromCustomOptions = true;
        }
        else
        {
            // none are null or have 0 items,
            var firstCustomOptions = membersToAdd.First().CustomOptions;
            foreach (var item in membersToAdd.Skip(1))
            {
                if (Differ(firstCustomOptions, item.CustomOptions))
                {
                    shouldExcludeFromCustomOptions = true;
                    break;
                }
            }
        }

        return shouldExcludeFromCustomOptions;

    }

    private bool Differ(IList<object> first, IList<object> second)
    {
        if (first.Count != second.Count)
        {
            return true;
        }
        for (int i = 0; i < first.Count; i++)
        {
            if (!object.Equals(first[i], second[i]))
            {
                return true;
            }
        }
        return false;
    }

    #endregion
}
