using CodeOutputPlugin.Models;
using CodeOutputPlugin.ViewModels;
using Gum;
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
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace CodeOutputPlugin.Views
{
    /// <summary>
    /// Interaction logic for CodeWindow.xaml
    /// </summary>
    public partial class CodeWindow : UserControl
    {
        #region Fields/Properties

        CodeWindowViewModel ViewModel => DataContext as CodeWindowViewModel;

        public CodeOutputProjectSettings CodeOutputProjectSettings
        {
            get; set;
        }

        CodeOutputElementSettings codeOutputElementSettings;
        public CodeOutputElementSettings CodeOutputElementSettings
        {
            get => codeOutputElementSettings;
            set
            {
                codeOutputElementSettings = value;

                DataGrid.Instance = codeOutputElementSettings;

                CreateGridCategories();

            }
        }

        #endregion

        #region Events

        public event EventHandler CodeOutputSettingsPropertyChanged;
        public event EventHandler GenerateCodeClicked;

        #endregion

        public CodeWindow()
        {
            InitializeComponent();
            DataGrid.PropertyChange += HandleCodeOutputSettingsPropertyChanged;

            CreateGridCategories();
        }

        private void CreateGridCategories()
        {
            DataGrid.Categories.Clear();

            var projectCategory = new MemberCategory("Project-Wide Code Generation");

            projectCategory.Members.Add(CreateProjectUsingStatementsMember());

            DataGrid.Categories.Add(projectCategory);


            var elementCategory = new MemberCategory("Element Code Generation");

            elementCategory.Members.Add(CreateAutoGenerateOnChangeMember());
            elementCategory.Members.Add(CreateUsingStatementMember());
            elementCategory.Members.Add(CreateNamespaceMember());
            elementCategory.Members.Add(CreateFileLocationMember());

            DataGrid.Categories.Add(elementCategory);

        }

        private InstanceMember CreateAutoGenerateOnChangeMember()
        {
            var member = new InstanceMember("Auto-generate on change", this);

            member.CustomSetEvent += (owner, value) =>
            {
                if (codeOutputElementSettings != null)
                {
                    codeOutputElementSettings.AutoGenerateOnChange = (bool)value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) => codeOutputElementSettings?.AutoGenerateOnChange;
            member.CustomGetTypeEvent += (owner) => typeof(bool);

            return member;
        }

        private InstanceMember CreateProjectUsingStatementsMember()
        {
            var member = new InstanceMember("Project-wide Using Statements", this);

            member.CustomSetEvent += (owner, value) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.CommonUsingStatements = (string)value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) =>
            {
                return CodeOutputProjectSettings?.CommonUsingStatements;
            };

            member.CustomGetTypeEvent += (owner) => typeof(string);

            member.PreferredDisplayer = typeof(MultiLineTextBoxDisplay);

            return member;
        }

        private InstanceMember CreateUsingStatementMember()
        {
            var member = new InstanceMember("Using Statements", this);

            member.CustomSetEvent += (owner, value) =>
            {
                if(codeOutputElementSettings != null)
                {
                    codeOutputElementSettings.UsingStatements = (string)value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) =>
            {
                return codeOutputElementSettings?.UsingStatements;
            };

            member.CustomGetTypeEvent += (owner) => typeof(string);

            member.PreferredDisplayer = typeof(MultiLineTextBoxDisplay);

            return member;
        }

        private InstanceMember CreateNamespaceMember()
        {
            var member = new InstanceMember("Namespace", this);

            member.CustomSetEvent += (owner, value) =>
            {
                if (codeOutputElementSettings != null)
                {
                    codeOutputElementSettings.Namespace = (string)value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) => codeOutputElementSettings?.Namespace;

            member.CustomGetTypeEvent += (owner) =>
            {
                return typeof(string);
            };
            
            return member;
        }

        private InstanceMember CreateFileLocationMember()
        {
            var member = new InstanceMember("Generated File Name", this);

            member.CustomSetEvent += (owner, value) =>
            {
                if (codeOutputElementSettings != null)
                {
                    var valueAsString = (string)value;
                    if(!string.IsNullOrWhiteSpace(ProjectState.Self.ProjectDirectory) && FileManager.IsRelative(valueAsString) == false)
                    {
                        valueAsString = FileManager.MakeRelative(valueAsString, ProjectState.Self.ProjectDirectory, preserveCase:true);
                    }
                    codeOutputElementSettings.GeneratedFileName = valueAsString;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) => codeOutputElementSettings?.GeneratedFileName;

            member.CustomGetTypeEvent += (owner) => typeof(string);

            return member;
        }

        private void HandleCodeOutputSettingsPropertyChanged(string arg1, PropertyChangedArgs arg2)
        {
            CodeOutputSettingsPropertyChanged?.Invoke(this, null);
        }

        private void HandleGenerateCodeClicked(object sender, RoutedEventArgs e)
        {
            GenerateCodeClicked(this, null);
        }

        private void CopyButtonClicked(object sender, RoutedEventArgs e)
        {
            TextBoxInstance.Focus();
            TextBoxInstance.SelectAll();
            if (!string.IsNullOrEmpty(TextBoxInstance.Text))
            {
                // from: https://stackoverflow.com/questions/68666/clipbrd-e-cant-open-error-when-setting-the-clipboard-from-net
                for (int i = 0; i < 11; i++)
                {
                    try
                    {
                        Clipboard.SetText(TextBoxInstance.Text);
                        return;
                    }
                    catch { }
                    System.Threading.Thread.Sleep(15);
                }
            }
        }
    }
}
