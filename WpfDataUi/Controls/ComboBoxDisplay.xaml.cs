using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls;


public class ComboBoxDisplay : UserControl, IDataUi, INotifyPropertyChanged
{
    /// <inheritdoc cref="ComboBoxDisplayLogic.NullSentinel"/>
    public const string NullSentinel = ComboBoxDisplayLogic.NullSentinel;

    private readonly ComboBoxDisplayLogic _logic = new ComboBoxDisplayLogic();

    #region Fields


    InstanceMember? _instanceMember;


    Type? mInstancePropertyType;

    static Brush? mUnmodifiedBrush = null;

    #endregion

    #region Properties

    public InstanceMember? InstanceMember
    {
        get
        {
            return _instanceMember;
        }
        set
        {
            bool instanceMemberChanged = _instanceMember != value;
            if (_instanceMember != null && instanceMemberChanged)
            {
                _instanceMember.PropertyChanged -= HandlePropertyChange;
            }
            _instanceMember = value;
            if (_instanceMember != null && instanceMemberChanged)
            {
                _instanceMember.PropertyChanged += HandlePropertyChange;
            }

            if (instanceMemberChanged)
            {
                this.RefreshAllContextMenus(force: true);
                // Clear stale green background from a previous pooled use.
                ComboBox.ClearValue(Control.BackgroundProperty);
            }

            Refresh();
        }
    }

    public bool SuppressSettingProperty { get; set; }

    protected Grid Grid
    {
        get;
        private set;
    } = default!;

    private ComboBox ComboBox
    {
        get;
        set;
    } = default!;

    private TextBlock TextBlock
    {
        get;
        set;
    } = default!;
    private TextBlock HintTextBlock = default!;


    public bool IsEditable
    {
        get => ComboBox.IsEditable;
        set => ComboBox.IsEditable = value;
    }

    #endregion

