
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace WpfDataUi
{
    #region DataUiGridEntry class
    public class DataUiGridEntry
    {
        public string Name { get; set; }
    }

    #endregion

    /// <summary>
    /// Interaction logic for DataUiGrid.xaml
    /// </summary>
    public class DataUiGrid : ItemsControl, INotifyPropertyChanged
    {
        #region Fields

        // Some members are optinally visible based off of a delegate.  We need to store
        // these off so that the delegate can be re-evaluated every time a member changes,
        // as the member may be based off of the current state of the instance.
        private readonly Dictionary<InstanceMember, Func<InstanceMember, bool>> _membersWithOptionalVisibility
            = new();

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty InstanceProperty =
            DependencyProperty.Register(
                nameof(Instance),
                typeof(object),
                typeof(DataUiGrid),
                new PropertyMetadata(null, HandleInstanceChanged));

        private static void HandleInstanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (DataUiGrid)d;

            if (e.OldValue is INotifyPropertyChanged oldNpc)
                oldNpc.PropertyChanged -= grid.HandleInstancePropertyChanged;

            grid._membersWithOptionalVisibility.Clear();
            grid.PopulateCategories();

            if (grid.Instance is INotifyPropertyChanged newNpc)
                newNpc.PropertyChanged += grid.HandleInstancePropertyChanged;
        }

        private void HandleInstancePropertyChanged(object sender, PropertyChangedEventArgs e) =>
            RefreshDelegateBasedElementVisibility();

        /// <summary>
        /// Sets the displayed instance.  Setting this property
        /// refreshes the Categories object, which means that any
        /// changes made directly to Categories, or applied through 
        /// the Apply function will only persist until the next time
        /// this property is set
        /// </summary>
        public object Instance
        {
            get => GetValue(InstanceProperty);
            set => SetValue(InstanceProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DataUiGrid),
                new PropertyMetadata(Orientation.Vertical));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        #endregion

        #region Properties

        public ObservableCollection<Type> TypesToIgnore { get; } = [];
        public ObservableCollection<string> MembersToIgnore { get; } = [];
        public ObservableCollection<MemberCategory> Categories { get; } = [];

        #endregion

        #region Events

        public event Action<string, BeforePropertyChangedArgs> BeforePropertyChange;

        /// <summary>
        /// Raised whenever an instance member is set by the UI, such as the user typing a value in a text box.
        /// </summary>
        public event Action<string, PropertyChangedArgs> PropertyChange;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructor

        static DataUiGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DataUiGrid),
                new FrameworkPropertyMetadata(typeof(DataUiGrid)));
        }

        public DataUiGrid()
        {
            ItemsSource = Categories;

            Categories.CollectionChanged += HandleCategoriesChanged;
            TypesToIgnore.CollectionChanged += (_, __) => PopulateCategories();
            MembersToIgnore.CollectionChanged += HandleMembersToIgnoreChanged;
        }

        private void HandleCategoriesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Subscribe(e.NewItems);
            Unsubscribe(e.OldItems);
        }

        private void Subscribe(IList newItems)
        {
            if (newItems == null) return;
            foreach (MemberCategory category in newItems)
            {
                category.MemberValueChangedByUi += HandleCategoryMemberChanged;
            }
        }

        private void Unsubscribe(IList oldItems)
        {
            if (oldItems == null) return;
            foreach (MemberCategory category in oldItems)
            {
                category.MemberValueChangedByUi -= HandleCategoryMemberChanged;
            }
        }

        private void HandleCategoryMemberChanged(InstanceMember member)
        {
            HandleInstanceMemberSetByUi(member, null);
        }

        #endregion

        #region Methods

        public void Apply(TypeMemberDisplayProperties properties)
        {
            foreach (var property in properties.DisplayProperties)
            {
                // does this member exist?
                InstanceMember member;
                MemberCategory category;

                bool found = TryGetInstanceMember(property.Name, out member, out category);

                if (member != null)
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
                    MemberInfo memberInfo = field as MemberInfo;
                    MembersToIgnore.Add(memberInfo.Name);
                }
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    MemberInfo memberInfo = property as MemberInfo;
                    MembersToIgnore.Add(memberInfo.Name);
                }

            }
        }

        private void RefreshDelegateBasedElementVisibility()
        {
            foreach (var kvp in _membersWithOptionalVisibility.ToList())
            {
                var member = kvp.Key;
                var category = member.Category;
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

            //if (displayProperties.GetEffectiveIsHidden(member.Instance))
            // let's instead just use the hidden property - we will apply functions after
            if (displayProperties.IsHidden)
            {
                category.Members.Remove(member);
            }
            else
            {
                // Put an if-statement for debugging
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

        public bool TryGetInstanceMember(string name, out InstanceMember member, out MemberCategory category)
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

        public InstanceMember GetInstanceMember(string memberName)
        {
            if (TryGetInstanceMember(memberName, out InstanceMember member, out MemberCategory _))
            {
                return member;
            }
            return null;
        }

        private void HandleMembersToIgnoreChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // If we do this, we completely wipe all custom categories. No good!
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    List<string> newItems = [];
                    foreach (var item in e.NewItems)
                    {
                        newItems.Add(item as string);
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

                    List<string> oldItems = [];
                    foreach (var item in e.OldItems)
                    {
                        oldItems.Add(item as string);
                    }

                    if (Instance != null)
                    {
                        Type type = Instance.GetType();

                        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                        {
                            MemberInfo memberInfo = field as MemberInfo;
                            if (oldItems.Contains(field.Name))
                            {
                                TryCreateCategoryAndInstanceFor(memberInfo);
                            }
                        }
                        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (oldItems.Contains(property.Name))
                            {
                                MemberInfo memberInfo = property as MemberInfo;
                                TryCreateCategoryAndInstanceFor(memberInfo);
                            }
                        }
                    }
                    break;
                default:
                    // This is a destructive action, it removes previously-added custom members.
                    // This is how things used to work, but Vic would like to get rid of it completely.
                    // However, there may be cases that haven't been handled so we're keeping this as a fallback.
                    PopulateCategories();
                    break;
            }
        }

        private void HandleTypesToIgnoreChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PopulateCategories();
        }

        private void PopulateCategories()
        {
            this.Categories.Clear();

            if (Instance != null)
            {
                Type type = Instance.GetType();

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    MemberInfo memberInfo = field as MemberInfo;
                    TryCreateCategoryAndInstanceFor(memberInfo);
                }
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    MemberInfo memberInfo = property as MemberInfo;
                    TryCreateCategoryAndInstanceFor(memberInfo);
                }
            }

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
                member.Category.Members.Remove(member);
                desiredCategory.Members.Add(member);
            }
        }

        private void TryCreateCategoryAndInstanceFor(MemberInfo memberInfo)
        {
            if (ShouldCreateUiFor(memberInfo.GetMemberType(), memberInfo.Name))
            {

                string categoryName = GetCategoryAttributeFor(memberInfo);

                MemberCategory memberCategory = GetOrInstantiateAndAddMemberCategory(categoryName);

                InstanceMember newMember = new InstanceMember(memberInfo.Name, Instance);
                AssignInstanceMemberEvents(newMember);
                newMember.Category = memberCategory;
                memberCategory.Members.Add(newMember);
            }
        }

        private void AssignInstanceMemberEvents(InstanceMember newMember)
        {
            // don't do this here, because this can get called before custom properties are added.
            // We're going to rely on the Memberategory to raise MemberValueChangedByUi
            //newMember.AfterSetByUi += HandleInstanceMemberSetByUi;
            newMember.BeforeSetByUi += HandleInstanceMemberBeforeSetByUi;
        }

        private void HandleInstanceMemberBeforeSetByUi(object sender, EventArgs e)
        {
            if (BeforePropertyChange != null)
            {
                BeforePropertyChangedArgs args = (BeforePropertyChangedArgs)e;
                args.Owner = this.Instance;
                args.OldValue = ((InstanceMember)sender).Value;
                args.PropertyName = ((InstanceMember)sender).Name;

                BeforePropertyChange(((InstanceMember)sender).Name, args);

            }

        }

        private void HandleInstanceMemberSetByUi(object sender, EventArgs e)
        {
            if (PropertyChange != null)
            {
                PropertyChangedArgs args = new PropertyChangedArgs();
                args.Owner = this.Instance;

                // This assumes reflection, which is bad...
                //args.NewValue = LateBinder.GetValueStatic(this.Instance, ((InstanceMember)sender).Name);

                args.NewValue = ((InstanceMember)sender).Value;
                args.PropertyName = ((InstanceMember)sender).Name;

                PropertyChange(((InstanceMember)sender).Name, args);
            }
            foreach (var item in Items)
            {
                MemberCategory memberCategory = item as MemberCategory;

                foreach (var instanceMember in memberCategory.Members)
                {
                    if (instanceMember.Name != ((InstanceMember)sender).Name)
                    {
                        instanceMember.SimulateValueChanged();
                    }
                }
            }

            RefreshDelegateBasedElementVisibility();
        }

        private MemberCategory GetOrInstantiateAndAddMemberCategory(string categoryName)
        {
            MemberCategory memberCategory = Categories.FirstOrDefault(item => item.Name == categoryName);
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

            if (attributes != null && attributes.Length != 0)
            {
                CategoryAttribute attribute = attributes.FirstOrDefault() as CategoryAttribute;
                category = attribute.Category;
            }
            return category;
        }

        public void Refresh()
        {

            // might this be faster?
            for (int i = 0; i < Items.Count; i++)
            {
                var uiElement =
                    ItemContainerGenerator.ContainerFromIndex(i);

                bool handledByRefresh = false;

                if (uiElement is ContentPresenter contentPresenter && VisualTreeHelper.GetChildrenCount(contentPresenter) > 0 && VisualTreeHelper.GetChild(contentPresenter, 0) is Expander expander)
                {
                    var itemsInExpander = expander.Content as ItemsControl;

                    if (itemsInExpander != null)
                    {
                        for (int j = 0; j < itemsInExpander.Items.Count; j++)
                        {
                            var innerUiElement =
                                itemsInExpander.ItemContainerGenerator.ContainerFromIndex(j) as ContentPresenter;

                            if (VisualTreeHelper.GetChildrenCount(innerUiElement) > 0 && VisualTreeHelper.GetChild(innerUiElement, 0) is SingleDataUiContainer singleDataUiContainer)
                            {
                                (singleDataUiContainer.UserControl as IDataUi)?.Refresh();
                                handledByRefresh = true;
                            }
                        }
                    }
                }

                if (!handledByRefresh)
                {
                    MemberCategory memberCategory = Items[i] as MemberCategory;

                    foreach (var instanceMember in memberCategory.Members)
                    {
                        instanceMember.SimulateValueChanged();

                    }
                }

            }

            //foreach (var item in InternalControl.Items)
            //{
            //    MemberCategory memberCategory = item as MemberCategory;

            //    foreach (var instanceMember in memberCategory.Members)
            //    {
            //        instanceMember.SimulateValueChanged();

            //    }
            //}

        }

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
                    member.DisplayName = InsertSpacesInCamelCaseString(member.DisplayName);
                }
            }
        }

        static string InsertSpacesInCamelCaseString(string originalString)
        {
            // Normally in reverse loops you go til i > -1, but 
            // we don't want the character at index 0 to be tested.
            for (int i = originalString.Length - 1; i > 0; i--)
            {
                if (char.IsUpper(originalString[i]) && i != 0
                    // make sure there's not already a space there
                    && originalString[i - 1] != ' '
                    )
                {
                    originalString = originalString.Insert(i, " ");
                }
            }

            return originalString;
        }

        public void SetMultipleCategoryLists(List<List<MemberCategory>> listOfCategoryLists)
        {
            HashSet<string> alreadyAddedMembers = [];

            List<MemberCategory> effectiveCategory = [];

            foreach (var instance in listOfCategoryLists)
            {
                foreach (var category in instance)
                {
                    var newCategory = new MemberCategory();
                    newCategory.Name = category.Name;
                    effectiveCategory.Add(newCategory);

                    foreach (var member in category.Members)
                    {
                        if (alreadyAddedMembers.Contains(member.DisplayName) == false)
                        {
                            alreadyAddedMembers.Add(member.DisplayName);

                            var multiSelectInstanceMember = TryCreateMultiGroup(listOfCategoryLists, member);
                            if (multiSelectInstanceMember != null)
                            {
                                newCategory.Members.Add(multiSelectInstanceMember);
                            }
                        }
                    }
                }
            }

            this.Categories.Clear();

            foreach (var category in effectiveCategory)
            {
                this.Categories.Add(category);

            }
        }

        private MultiSelectInstanceMember TryCreateMultiGroup(List<List<MemberCategory>> source, InstanceMember templateMember)
        {
            List<InstanceMember> membersToAdd = [];
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
            // They all have 
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
            {
                for (int i = 0; i < first.Count; i++)
                {
                    if (!object.Equals(first[i], second[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion
    }
}
