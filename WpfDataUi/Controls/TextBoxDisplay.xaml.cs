using System;
using System.ComponentModel;
using System.IO.Packaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Xml.Linq;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Interaction logic for TextBoxDisplay.xaml
    /// </summary>
    public partial class TextBoxDisplay : UserControl, IDataUi, ISetDefaultable
    {
        #region Fields/Properties

        TextBoxDisplayLogic mTextBoxLogic;

        InstanceMember mInstanceMember;

        ApplyValueResult? lastApplyValueResult = null;

        bool IsInSet = false;

        public decimal? LabelDragValueRounding { get; set; } = 1;

        public bool EnableLabelDragValueChange { get; set; } = true;

        public InstanceMember InstanceMember
        {
            get => mInstanceMember;
            set
            {
                mTextBoxLogic.InstanceMember = value;

                bool instanceMemberChanged = mInstanceMember != value;
                if (mInstanceMember != null && instanceMemberChanged)
                {
                    mInstanceMember.PropertyChanged -= HandlePropertyChange;
                }
                mInstanceMember = value;

                if (mInstanceMember != null && instanceMemberChanged)
                {
                    mInstanceMember.PropertyChanged += HandlePropertyChange;
                }

                if(instanceMemberChanged)
                {
                    this.RefreshAllContextMenus(force:true);
                }

                Refresh();
            }
        }

        public bool SuppressSettingProperty { get; set; }

        bool isAboveBelowLayout;
        public bool IsAboveBelowLayout
        {
            get => isAboveBelowLayout;
            set
            {
                if(isAboveBelowLayout != value)
                {
                    isAboveBelowLayout = value;
                    if(isAboveBelowLayout)
                    {
                        // move these to the 2nd row:
                        this.TextBox.SetValue(Grid.RowProperty, 1);
                        this.TextBox.SetValue(Grid.ColumnProperty, 0);
                        this.TextBox.SetValue(Grid.ColumnSpanProperty, 3);

                        this.PlaceholderText.SetValue(Grid.RowProperty, 1);
                        this.PlaceholderText.SetValue(Grid.ColumnProperty, 0);
                        this.PlaceholderText.SetValue(Grid.ColumnSpanProperty, 3);
                    }
                }
            }
        }

        public string NullCheckboxText
        {
            get => (string)NullableCheckBox.Content;
            set => NullableCheckBox.Content = value;
        }

        #endregion

        static void LoadViewFromUri(UserControl userControl, string baseUri)
        {
            try
            {
                var resourceLocater = new Uri(baseUri, UriKind.Relative);
                var exprCa = (PackagePart)typeof(System.Windows.Application).GetMethod("GetResourceOrContentPart", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { resourceLocater });
                var stream = exprCa.GetStream();
                var uri = new Uri((Uri)typeof(BaseUriHelper).GetProperty("PackAppBaseUri", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null), resourceLocater);
                var parserContext = new ParserContext
                {
                    BaseUri = uri
                };
                typeof(XamlReader).GetMethod("LoadBaml", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { stream, parserContext, userControl, true });
            }
            catch (Exception)
            {
                //log
            }
        }

        public TextBoxDisplay()
        {
            // from here:
            // https://stackoverflow.com/questions/7646331/the-component-does-not-have-a-resource-identified-by-the-uri
            // Inheriting from TextBoxDisplay results in a confusing runtime error. It seems like the problem is that the 
            // code tries to load the XAML based on the class name. This forces it:
            var assemblyName = typeof(TextBoxDisplay).Assembly.FullName.Split(',')[0];
            var xamlLocation = $"/{assemblyName};component/controls/textboxdisplay.xaml";
            //InitializeComponent();
            LoadViewFromUri(this, xamlLocation);
            mTextBoxLogic = new TextBoxDisplayLogic(this, TextBox);

            this.RefreshAllContextMenus();
            this.ContextMenu = TextBox.ContextMenu;
        }

        private void RefreshAllContextMenus(bool force=false)
        {
            if(force)
            {
                this.ForceRefreshContextMenu(TextBox.ContextMenu);
                this.ForceRefreshContextMenu(StackPanel.ContextMenu);
            }
            else
            {
                this.RefreshContextMenu(TextBox.ContextMenu);
                this.RefreshContextMenu(StackPanel.ContextMenu);
            }
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            // If the user is editing a value, we don't want to change
            // the value under the cursor
            // If we're default, then go ahead and change the value
            bool canRefresh =
                this.TextBox.IsKeyboardFocused == false || forceRefreshEvenIfFocused || mTextBoxLogic.InstanceMember.IsDefault;

            canRefresh = canRefresh && !IsInSet;

            if (canRefresh)
            {
                SuppressSettingProperty = true;

                mTextBoxLogic.RefreshDisplay(out object valueOnInstance);

                this.Label.Text = InstanceMember.DisplayName;
                this.RefreshAllContextMenus();

                RefreshHintText();

                RefreshIsEnabled(valueOnInstance, forceNullableEnable:false);

                SuppressSettingProperty = false;

                if (mTextBoxLogic.IsNumeric)
                {
                    this.Label.Cursor = Cursors.ScrollWE;
                }
                else
                {
                    this.Label.Cursor = null;
                }

                RefreshNullableRelatedUiVisibility(valueOnInstance);

            }
        }

        private void RefreshHintText()
        {
            HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
            HintTextBlock.Text = InstanceMember?.DetailText;
        }

        private void RefreshNullableRelatedUiVisibility(object valueOnInstance)
        {
            bool isNullable = IsDisplayedTypeNullable();

            if (isNullable)
            {
                this.NullableCheckBox.Visibility = Visibility.Visible;
            }
            else
            {
                this.NullableCheckBox.Visibility = Visibility.Collapsed;
            }
        }

        private bool IsDisplayedTypeNullable()
        {
            var type = InstanceMember?.PropertyType;

            var isNullable = type != null &&
                Nullable.GetUnderlyingType(type) != null;
            return isNullable;
        }

        public virtual ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            if(!mTextBoxLogic.IsInApplicationToInstance)
            {
                this.NullableCheckBox.IsChecked = valueOnInstance == null;
                // we could put more things here if needed
            }
            this.TextBox.Text = mTextBoxLogic.ConvertNumberToString(valueOnInstance);

            RefreshPlaceholderText();

            RefreshNullableRelatedUiVisibility(valueOnInstance);

            return ApplyValueResult.Success;
        }

        private void RefreshPlaceholderText()
        {
            if (this.IsFocused || this.TextBox.IsFocused)
            {
                PlaceholderText.Visibility = Visibility.Collapsed;
            }
            else
            {
                TryGetValueOnUi(out object valueOnInstance);
                if (valueOnInstance == null)
                {
                    PlaceholderText.Visibility = Visibility.Visible;
                    PlaceholderText.Text = "<NULL>";
                }
                else
                {
                    PlaceholderText.Visibility = Visibility.Collapsed;
                }

            }
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            if(this.NullableCheckBox.Visibility == Visibility.Visible && this.NullableCheckBox.IsChecked == true)
            {
                value = null;
                return ApplyValueResult.Success;
            }
            else
            {
                return mTextBoxLogic.TryGetValueOnUi(out value);
            }
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();
            }
            else if(e.PropertyName == nameof(InstanceMember.SupportsMakeDefault))
            {
                this.RefreshAllContextMenus(force: true);
            }
        }

        public void MakeMultiline()
        {
            this.Label.VerticalAlignment = VerticalAlignment.Top;

            this.TextBox.TextWrapping = TextWrapping.Wrap;
            this.TextBox.AcceptsReturn = true;
            this.TextBox.VerticalContentAlignment = VerticalAlignment.Top;
            this.TextBox.VerticalAlignment = VerticalAlignment.Top;
            this.TextBox.Height = 65;
            this.TextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            this.mTextBoxLogic.HandlesEnter = false;
        }

        public void AddUiAfterTextBox(UIElement element)
        {
            AfterTextBoxUi.Children.Add(element);
        }

        private void TextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            RefreshPlaceholderText();

            if(this.TextBox.Text != mTextBoxLogic.TextAtStartOfEditing)
            {
                // Check if the text has actually changed. If it hasn't, we don't
                // want to forcefully set the text on a lost focus.
                lastApplyValueResult = mTextBoxLogic.TryApplyToInstance();
            }

            TryGetValueOnUi(out object valueOnInstance);

            RefreshIsEnabled(valueOnInstance, forceNullableEnable:false);
        }

        private void RefreshIsEnabled(object valueOnInstance, bool forceNullableEnable)
        {
            if (lastApplyValueResult == ApplyValueResult.NotSupported)
            {
                this.IsEnabled = false;
            }
            else if(mTextBoxLogic.InstanceMember?.IsReadOnly == true)
            {
                this.IsEnabled = false;
            }
            else
            {
                if (IsDisplayedTypeNullable())
                {
                    this.TextBox.IsEnabled = forceNullableEnable || valueOnInstance != null;
                }

                this.IsEnabled = true;
            }
        }

        public void SetToDefault()
        {
            // So we don't exlicitly set values when losing focus
            this.mTextBoxLogic.HasUserChangedAnything = false;

            mTextBoxLogic.TextAtStartOfEditing = this.TextBox.Text;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            RefreshPlaceholderText();
        }

        #region Label Dragging
        double? currentDownX;
        double? pressedX;
        private double unroundedValue;

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);


        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mTextBoxLogic.IsNumeric && EnableLabelDragValueChange)
            {
                currentDownX = e.GetPosition(this).X;
                pressedX = currentDownX;

                System.Windows.Input.Mouse.Capture(Label);

                var getValueStatus = TryGetValueOnUi(out object valueOnInstance);

                if (getValueStatus == ApplyValueResult.Success)
                {
                    var converter = TypeDescriptor.GetConverter(mTextBoxLogic.InstancePropertyType);

                    if(valueOnInstance == null)
                    {
                        // If increasing from null, we should just treat it as if it's 0
                        valueOnInstance = 0;
                    }

                    unroundedValue = (double)converter.ConvertTo(valueOnInstance, typeof(double));
                }
            }
        }

        private void Label_MouseMove(object sender, MouseEventArgs e)
        {
            if(currentDownX != null)
            {
                if ( e.LeftButton == MouseButtonState.Pressed)
                {
                    var newX = e.GetPosition(this).X;
                    var difference = newX - currentDownX.Value;
                    currentDownX = newX;

                    if(difference != 0)
                    {
                        unroundedValue += difference;
                        var rounded = unroundedValue;
                        if(LabelDragValueRounding != null)
                        {
                            var isInt = Math.Abs(LabelDragValueRounding.Value - (int)LabelDragValueRounding.Value) < .0001m;

                            rounded = RoundDouble(unroundedValue, (double)LabelDragValueRounding.Value);

                            if(isInt)
                            {
                                rounded = (int)(System.Math.Round(rounded) + (System.Math.Sign(rounded) * .5f));
                            }
                        }

                        var getValueStatus = TryGetValueOnUi(out object valueOnInstance);

                        if(getValueStatus == ApplyValueResult.Success)
                        {
                            var newValue = mTextBoxLogic.GetValueInDirection(difference, rounded);
                            TrySetValueOnUi(newValue);
                            lastApplyValueResult = mTextBoxLogic.TryApplyToInstance(SetPropertyCommitType.Intermediate);
                        }
                    }
                }
                else
                {
                    if(System.Windows.Input.Mouse.Captured == Label)
                    {
                        System.Windows.Input.Mouse.Capture(null);
                    }
                }
            }
        }

        private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mTextBoxLogic.IsNumeric && currentDownX != 0 && EnableLabelDragValueChange &&
                // If the user changed the value with the mouse. Otherwise, it was a simple click
                pressedX != e.GetPosition(this).X)
            {
                lastApplyValueResult = mTextBoxLogic.TryApplyToInstance(SetPropertyCommitType.Full);

                pressedX = null;
                System.Windows.Input.Mouse.Capture(null);
            }
        }

        #endregion

        public double RoundDouble(double valueToRound, double multipleOf)
        {
            return ((int)(System.Math.Sign(valueToRound) * .5f + valueToRound / multipleOf)) * multipleOf;
        }

        private void NullableCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HandleNullableCheckBoxCheckChanged();
        }

        private void NullableCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var propertyType = this.GetPropertyType();

            if (propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) != null)
            {
                // For nullable value types
                var underlyingType = Nullable.GetUnderlyingType(propertyType);
                var value = Activator.CreateInstance(underlyingType);
                TrySetValueOnUi(value);
            }

            HandleNullableCheckBoxCheckChanged();
        }

        private void HandleNullableCheckBoxCheckChanged()
        {
            IsInSet = true;

            lastApplyValueResult = mTextBoxLogic.TryApplyToInstance();

            TryGetValueOnUi(out object newValue);

            RefreshIsEnabled(newValue, forceNullableEnable: NullableCheckBox.IsChecked == false);

            IsInSet = false;
        }
    }
}
