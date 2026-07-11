using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Themes;
using WpfDataUi;
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

        private void CommitValue()
        {
            if (_isSyncingFromInstance)
            {
                return;
            }

            ApplyValueResult result = TryGetValueOnUi(out object? value);
            if (result == ApplyValueResult.Success && value != null)
            {
                this.TrySetValueOnInstance(value, SetPropertyCommitType.Full);
            }
        }

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
