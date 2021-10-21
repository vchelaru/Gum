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
        public event EventHandler GenerateAllCodeClicked;

        #endregion

        public CodeWindow()
        {
            InitializeComponent();
            DataGrid.PropertyChange += (not, used) => CodeOutputSettingsPropertyChanged?.Invoke(this, null);

            CreateGridCategories();
        }

        private void CreateGridCategories()
        {
            DataGrid.Categories.Clear();

            CreateProjectWideUi();

            var elementCategory = new MemberCategory("Element Code Generation");

            elementCategory.Members.Add(CreateAutoGenerateOnChangeMember());
            elementCategory.Members.Add(CreateUsingStatementMember());
            elementCategory.Members.Add(CreateNamespaceMember());
            elementCategory.Members.Add(CreateFileLocationMember());

            DataGrid.Categories.Add(elementCategory);

        }

        #region Project-wide UI

        private void CreateProjectWideUi()
        {
            var projectCategory = new MemberCategory("Project-Wide Code Generation");
            projectCategory.Members.Add(CreateProjectTypeSelectionMember());
            projectCategory.Members.Add(CreateProjectUsingStatementsMember());
            projectCategory.Members.Add(CreateCodeProjectRootMember());
            projectCategory.Members.Add(CreateRootNamespaceMember());
            projectCategory.Members.Add(CreateDefaultScreenBaseMember());
            DataGrid.Categories.Add(projectCategory);
        }

        private InstanceMember CreateProjectTypeSelectionMember()
        {
            var member = new InstanceMember("Output Library", this);

            member.CustomSetEvent += (owner, value) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.OutputLibrary = (OutputLibrary)value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) => CodeOutputProjectSettings?.OutputLibrary;



            var optionsArray = Enum.GetValues(typeof(OutputLibrary));
            List<object> options = new List<object>();
            foreach(var option in optionsArray)
            {
                options.Add(option);
            }
            member.CustomOptions = options;


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

            member.CustomGetEvent += (owner) => CodeOutputProjectSettings?.CommonUsingStatements;
            member.CustomGetTypeEvent += (owner) => typeof(string);
            member.PreferredDisplayer = typeof(MultiLineTextBoxDisplay);

            return member;
        }

        private InstanceMember CreateCodeProjectRootMember()
        {
            var member = new InstanceMember("Code Project Root", this);

            member.CustomSetEvent += (owner, value) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    var valueToSet = (string)value;
                    var needsAppendedSlash = !string.IsNullOrEmpty(valueToSet) &&
                        !valueToSet.EndsWith("\\") &&
                        !valueToSet.EndsWith("/");
                    if (needsAppendedSlash)
                    {
                        valueToSet += "\\";
                    }

                    if(!string.IsNullOrWhiteSpace(valueToSet) && FileManager.IsRelative(valueToSet) == false)
                    {
                        valueToSet = FileManager.MakeRelative(valueToSet, GumState.Self.ProjectState.ProjectDirectory, preserveCase:true);
                    }
                    CodeOutputProjectSettings.CodeProjectRoot = valueToSet;

                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) =>
            {
                if (CodeOutputProjectSettings?.CodeProjectRoot != null && FileManager.IsRelative(CodeOutputProjectSettings?.CodeProjectRoot))
                {
                    return FileManager.RemoveDotDotSlash( GumState.Self.ProjectState.ProjectDirectory + CodeOutputProjectSettings?.CodeProjectRoot);
                }
                else
                {
                    return CodeOutputProjectSettings?.CodeProjectRoot;
                }
            };
            member.CustomGetTypeEvent += (owner) => typeof(string);
            // Don't use a FileSelectionDisplay since it currently only supports
            // selecting files, and we want to select a folder. Maybe at some point 
            // in the future this could have a property for selecting folder, but until then....
            //member.PreferredDisplayer = typeof(FileSelectionDisplay);

            return member;
        }

        private InstanceMember CreateRootNamespaceMember()
        {
            var member = new InstanceMember("Root Namespace", this);

            member.CustomSetEvent += (owner, value) =>
            {
                if(CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.RootNamespace = (string)value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) => CodeOutputProjectSettings?.RootNamespace;
            member.CustomGetTypeEvent += (owner) => typeof(string);

            return member;
        }

        private InstanceMember CreateDefaultScreenBaseMember()
        {

            var member = new InstanceMember("Default Screen Base", this);
            member.DetailText = "Base class for screens";
            member.CustomSetEvent += (owner, value) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.DefaultScreenBase = (string)value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) => CodeOutputProjectSettings?.DefaultScreenBase;
            member.CustomGetTypeEvent += (owner) => typeof(string);

            return member;

            
        }

        #endregion

        #region Current Element UI

        private InstanceMember CreateAutoGenerateOnChangeMember()
        {
            var member = new InstanceMember("Generation Behavior", this);

            member.CustomSetEvent += (owner, value) =>
            {
                if (codeOutputElementSettings != null && value != null)
                {
                    codeOutputElementSettings.GenerationBehavior = (GenerationBehavior)value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) => codeOutputElementSettings?.GenerationBehavior;
            member.CustomGetTypeEvent += (owner) => typeof(GenerationBehavior);

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

        #endregion

        #region Button Event Handlers

        private void HandleGenerateCodeClicked(object sender, RoutedEventArgs e)
        {
            GenerateCodeClicked(this, null);
        }

        private void HandleGenerateAllCodeClicked(object sender, RoutedEventArgs e)
        {
            GenerateAllCodeClicked(this, null);
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

        #endregion
    }
}
