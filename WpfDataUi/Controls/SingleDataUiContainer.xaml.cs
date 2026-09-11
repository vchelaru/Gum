using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace WpfDataUi
{
    /// <summary>
    /// The WPF row host: creates (or reuses) the displayer control for its <see cref="InstanceMember"/>
    /// DataContext. Which control is chosen is decided by <see cref="DisplayerRegistry"/>.
    /// </summary>
    public partial class SingleDataUiContainer : UserControl
    {
        #region Fields

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

        /// <summary>
        /// Maps displayer keys to this head's controls and picks the control for each row. Pre-filled
        /// with the WpfDataUi editors; the tool registers its own displayers at startup.
        /// </summary>
        public static DisplayerRegistry DisplayerRegistry { get; }

        #endregion

        #region Constructor

        static SingleDataUiContainer()
        {
            DisplayerRegistry = new DisplayerRegistry();
            DisplayerRegistry.Register(typeof(StandardDisplayers.TextBox), typeof(TextBoxDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.MultiLineTextBox), typeof(MultiLineTextBoxDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.CheckBox), typeof(CheckBoxDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.NullableBool), typeof(NullableBoolDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.ComboBox), typeof(ComboBoxDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.EditableComboBox), typeof(EditableComboBoxDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.ListBox), typeof(ListBoxDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.Slider), typeof(SliderDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.PlusMinus), typeof(PlusMinusTextBox));
            DisplayerRegistry.Register(typeof(StandardDisplayers.AngleSelector), typeof(AngleSelectorDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.FileSelection), typeof(FileSelectionDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.MultiFile), typeof(MultiFileDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.StringList), typeof(StringListTextBoxDisplay));
            DisplayerRegistry.Register(typeof(StandardDisplayers.InlineChannels), typeof(InlineChannelsDisplay));
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
            if (UserControl == null)
            {
                return;
            }

            // Only pool when the container is being discarded by a grid rebuild
            // (DataContext goes null or changes). When a tab is merely hidden,
            // the DataContext stays the same and we should keep the control
            // attached so it reappears intact when the tab is shown again.
            if (this.DataContext != null)
            {
                return;
            }

            Grid.Children.Remove(UserControl);

            if (UserControl is IDataUi dataUi)
            {
                dataUi.ResetForPooling();
            }

            var type = UserControl.GetType();
            if (!_controlPool.TryGetValue(type, out var stack))
            {
                _controlPool[type] = stack = new Stack<UserControl>();
            }
            stack.Push(UserControl);
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

            Type controlType = DisplayerRegistry.SelectControlType(InstanceMember);

            UserControl controlToAdd;

            // Reuse the existing control for an explicitly preferred displayer: faster, and it keeps
            // focus when a row is re-bound to a member of the same kind.
            if (InstanceMember.PreferredDisplayer != null && this.UserControl != null && this.UserControl.GetType() == controlType)
            {
                controlToAdd = this.UserControl;
            }
            else
            {
                controlToAdd = TryGetFromPool(controlType)
                    ?? (UserControl)Activator.CreateInstance(controlType)!;
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
