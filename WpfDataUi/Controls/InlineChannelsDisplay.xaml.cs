using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Generic displayer for a <see cref="CompositeInstanceMember"/>: one equal-width labeled numeric
    /// <see cref="TextBox"/> per <see cref="CompositeInstanceMember.ChannelMembers"/> entry, laid out
    /// horizontally. Each field commits directly to its own channel member on edit rather than composing
    /// all fields into one value first, so this control never needs to know the composite's concrete
    /// type (Vector2, Vector3, ...) - only the descriptor that registers it does. Domain-agnostic sibling
    /// of the bespoke single-purpose composite displayers (e.g. a color swatch or a corner-radius link
    /// toggle), which do need to know their composite type because they render more than N plain fields.
    /// </summary>
    public partial class InlineChannelsDisplay : UserControl, IDataUi
    {
        private readonly List<(TextBox TextBox, InstanceMember Channel)> _fields = new();
        private InstanceMember? _instanceMember;
        private bool _isSyncingFromInstance;

        public InstanceMember? InstanceMember
        {
            get => _instanceMember;
            set
            {
                bool valueChanged = _instanceMember != value;

                if (_instanceMember != null && valueChanged)
                {
                    _instanceMember.PropertyChanged -= HandlePropertyChange;
                }
                _instanceMember = value;
                if (_instanceMember != null && valueChanged)
                {
                    _instanceMember.PropertyChanged += HandlePropertyChange;
                }

                if (valueChanged)
                {
                    RebuildFields();
                }

                Refresh();
            }
        }

        public bool SuppressSettingProperty { get; set; }

        public InlineChannelsDisplay()
        {
            InitializeComponent();
        }

        private void RebuildFields()
        {
            FieldsPanel.Children.Clear();
            _fields.Clear();

            if (InstanceMember is not CompositeInstanceMember composite)
            {
                return;
            }

            FieldsPanel.Columns = composite.ChannelMembers.Count;

            foreach (InstanceMember channel in composite.ChannelMembers)
            {
                StackPanel fieldPanel = new() { Margin = new Thickness(0, 0, 4, 0) };

                TextBlock label = new()
                {
                    Text = channel.DisplayName,
                    FontSize = 10,
                    Margin = new Thickness(0, 0, 0, 2)
                };

                TextBox textBox = new();
                textBox.LostFocus += (_, _) => Commit(channel, textBox);
                textBox.KeyDown += (_, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        Commit(channel, textBox);
                        e.Handled = true;
                    }
                };

                fieldPanel.Children.Add(label);
                fieldPanel.Children.Add(textBox);
                FieldsPanel.Children.Add(fieldPanel);

                _fields.Add((textBox, channel));
            }
        }

        private void Commit(InstanceMember channel, TextBox textBox)
        {
            if (_isSyncingFromInstance || SuppressSettingProperty)
            {
                return;
            }

            if (TextBoxDisplayLogic.TryParseNumeric(textBox.Text, channel.PropertyType, out object parsed))
            {
                channel.SetValue(parsed, SetPropertyCommitType.Full);
                channel.CallAfterSetByUi();
            }
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            if (InstanceMember is not CompositeInstanceMember composite)
            {
                return;
            }

            Label.Content = InstanceMember.DisplayName;
            IsEnabled = !InstanceMember.IsReadOnly;
            RefreshHintText();

            _isSyncingFromInstance = true;
            try
            {
                InlineChannelFieldState[] states = InlineChannelsDisplayLogic.BuildFieldStates(composite.ChannelMembers);
                for (int i = 0; i < _fields.Count; i++)
                {
                    (TextBox textBox, InstanceMember channel) = _fields[i];
                    if (forceRefreshEvenIfFocused || !textBox.IsKeyboardFocused)
                    {
                        textBox.Text = states[i].Text;
                    }
                    textBox.IsEnabled = !channel.IsReadOnly;
                    ApplyBackground(textBox, states[i]);
                }
            }
            finally
            {
                _isSyncingFromInstance = false;
            }

            this.RefreshContextMenu(MainGrid.ContextMenu);
        }

        /// <summary>
        /// Deferred like <c>CornerRadiusDisplay.RefreshBackgrounds</c>: <see cref="DataUiGrid.OverridesIsDefaultStylingProperty"/>
        /// is an inherited attached property, and a pooled control isn't necessarily re-parented into the
        /// grid's visual tree yet when Refresh runs, so reading it synchronously here can see the
        /// pre-inheritance default (false).
        /// </summary>
        private void ApplyBackground(TextBox textBox, InlineChannelFieldState state)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (DataUiGrid.GetOverridesIsDefaultStyling(textBox))
                {
                    return;
                }

                if (state.IsDefault)
                {
                    textBox.Background = TextBoxDisplayLogic.DefaultValueBackground;
                }
                else if (state.IsIndeterminate)
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
            });
        }

        private void RefreshHintText()
        {
            string? detailText = InstanceMember?.DetailText;
            HintTextBlock.Text = detailText;
            HintTextBlock.Visibility = string.IsNullOrEmpty(detailText) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Unused: fields commit directly to their own channel member (see <see cref="Commit"/>), not
        /// through a single composed value - see the class summary for why.
        /// </summary>
        public ApplyValueResult TrySetValueOnUi(object valueOnInstance) => ApplyValueResult.NotSupported;

        /// <inheritdoc cref="TrySetValueOnUi"/>
        public ApplyValueResult TryGetValueOnUi(out object? value)
        {
            value = null;
            return ApplyValueResult.NotSupported;
        }

        private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value) || e.PropertyName == nameof(InstanceMember.DetailText))
            {
                Refresh();
            }
        }
    }
}
