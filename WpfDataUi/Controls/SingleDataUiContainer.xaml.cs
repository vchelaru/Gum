using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace WpfDataUi
{
    /// <summary>
    /// Interaction logic for SingleDataUiContainer.xaml
    /// </summary>
    public partial class SingleDataUiContainer : UserControl
    {
        #region Fields

        static List<KeyValuePair<Func<Type, bool>, Type>> mTypeDisplayerAssociation = new List<KeyValuePair<Func<Type, bool>, Type>>();

        // Controls are expensive to create (WPF InitializeComponent builds a full visual tree).
        // When a SingleDataUiContainer is removed from the visual tree during a grid rebuild,
        // its inner displayer control is returned here and reused by the next container that
        // needs the same type, avoiding repeated construction.
        static Dictionary<Type, Stack<UserControl>> _controlPool = new();

        #endregion

        #region Properties

        public UserControl UserControl { get; private set; }

        InstanceMember InstanceMember
        {
            get
            {
                return (InstanceMember)DataContext;
            }
        }

        object Instance
        {
            get
            {
                return InstanceMember.Instance;
            }
        }

        public static List<KeyValuePair<Func<Type, bool>, Type>> TypeDisplayerAssociation
        {
            get
            {
                return mTypeDisplayerAssociation;
            }
        }

        #endregion

        #region Constructor

        static SingleDataUiContainer()
        {
            mTypeDisplayerAssociation.Add(new KeyValuePair<Func<Type, bool>, Type>(
                (item) => item == typeof(bool),
                typeof(CheckBoxDisplay))
                );


            mTypeDisplayerAssociation.Add(new KeyValuePair<Func<Type, bool>, Type>(
                (item) => item == typeof(bool?),
                typeof(NullableBoolDisplay))
                );


            mTypeDisplayerAssociation.Add(new KeyValuePair<Func<Type, bool>, Type>(
                (item) => item!= null && item.IsEnum,
                typeof(ComboBoxDisplay))
                );

            mTypeDisplayerAssociation.Add(new KeyValuePair<Func<Type, bool>, Type>(
                (item) => item != null && typeof(IEnumerable).IsAssignableFrom(item) && item != typeof(string),
                typeof(ListBoxDisplay))
                );
        }
        public SingleDataUiContainer()
        {
            this.DataContextChanged += HandleDataContextChanged;
            this.Unloaded += HandleUnloaded;
            InitializeComponent();

        }

        #endregion

        private void HandleUnloaded(object? sender, RoutedEventArgs e)
        {
            if (UserControl != null)
            {
                // Detach from this Grid first so the control can be re-parented when
                // reused from the pool (a UIElement can only have one visual parent).
                Grid.Children.Remove(UserControl);

                var type = UserControl.GetType();
                if (!_controlPool.TryGetValue(type, out var stack))
                    _controlPool[type] = stack = new Stack<UserControl>();
                stack.Push(UserControl);
            }
        }

        static UserControl? TryGetFromPool(Type type)
        {
            if (_controlPool.TryGetValue(type, out var stack) && stack.Count > 0)
                return stack.Pop();
            return null;
        }

        private void HandleDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            Grid.Children.Clear();

            UserControl = CreateInternalControl();
            Grid.Children.Add(UserControl);
        }

        UserControl CreateInternalControl()
        {
            if(InstanceMember == null)
            {
                throw new NullReferenceException(nameof(InstanceMember));
            }


            UserControl controlToAdd = null;

            if (InstanceMember.PreferredDisplayer != null)
            {
                var preferredDisplayer = InstanceMember.PreferredDisplayer;
                if(this.UserControl != null && this.UserControl.GetType() == preferredDisplayer)
                {
                    // reuse the existing control for speed, and to fix a potential bug when losing focus:
                    controlToAdd = this.UserControl;
                }
                else
                {
                    controlToAdd = TryGetFromPool(preferredDisplayer)
                        ?? (UserControl)Activator.CreateInstance(preferredDisplayer);
                }
            }

            // give preference to CustomOptions...:
            if(controlToAdd == null && InstanceMember.CustomOptions != null && InstanceMember.CustomOptions.Count != 0)
            {
                controlToAdd = TryGetFromPool(typeof(ComboBoxDisplay)) ?? new ComboBoxDisplay();
            }

            // ... then fall back if that isn't found:
            if (controlToAdd == null)
            {
                var type = InstanceMember.PropertyType;

                foreach (var kvp in mTypeDisplayerAssociation)
                {
                    if (kvp.Key(type))
                    {
                        controlToAdd = TryGetFromPool(kvp.Value)
                            ?? (UserControl)Activator.CreateInstance(kvp.Value);
                    }
                }
            }

            if (controlToAdd == null)
            {
                controlToAdd = TryGetFromPool(typeof(TextBoxDisplay)) ?? new TextBoxDisplay();
            }

            var displayerType = controlToAdd.GetType();

            foreach (var kvp in InstanceMember.PropertiesToSetOnDisplayer)
            {
                var propertyInfo = displayerType.GetProperty(kvp.Key);

                propertyInfo?.SetValue(controlToAdd, kvp.Value, null);
            }

            if(controlToAdd is IDataUi display)
            {
                display.InstanceMember = InstanceMember;

                InstanceMember.PropertyChanged += (sender, args) =>
                {
                    switch (args.PropertyName)
                    {
                        case nameof(InstanceMember.DetailText):
                            display.Refresh();
                            break;
                    }
                };
            }
            else
            {
                throw new InvalidOperationException(
                    $"The object of type {controlToAdd?.GetType()} must implement the IDataUi interface to be used in a DataUiGrid");
            }

            InstanceMember.CallUiCreated(controlToAdd);

            // can we share them like this? Is it safe? OK?
            this.ContextMenu = controlToAdd.ContextMenu;

            return controlToAdd;
        }

    }
}
