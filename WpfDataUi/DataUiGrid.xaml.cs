
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfDataUi.Controls;
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
    public partial class DataUiGrid : UserControl, INotifyPropertyChanged
    {
        #region Fields

        //object mInstance;
        StackPanel templateStackPanel;

        List<IDataUi> mDataUi = new List<IDataUi>();

        // Some members are optinally visible based off of a delegate.  We need to store
        // these off so that the delegate can be re-evaluated every time a member changes,
        // as the member may be based off of the current state of the instance.
        Dictionary<InstanceMember, Func<InstanceMember, bool>> mMembersWithOptionalVisibility = new Dictionary<InstanceMember, Func<InstanceMember, bool>>();
        #endregion

        #region Instance (Bindable)

        /// <summary>
        /// Sets the displayed instance.  Setting this property
        /// refreshes the Categories object, which means that any
        /// changes made directly to Categories, or applied through 
        /// the Apply function will only persist until the next time
        /// this property is set.
        /// </summary>
        public object Instance
        {
            get { return (object)GetValue(InstanceProperty); }
            set { SetValue(InstanceProperty, value); }
        }

        
        // This is currently a DP, but it doesn't function because the DataContext for this is not the Instance
        // I should consider changing this at some point...
        public static readonly DependencyProperty InstanceProperty =
            DependencyProperty.Register("Instance", 
                typeof(object), 
                typeof(DataUiGrid), 
                new PropertyMetadata(null, HandlePropertyChanged));

        private static void HandlePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as DataUiGrid;
            var oldValue = e.OldValue;
            if(oldValue is INotifyPropertyChanged oldNotifyPropertyChanged)
            {
                oldNotifyPropertyChanged.PropertyChanged -= grid.HandleInstancePropertyChanged;
            }

            grid.mMembersWithOptionalVisibility.Clear();

            grid.PopulateCategories();

            if(grid.Instance is INotifyPropertyChanged newNotifyPropertyChanged)
            {
                newNotifyPropertyChanged.PropertyChanged += grid.HandleInstancePropertyChanged;
            }
        }

        private void HandleInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshDelegateBasedElementVisibility();
        }

        #endregion

        #region Properties

        //public object Instance
        //{
        //    get
        //    {
        //        return mInstance;
        //    }
        //    set
        //    {
        //        if (mInstance != value)
        //        {
        //            mInstance = value;
        //            mMembersWithOptionalVisibility.Clear();

        //            PopulateCategories();
        //        }
        //    }
        //}

        public ObservableCollection<Type> TypesToIgnore
        {
            get;
            private set;
        }

        public ObservableCollection<string> MembersToIgnore
        {
            get;
            private set;
        }

        public ObservableCollection<MemberCategory> Categories
        {
            get;
            private set;
        }

        public bool IsInnerGridEnabled
        {
            get => InternalControl.IsEnabled;
            set => InternalControl.IsEnabled = value;
        }

        Orientation mCategoriesOrientation = Orientation.Vertical;
        public Orientation CategoriesOrientation
        {
            get => mCategoriesOrientation;
            set
            {
                if(value != mCategoriesOrientation)
                {
                    mCategoriesOrientation = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CategoriesOrientation)));
                }
            }
        }

        #endregion

        #region Events
        public event Action<string, BeforePropertyChangedArgs> BeforePropertyChange;
        /// <summary>
        /// Raised whenever an instance member is set by the UI, such as the user typing a value in a text box.
        /// </summary>
        public event Action<string, PropertyChangedArgs> PropertyChange;

        /// <summary>
        /// Implementation of INotifyPropertyChanged for this object to enable it to self-bind. This should not
        /// be accessed outside of the class.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructor

        public DataUiGrid()
        {
            Categories = new ObservableCollection<MemberCategory>();

            InitializeComponent();
            
            //TreeView.DataContext = mCategories;


            TypesToIgnore = new ObservableCollection<Type>();
            TypesToIgnore.CollectionChanged += HandleTypesToIgnoreChanged;

            MembersToIgnore = new ObservableCollection<string>();
            MembersToIgnore.CollectionChanged += HandleMembersToIgnoreChanged;

            this.DataContext = this;
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

        private void RefreshDelegateBasedElementVisibilityFromConditions()
        {
            throw new NotImplementedException();
        }

        private void RefreshDelegateBasedElementVisibility()
        {
            foreach (var kvp in mMembersWithOptionalVisibility)
            {
                var member = kvp.Key;
                var category = member.Category;
                bool shouldBeVisible = !kvp.Value(member);
                bool isVisible = category.Members.Contains(member);

                if (isVisible && !shouldBeVisible)
                {
                    category.Members.Remove(kvp.Key);
                }
                else if (!isVisible && shouldBeVisible)
                {
                    category.Members.Add(member);
                }
            }
        }

        private void ApplyDisplayPropertyToInstanceMember(InstanceMemberDisplayProperties displayProperties, InstanceMember member, MemberCategory category)
        {
            if (displayProperties.IsHiddenDelegate != null && mMembersWithOptionalVisibility.ContainsKey(member) == false)
            {
                mMembersWithOptionalVisibility.Add(member, displayProperties.IsHiddenDelegate);
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
            if(TryGetInstanceMember(memberName, out InstanceMember member, out MemberCategory _))
            {
                return member;
            }
            return null;
        }

        private void HandleMembersToIgnoreChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PopulateCategories();
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

            if(desiredCategory == null)
            {
                desiredCategory = new MemberCategory(categoryName);
                Categories.Add(desiredCategory);
            }

            if(member != null && member.Category != desiredCategory)
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
                newMember.AfterSetByUi += HandleInstanceMemberSetByUi;
                newMember.BeforeSetByUi += HandleInstanceMemberBeforeSetByUi;
                newMember.Category = memberCategory;
                memberCategory.Members.Add(newMember);
            }
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

                args.NewValue = LateBinder.GetValueStatic(this.Instance, ((InstanceMember)sender).Name);
                args.PropertyName = ((InstanceMember)sender).Name;

                PropertyChange(((InstanceMember)sender).Name, args);
            }
            foreach (var item in InternalControl.Items)
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
            foreach (var item in InternalControl.Items)
            {
                MemberCategory memberCategory = item as MemberCategory;

                foreach (var instanceMember in memberCategory.Members)
                {
                    instanceMember.SimulateValueChanged();

                }
            }

        }

        public void InsertSpacesInCamelCaseMemberNames()
        {
            foreach (var category in Categories)
            {
                foreach (var member in category.Members)
                {
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

        #endregion

    }
}
