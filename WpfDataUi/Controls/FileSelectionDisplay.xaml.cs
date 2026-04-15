using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls;

/// <summary>
/// Interaction logic for FileSelectionDisplay.xaml
/// </summary>
public partial class FileSelectionDisplay : UserControl, IDataUi
{
    #region Fields

    TextBoxDisplayLogic _textBoxLogic;
    FilePickingLogic _filePickingLogic;

    InstanceMember? _instanceMember;

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
            _textBoxLogic.InstanceMember = value;

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
            }

            Refresh();
        }
    }

    public bool SuppressSettingProperty { get; set; }

    /// <summary>
    /// Sets the filter used by the OpenFileDialog. Example: "Bitmap Font Generator Font|*.fnt"
    /// </summary>
    public string Filter
    {
        get => _filePickingLogic.Filter;
        set => _filePickingLogic.Filter = value;
    }

    public bool IsFolderDialog
    {
        get => _filePickingLogic.IsFolderDialog;
        set => _filePickingLogic.IsFolderDialog = value;
    }

    #endregion

    #region Methods

    public FileSelectionDisplay()
    {
        _filePickingLogic = new FilePickingLogic();

        InitializeComponent();

        _textBoxLogic = new TextBoxDisplayLogic(this, TextBox);

        RefreshAllContextMenus();
    }


    public void Refresh(bool forceRefreshEvenIfFocused = false)
    {

        SuppressSettingProperty = true;

        _textBoxLogic.RefreshDisplay(out object _);

        HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
        HintTextBlock.Text = InstanceMember?.DetailText;

        this.Label.Text = InstanceMember.DisplayName;

        RefreshAllContextMenus();
        RefreshViewInExplorerButton();
        RefreshIsEnabled();

        SuppressSettingProperty = false;
    }

    private void RefreshViewInExplorerButton()
    {
        ViewInExplorerButton.IsEnabled = !string.IsNullOrEmpty(InstanceMember.Value as string);
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
            this.ForceRefreshContextMenu(TextBox.ContextMenu);
            this.ForceRefreshContextMenu(Label.ContextMenu);
        }
        else
        {
            this.RefreshContextMenu(TextBox.ContextMenu);
            this.RefreshContextMenu(Label.ContextMenu);
        }
    }

    public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        this.TextBox.Text = valueOnInstance?.ToString();
        return ApplyValueResult.Success;
    }

    public ApplyValueResult TryGetValueOnUi(out object value)
    {
        return _textBoxLogic.TryGetValueOnUi(out value);
    }

    private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Value")
        {
            this.Refresh();
        }
    }


    private void TextBox_LostFocus_1(object? sender, RoutedEventArgs e)
    {
        if (this.TextBox.Text != _textBoxLogic.TextAtStartOfEditing)
        {
            var result = _textBoxLogic.TryApplyToInstance();

            if (result == ApplyValueResult.NotSupported)
            {
                this.IsEnabled = false;
            }

            RefreshViewInExplorerButton();
        }
    }


    private void Button_Click_1(object? sender, RoutedEventArgs e)
    {
        string? selected = _filePickingLogic.ShowOpenDialog();
        if (selected != null)
        {
            this.TextBox.Text = selected;
            _textBoxLogic.TryApplyToInstance();
        }
    }

    private void ViewInExplorerClicked(object? sender, RoutedEventArgs e)
    {
        _filePickingLogic.ShowInExplorer(this.TextBox.Text);
    }

    #endregion
}
