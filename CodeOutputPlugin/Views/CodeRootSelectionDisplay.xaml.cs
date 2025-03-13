using Gum.DataTypes;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ToolsUtilities;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace CodeOutputPlugin.Views;
/// <summary>
/// Interaction logic for CodeRootSelectionDisplay.xaml
/// </summary>
public partial class CodeRootSelectionDisplay : UserControl, IDataUi
{

    #region Fields/Properties

    InstanceMember mInstanceMember;

    public InstanceMember InstanceMember
    {
        get => mInstanceMember;
        set
        {
            mInstanceMember = value;

            Refresh();
        }
    }

    public bool SuppressSettingProperty{ get; set; }

    string selectedPath = string.Empty;

    FilePath _csprojDirectory;
    public FilePath? CsprojDirectory
    {
        get => _csprojDirectory;
        set
        {
            _csprojDirectory = value;
            RefreshDotDotbutton();
        }
    }



    #endregion

    public ApplyValueResult TryGetValueOnUi(out object result)
    {
        result = null;
        return ApplyValueResult.Success;
    }

    public ApplyValueResult TrySetValueOnUi(object value)
    {
        return ApplyValueResult.Success;
    }

    public CodeRootSelectionDisplay()
    {
        InitializeComponent();
    }

    public void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        this.Label.Text = InstanceMember.DisplayName;

        RefreshDotDotbutton();
    }

    private void RefreshDotDotbutton()
    {
        // This produced somewhat confusing text like "Set to ..\..\"
        // Paths are probably short enough so let's just show the full path:
        var gumProject = GumState.Self.ProjectState.ProjectDirectory;
        var relative = CsprojDirectory.RelativeTo(gumProject);
        this.UseDotDotPathButton.Content = $"Set to {relative}";
        //this.UseDotDotPathButton.Content = CsprojDirectory.FullPath;
    }

    private void RefreshAllContextMenus(bool force = false)
    {
        if (force)
        {
            //this.ForceRefreshContextMenu(TextBox.ContextMenu);
            //this.ForceRefreshContextMenu(StackPanel.ContextMenu);
        }
        else
        {
            //this.RefreshContextMenu(TextBox.ContextMenu);
            //this.RefreshContextMenu(StackPanel.ContextMenu);
        }
    }

    private void UseDotDotPathButton_Click(object sender, RoutedEventArgs e)
    {
        selectedPath = CsprojDirectory.FullPath;
        this.TrySetValueOnInstance(selectedPath);
    }

    private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
    {
        using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
        {
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if(result == System.Windows.Forms.DialogResult.OK)
            {
                selectedPath = dialog.SelectedPath;
                this.TrySetValueOnInstance(selectedPath);
            }
        }
    }
}