    public virtual void ResetForPooling()
    {
        ComboBox.IsEditable = false;
    }

#pragma warning disable CS0067 // Required by INotifyPropertyChanged; reserved for derived classes.
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    public ComboBoxDisplay()
    {

        // So this used to use WPF, but it turns out that inheriting from 
        // this class and instantiating the derived class causes a crash, as 
        // discussed here:
        // http://stackoverflow.com/questions/7646331/the-component-does-not-have-a-resource-identified-by-the-uri
        // I tried rebuilding, deleting folders, restarting Visual Studio, no go. So I guess I'm going to C# it
        //InitializeComponent();
        CreateLayout();
        


        if (mUnmodifiedBrush == null)
        {
            mUnmodifiedBrush = ComboBox.Background;
        }

        this.ComboBox.DataContext = this;

        //this.ComboBox.IsEditable = true;

        RefreshAllContextMenus();

        this.ComboBox.IsKeyboardFocusWithinChanged += HandleIsKeyboardFocusChanged;
        this.ComboBox.PreviewKeyDown += HandlePreviewKeyDown;
        this.ComboBox.KeyDown += HandleKeyDown;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ComboBox.IsEditable)
        {
            HandleChange();
            e.Handled = true;
        }
    }

    private void HandlePreviewKeyDown(object? sender, KeyEventArgs e)
    {
        // by suppressing CTRL+Z, we prevent it from
        // undoing a change to a bad value as shown here:
        // https://github.com/vchelaru/Gum/issues/658
        // But I'm not sure how to push this back up to the
        // app to do app-level undo.
        if (e.Key == Key.Z && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            e.Handled = true;
        }
    }

    private void HandleIsKeyboardFocusChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        // Sept 15, 2021
        // This is an interesting bug...
        // If the user right-clicks on a combo 
        // box to set it to default, the combo box
        // changes the property on an object which may
        // result in out put being printed (such as about
        // code generation). When this happens, the output
        // window scrolls to the bottom, which takes keyboard
        // focus from whatever was focused before, which is this
        // combo box. The combo box probably got focus from the right-click.
        // For now the fix is easy - just make it only do so if it's editable,
        // but this may require more fixes to prevent this bug from happening on 
        // editable combo boxes.
        //if(ComboBox.IsKeyboardFocusWithin == false)
        if(ComboBox.IsKeyboardFocusWithin == false && IsEditable)
        {
            HandleChange();
        }
    }

    private void CreateLayout()
    {
        StackPanel stackPanel = new StackPanel();

        Grid = new Grid();
        Grid.Name = "TopRowGrid";
        stackPanel.Children.Add(Grid);

        var firstColumnDefinition = new ColumnDefinition();
        firstColumnDefinition.SetBinding(ColumnDefinition.WidthProperty, "FirstGridLength");
        Grid.ColumnDefinitions.Add(firstColumnDefinition);
        Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto});

        Grid.RowDefinitions.Add(new RowDefinition());
        Grid.RowDefinitions.Add(new RowDefinition());

        TextBlock = new TextBlock
        {
            Name="Label",
            VerticalAlignment = VerticalAlignment.Center,
            Text = "Property Label:"
        };
        TextBlock.TextWrapping = TextWrapping.Wrap;
        TextBlock.Margin = new Thickness(4, 0, 0, 0);
        TextBlock.ContextMenu = new ContextMenu();

        Grid.Children.Add(TextBlock);

        ComboBox = new ComboBox
        {
            Name="ComboBox",
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinWidth = 60
        };
        ComboBox.ContextMenu = new ContextMenu();
        // 4 px margin matching the text block in a TextBoxDisplay
        //ComboBox.SetBinding(TextBlock.ForegroundProperty, "DesiredForegroundBrush");

        //TextBlock.SetForeground(ComboBox, asdf);
        //var textBlock = FindVisualChildByName<TextBlock>(ComboBox, "TextBlock");
        //textBlock.SetBinding(TextBlock.ForegroundProperty, "DesiredForegroundBrush");
        ComboBox.SelectionChanged += ComboBox_SelectionChanged;


        Grid.SetColumn(ComboBox, 1);
        Grid.Children.Add(ComboBox);

        HintTextBlock = new TextBlock
        {
            Name="DetailTextBlock",
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(8, 1, 0, 4),
            Margin = new Thickness(0, 0, -4, 0),
            FontSize = 10
        };

        stackPanel.Children.Add(HintTextBlock);

        this.Content = stackPanel;
    }

    public void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        var hasEnoughInformationToWork = this.HasEnoughInformationToWork();
        if (hasEnoughInformationToWork)
        {
            Type type = this.GetPropertyType();

            mInstancePropertyType = type;

            PopulateItems();
        }

        object valueOnInstance;
        bool successfulGet = this.TryGetValueOnInstance(out valueOnInstance);
        if (successfulGet)
        {
            if (valueOnInstance != null)
            {
                TrySetValueOnUi(valueOnInstance);
            }
            else
            {
                this.ComboBox.Text = null;
            }
        }
        else
        {

        }

        HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
        HintTextBlock.Text = InstanceMember?.DetailText;

        SyncBackgroundWithState();

        RefreshAllContextMenus();
        
        this.TextBlock.Text = InstanceMember?.DisplayName;

        RefreshIsEnabled();

    }

    private void RefreshIsEnabled()
    {
        if (InstanceMember?.IsReadOnly == true)
        {
            this.IsEnabled = false;
        }
        else
        {
            this.IsEnabled = true;
        }
    }

    private void RefreshAllContextMenus(bool force = false)
    {
        if (force)
        {
            this.ForceRefreshContextMenu(ComboBox.ContextMenu);
            this.ForceRefreshContextMenu(TextBlock.ContextMenu);
        }
        else
        {
            this.RefreshContextMenu(ComboBox.ContextMenu);
            this.RefreshContextMenu(TextBlock.ContextMenu);
        }
    }

    public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        this.SuppressSettingProperty = true;
        // A null nullable-enum value selects the "<None>" entry, so the combo still shows that the
        // variable has no value rather than deselecting.
        object? itemToSelect = _logic.GetItemToSelect(valueOnInstance, mInstancePropertyType);
        this.ComboBox.SelectedItem = itemToSelect;
        this.ComboBox.Text = itemToSelect?.ToString();
        this.SuppressSettingProperty = false;

        SyncBackgroundWithState();

        return ApplyValueResult.Success;
    }

    protected virtual IEnumerable<object> CustomOptions => _logic.GetOptions(InstanceMember, mInstancePropertyType);

    private void PopulateItems()
    {
        this.SuppressSettingProperty = true;

        // July 17, 2022
        // Should this be 
        // a "smart refresh"
        // which only adds and
        // removes individual items
        // which should be changed, rather
        // than a full refresh?
        // July 5, 2025
        // Yes, it should because
        // otherwise this can be slow
        // on frequent refreshes like if
        // playing animations in Gum:

        var shouldRefresh = false;

        if(this.ComboBox.Items.Count != CustomOptions.Count())
        {
            shouldRefresh = true;
        }

        if (shouldRefresh == false)
        {
            for (int i = 0; i < this.ComboBox.Items.Count; i++)
            {
                var item = this.ComboBox.Items[i];

                var customOption = CustomOptions.ElementAt(i);

                if (customOption?.Equals(item) != true)
                {
                    shouldRefresh = true;
                    break;
                }
            }
        }

        if(shouldRefresh)
        {
            this.ComboBox.Items.Clear();
            foreach(var item in CustomOptions)
            {
                this.ComboBox.Items.Add(item);
            }
        }

        this.SuppressSettingProperty = false;
    }

    public ApplyValueResult TryGetValueOnUi(out object? value)
    {
        if(ComboBox.IsEditable)
        {
            value = ComboBox.Text;
        }
        else
        {
            value = this.ComboBox.SelectedItem;
        }

        return ApplyValueResult.Success;
    }

    bool isInSelectionChanged = false;
    private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // August 17, 2021 - If we check ComboBox.IsKeyboardFocusWithin,
        // then selecting a new item in the drop-down won't update the UI
        // immediately because the text is focused. Why? This seems annoying
        // for the user to have to tab out of the textbox...
        //var canBroadcast = ComboBox.IsEditable == false ||
        //    ComboBox.IsKeyboardFocusWithin == false;
        // Update - this code was here to prevent recursive calls to this because of 
        // the text box being changed. Therefore, we should have a value here to prevent recurisve calls
        if(!isInSelectionChanged)
        {
            isInSelectionChanged = true;

            var selectedItemString = this.ComboBox.SelectedItem?.ToString();
            var selectedItem = this.ComboBox.SelectedItem;
            // The text hasn't yet been set by default, so we need to force the text value here:
            ComboBox.Text = selectedItemString;

            // March 21, 2022
            // The ComboBoxDisplay
            // has a very weird bug
            // as reported here: https://github.com/vchelaru/FlatRedBall/issues/503
            // This bug happens when
            // a value is changed on the
            // combo box. That change is assigned
            // on the ComboBox.Text, which then calls
            // ComboBox_SelectionChanged again. For some
            // reason this recurisve call nulls out the display.
            // Vic has no idea why, but re-setting the SelectedItem
            // seems to fix it. So...HACK ALERT:
            // Update March 23, 2022
            ComboBox.SelectedItem = selectedItem;

            // Only apply the change immediately if:
            // 1. ComboBox is not editable, OR
            // 2. ComboBox is editable but the dropdown is open (user clicked an item)
            // If the user is typing and happens to match an item, wait for focus loss
            var shouldApplyImmediately = !ComboBox.IsEditable || ComboBox.IsDropDownOpen;
            
            if (shouldApplyImmediately)
            {
                HandleChange();
            }

            isInSelectionChanged = false;
        }

    }

    private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InstanceMember.Value))
        {
            this.Refresh();

        }
    }

    private void HandleChange()
    {
        // April 23, 2025 - I would like
        // to detect if this was assigned
        // by explicitly selecting an item in
        // the dropdown, or if it's a set while
        // searching through items which would be
        // an SetPropertyCommitType.Intermediate. However
        // as soon as an item is selected, the textbox is
        // focused. This is preventing us from fixing this
        // problem: https://github.com/vchelaru/Gum/issues/676.
        this.TrySetValueOnInstance();

        SyncBackgroundWithState();
    }

    private void SyncBackgroundWithState()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (DataUiGrid.GetOverridesIsDefaultStyling(this))
            {
                return;
            }

            // Green background for a default value, matching TextBoxDisplayLogic (FlatRedBall#1755).
            if (InstanceMember?.IsDefault == true)
            {
                ComboBox.Background = DataUiBrushes.DefaultValueBackground;
            }
            else if (ComboBox.TryFindResource("Frb.Brushes.Field.Background") != null)
            {
                ComboBox.SetResourceReference(Control.BackgroundProperty,
                    "Frb.Brushes.Field.Background");
            }
            else
            {
                ComboBox.ClearValue(Control.BackgroundProperty);
            }
        });

    }
}
