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
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Interaction logic for TextBoxDisplay.xaml
    /// </summary>
    public partial class TextBoxDisplay : UserControl, IDataUi, ISetDefaultable
    {
        #region Fields

        TextBoxDisplayLogic mTextBoxLogic;

        InstanceMember mInstanceMember;

        ApplyValueResult? lastApplyValueResult = null;

        public decimal? LabelDragValueRounding { get; set; } = 1;

        #endregion

        #region Properties

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


                //if (mInstanceMember != null)
                //{
                //    mInstanceMember.DebugInformation = "TextBoxDisplay " + mInstanceMember.Name;
                //}


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

            this.RefreshContextMenu(TextBox.ContextMenu);
            this.RefreshContextMenu(StackPanel.ContextMenu);
            this.ContextMenu = TextBox.ContextMenu;
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            // If the user is editing a value, we don't want to change
            // the value under the cursor
            // If we're default, then go ahead and change the value
            bool canRefresh =
                this.TextBox.IsFocused == false || forceRefreshEvenIfFocused || mTextBoxLogic.InstanceMember.IsDefault;

            if (canRefresh)
            {
                SuppressSettingProperty = true;

                mTextBoxLogic.RefreshDisplay();

                this.Label.Text = InstanceMember.DisplayName;
                this.RefreshContextMenu(TextBox.ContextMenu);
                this.RefreshContextMenu(StackPanel.ContextMenu);

                HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
                HintTextBlock.Text = InstanceMember?.DetailText;

                RefreshIsEnabled();

                SuppressSettingProperty = false;

                if(mTextBoxLogic.IsNumeric)
                {
                    this.Label.Cursor = Cursors.ScrollWE;
                }
                else
                {
                    this.Label.Cursor = null;
                }

            }
        }

        public virtual ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            this.TextBox.Text = mTextBoxLogic.ConvertNumberToString(valueOnInstance);

            RefreshPlaceholderText();

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
            return mTextBoxLogic.TryGetValueOnUi(out value);
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();

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

            RefreshIsEnabled();
        }

        private void RefreshIsEnabled()
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
                this.IsEnabled = true;
            }
        }

        public void SetToDefault()
        {
            // So we don't exlicitly set values when losing focus
            this.mTextBoxLogic.HasUserChangedAnything = false;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            RefreshPlaceholderText();
        }

        double? currentDownX;
        private double unroundedValue;

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mTextBoxLogic.IsNumeric)
            {
                currentDownX = e.GetPosition(this).X;

                System.Windows.Input.Mouse.Capture(Label);

                var getValueStatus = TryGetValueOnUi(out object valueOnInstance);

                if (getValueStatus == ApplyValueResult.Success)
                {
                    var converter = TypeDescriptor.GetConverter(mTextBoxLogic.InstancePropertyType);
                    unroundedValue = (double)converter.ConvertTo(valueOnInstance, typeof(double));
                }
            }
        }

        private void Label_MouseMove(object sender, MouseEventArgs e)
        {
            if(currentDownX != null)
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
        }

        private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mTextBoxLogic.IsNumeric && currentDownX != 0)
            {
                lastApplyValueResult = mTextBoxLogic.TryApplyToInstance(SetPropertyCommitType.Full);

                currentDownX = null;
                System.Windows.Input.Mouse.Capture(null);
            }
        }

        public double RoundDouble(double valueToRound, double multipleOf)
        {
            return ((int)(System.Math.Sign(valueToRound) * .5f + valueToRound / multipleOf)) * multipleOf;
        }
    }
}
