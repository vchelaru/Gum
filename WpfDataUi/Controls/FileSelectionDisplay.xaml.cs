using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
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

    TextBoxDisplayLogic mTextBoxLogic;

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
            mTextBoxLogic.InstanceMember = value;

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
            //if (mInstanceMember != null)
            //{
            //    mInstanceMember.DebugInformation = "TextBoxDisplay " + mInstanceMember.Name;
            //}


            Refresh();
        }
    }

    public bool SuppressSettingProperty { get; set; }

    /// <summary>
    /// Sets the filter used by the OpenFileDialog. Example: "Bitmap Font Generator Font|*.fnt"
    /// </summary>
    public string Filter
    {
        get; set;
    } = string.Empty;

    public static string FolderRelativeTo { get; set; }

    public bool IsFolderDialog { get; set; }

    #endregion

    #region Methods

    public FileSelectionDisplay()
    {
        InitializeComponent();


        mTextBoxLogic = new TextBoxDisplayLogic(this, TextBox);

        RefreshAllContextMenus();
    }


    public void Refresh(bool forceRefreshEvenIfFocused = false)
    {

        SuppressSettingProperty = true;

        mTextBoxLogic.RefreshDisplay(out object _);

        HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
        HintTextBlock.Text = InstanceMember?.DetailText;

        this.Label.Text = InstanceMember.DisplayName;

        RefreshAllContextMenus();
        RefreshViewInExplorerButton();

        SuppressSettingProperty = false;
    }

    private void RefreshViewInExplorerButton()
    {
        ViewInExplorerButton.IsEnabled = !string.IsNullOrEmpty(InstanceMember.Value as string);
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
        return mTextBoxLogic.TryGetValueOnUi(out value);
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
        if (this.TextBox.Text != mTextBoxLogic.TextAtStartOfEditing)
        {
            var result = mTextBoxLogic.TryApplyToInstance();

            if (result == ApplyValueResult.NotSupported)
            {
                this.IsEnabled = false;
            }

            RefreshViewInExplorerButton();
        }
    }


    private void Button_Click_1(object? sender, RoutedEventArgs e)
    {
        if (IsFolderDialog)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            var result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                this.TextBox.Text = fbd.SelectedPath;
                mTextBoxLogic.TryApplyToInstance();
            }
        }
        else
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Filter = Filter;

            var shouldOpen = fileDialog.ShowDialog();

            if (shouldOpen.HasValue && shouldOpen.Value)
            {
                string file = fileDialog.FileName;
                this.TextBox.Text = file;
                mTextBoxLogic.TryApplyToInstance();
            }

        }
    }

    private void ViewInExplorerClicked(object? sender, RoutedEventArgs e)
    {
        var fileToOpen = this.TextBox.Text;

        if (!string.IsNullOrEmpty(fileToOpen))
        {
            if (!string.IsNullOrEmpty(FolderRelativeTo))
            {
                fileToOpen = RemoveDotDotSlash(
                    FolderRelativeTo + fileToOpen)
                    .Replace("/", "\\");


            }

            if (System.IO.File.Exists(fileToOpen))
            {
                Process.Start("explorer.exe", "/select," + fileToOpen);
            }
        }
    }

    private string RemoveDotDotSlash(string fileNameToFix)
    {
        if (fileNameToFix.Contains(".."))
        {
            fileNameToFix = fileNameToFix.Replace("\\", "/");

            // First let's get rid of any ..'s that are in the middle
            // for example:
            //
            // "content/zones/area1/../../background/outdoorsanim/outdoorsanim.achx"
            //
            // would become
            // 
            // "content/background/outdoorsanim/outdoorsanim.achx"

            int indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");

            bool shouldLoop = indexOfNextDotDotSlash > 0;

            while (shouldLoop)
            {
                int indexOfPreviousDirectory = fileNameToFix.LastIndexOf('/', indexOfNextDotDotSlash - 2, indexOfNextDotDotSlash - 2);

                fileNameToFix = fileNameToFix.Remove(indexOfPreviousDirectory + 1, indexOfNextDotDotSlash - indexOfPreviousDirectory + 2);

                indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");

                shouldLoop = indexOfNextDotDotSlash > 0;
            }
        }

        return fileNameToFix.Replace("\\", "/");
    }

    #endregion

}
