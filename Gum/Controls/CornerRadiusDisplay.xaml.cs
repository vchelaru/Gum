using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Themes;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Controls.DataUi
{
    /// <summary>
    /// Interaction logic for CornerRadiusDisplay.xaml. Displayer for the Rectangle corner-radius
    /// composite (see <see cref="CompositeMemberRegistry"/>): a single uniform field while linked
    /// (the common case, unchanged from before per-corner overrides existed), or four independent
    /// per-corner fields once unlinked via the chain toggle button.
    /// </summary>
    public partial class CornerRadiusDisplay : UserControl, IDataUi
    {
        InstanceMember? mInstanceMember;
        bool _isSyncingFromInstance;
        bool _isLinked = true;
        CornerRadiusComposite _current;

        readonly Dictionary<FrameworkElement, TextBox> _labelDragTargets;
        FrameworkElement? _draggingLabel;
        TextBox? _draggingTextBox;
        double? _dragCurrentX;
        double? _dragPressedX;
        double _dragUnroundedValue;

        public InstanceMember? InstanceMember
        {
            get => mInstanceMember;
            set
            {
                bool valueChanged = mInstanceMember != value;

                if (mInstanceMember != null && valueChanged)
                {
                    mInstanceMember.PropertyChanged -= HandlePropertyChange;
                }
                mInstanceMember = value;
                if (mInstanceMember != null && valueChanged)
                {
                    mInstanceMember.PropertyChanged += HandlePropertyChange;
                }

                Refresh();
            }
        }

        public bool SuppressSettingProperty { get; set; }

        public CornerRadiusDisplay()
        {
            InitializeComponent();

            _labelDragTargets = new Dictionary<FrameworkElement, TextBox>
            {
                [Label] = UniformTextBox,
                [TopLeftLabel] = TopLeftTextBox,
                [TopRightLabel] = TopRightTextBox,
                [BottomLeftLabel] = BottomLeftTextBox,
                [BottomRightLabel] = BottomRightTextBox,
            };
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            if (InstanceMember == null)
            {
                return;
            }

            bool successfulGet = this.TryGetValueOnInstance(out object valueOnInstance);
            if (successfulGet && valueOnInstance != null)
            {
                TrySetValueOnUi(valueOnInstance);
            }

            Label.Content = InstanceMember.DisplayName;
            IsEnabled = !InstanceMember.IsReadOnly;
            RefreshHintText();
            RefreshBackgrounds();
            this.RefreshContextMenu(MainGrid.ContextMenu);
        }

        /// <summary>
        /// Tints each visible field by whether its own channel is at its default (inherited) value,
        /// matching the green/gray convention TextBoxDisplayLogic uses elsewhere in the grid. Linked
        /// mode only shows the uniform field, so only the CornerRadius channel's own default state
        /// is relevant there - the hidden per-corner overrides aren't represented by any visible
        /// field and would only confuse a single shared color. When InstanceMember isn't a plain
        /// CompositeInstanceMember (e.g. a multi-select wrapper), fall back to its own aggregate
        /// IsDefault/IsIndeterminate for every field - less precise, but still a signal.
        /// </summary>
        private void RefreshBackgrounds()
        {
            if (InstanceMember == null)
            {
                return;
            }

            if (InstanceMember is CompositeInstanceMember composite && composite.ChannelMembers.Count == 5)
            {
                ApplyBackground(UniformTextBox, composite.ChannelMembers[0]);
                ApplyBackground(TopLeftTextBox, composite.ChannelMembers[1]);
                ApplyBackground(TopRightTextBox, composite.ChannelMembers[2]);
                ApplyBackground(BottomLeftTextBox, composite.ChannelMembers[3]);
                ApplyBackground(BottomRightTextBox, composite.ChannelMembers[4]);
            }
            else
            {
                ApplyBackground(UniformTextBox, InstanceMember);
                ApplyBackground(TopLeftTextBox, InstanceMember);
                ApplyBackground(TopRightTextBox, InstanceMember);
                ApplyBackground(BottomLeftTextBox, InstanceMember);
                ApplyBackground(BottomRightTextBox, InstanceMember);
            }
        }

        private static void ApplyBackground(TextBox textBox, InstanceMember member)
        {
            // The DataUiGrid style sets this attached property so rows rely on the per-row
            // "is edited" icon (Frb.Styles.Defaults.xaml's IsDefaultIcon) instead of a
            // displayer-painted background - see TextBoxDisplayLogic.RefreshBackgroundColor
            // for the reference check every other displayer already honors.
            if (DataUiGrid.GetOverridesIsDefaultStyling(textBox))
            {
                return;
            }

            if (member.IsDefault)
            {
                textBox.Background = TextBoxDisplayLogic.DefaultValueBackground;
            }
            else if (member.IsIndeterminate)
            {
                textBox.Background = TextBoxDisplayLogic.IndeterminateValueBackground;
            }
            else if (textBox.TryFindResource("Frb.Brushes.Field.Background") is Brush themed)
            {
                textBox.Background = themed;
            }
            else
            {
                textBox.ClearValue(TextBox.BackgroundProperty);
            }
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            if (valueOnInstance is not CornerRadiusComposite composite)
            {
                return ApplyValueResult.NotSupported;
            }

            _isSyncingFromInstance = true;
            try
            {
                _current = composite;
                UniformTextBox.Text = FormatFloat(composite.Uniform);
                TopLeftTextBox.Text = FormatNullableFloat(composite.TopLeft);
                TopRightTextBox.Text = FormatNullableFloat(composite.TopRight);
                BottomLeftTextBox.Text = FormatNullableFloat(composite.BottomLeft);
                BottomRightTextBox.Text = FormatNullableFloat(composite.BottomRight);
                SetLinkedState(composite.IsLinked);
            }
            finally
            {
                _isSyncingFromInstance = false;
            }

            return ApplyValueResult.Success;
        }

        public ApplyValueResult TryGetValueOnUi(out object? value)
        {
            float uniform = ParseFloat(UniformTextBox.Text) ?? _current.Uniform;

            value = _isLinked
                ? new CornerRadiusComposite(uniform, null, null, null, null)
                : new CornerRadiusComposite(
                    uniform,
                    ParseFloat(TopLeftTextBox.Text),
                    ParseFloat(TopRightTextBox.Text),
                    ParseFloat(BottomLeftTextBox.Text),
                    ParseFloat(BottomRightTextBox.Text));

            return ApplyValueResult.Success;
        }

        private void SetLinkedState(bool isLinked)
        {
            _isLinked = isLinked;
            UniformTextBox.Visibility = isLinked ? Visibility.Visible : Visibility.Collapsed;
            CornersGrid.Visibility = isLinked ? Visibility.Collapsed : Visibility.Visible;
            LinkIcon.Icon = isLinked ? GumIconKind.ChainLinked : GumIconKind.ChainUnlinked;
            LinkToggleButton.ToolTip = isLinked
                ? "Linked - all corners share Corner Radius. Click to set corners independently."
                : "Unlinked - click to use one Corner Radius for all corners again.";
        }

        private void LinkToggleButton_Click(object sender, RoutedEventArgs e)
        {
            bool wasLinked = _isLinked;
            SetLinkedState(!wasLinked);

            if (wasLinked)
            {
                // Unlinking: seed the four fields with the current uniform value so the visual
                // radius doesn't change until the user edits an individual corner.
                string uniformText = UniformTextBox.Text;
                TopLeftTextBox.Text = uniformText;
                TopRightTextBox.Text = uniformText;
                BottomLeftTextBox.Text = uniformText;
                BottomRightTextBox.Text = uniformText;
            }

            CommitValue();
        }

        private void UniformTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitValue();
                e.Handled = true;
            }
        }

        private void UniformTextBox_LostFocus(object sender, RoutedEventArgs e) => CommitValue();

        private void CornerTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitValue();
                e.Handled = true;
            }
        }

        private void CornerTextBox_LostFocus(object sender, RoutedEventArgs e) => CommitValue();

        private void CommitValue(SetPropertyCommitType commitType = SetPropertyCommitType.Full)
        {
            if (_isSyncingFromInstance)
            {
                return;
            }

            ApplyValueResult result = TryGetValueOnUi(out object? value);
            if (result == ApplyValueResult.Success && value != null)
            {
                this.TrySetValueOnInstance(value, commitType);
            }
        }

        #region Label Dragging

        /// <summary>
        /// Click-and-drag over a field's label to scrub its numeric value, mirroring
        /// <see cref="TextBoxDisplay"/>'s label-drag gesture. Applies to the uniform field's shared
        /// <see cref="Label"/> and each per-corner TL/TR/BL/BR label via <see cref="_labelDragTargets"/>.
        /// </summary>
        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement label || !_labelDragTargets.TryGetValue(label, out TextBox target))
            {
                return;
            }

            _draggingLabel = label;
            _draggingTextBox = target;
            _dragCurrentX = e.GetPosition(this).X;
            _dragPressedX = _dragCurrentX;

            Mouse.Capture(label);

            _dragUnroundedValue = ParseFloat(target.Text) ?? _current.Uniform;
        }

        private void Label_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragCurrentX == null || _draggingTextBox == null)
            {
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double newX = e.GetPosition(this).X;
                double difference = newX - _dragCurrentX.Value;
                _dragCurrentX = newX;

                if (difference != 0)
                {
                    _dragUnroundedValue += difference;
                    double rounded = TextBoxDisplayLogic.SnapDraggedValue(_dragUnroundedValue, rounding: 1);

                    // Stick visibly at the 0 floor while scrubbing (matching TextBoxDisplay's
                    // min/max handling) instead of showing a negative value that only snaps back
                    // to 0 once the commit round-trips through Decompose's clamp.
                    rounded = (double)TextBoxDisplayLogic.ClampToRange(rounded, min: 0m, max: null);

                    _draggingTextBox.Text = FormatFloat((float)rounded);
                    CommitValue(SetPropertyCommitType.Intermediate);
                }
            }
            else if (Mouse.Captured == _draggingLabel)
            {
                Mouse.Capture(null);
            }
        }

        private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingTextBox != null && _dragPressedX != e.GetPosition(this).X)
            {
                CommitValue(SetPropertyCommitType.Full);
            }

            _draggingLabel = null;
            _draggingTextBox = null;
            _dragCurrentX = null;
            _dragPressedX = null;
            Mouse.Capture(null);
        }

        #endregion

        private static string FormatFloat(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        private static string FormatNullableFloat(float? value) =>
            value == null ? string.Empty : FormatFloat(value.Value);

        private static float? ParseFloat(string text) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : null;

        private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value" || e.PropertyName == nameof(InstanceMember.DetailText))
            {
                Refresh();
            }
        }

        private void RefreshHintText()
        {
            string? detailText = InstanceMember?.DetailText;
            HintTextBlock.Text = detailText;
            HintTextBlock.Visibility = string.IsNullOrEmpty(detailText) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
