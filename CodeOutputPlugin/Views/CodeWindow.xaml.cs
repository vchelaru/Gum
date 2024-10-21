using CodeOutputPlugin.Models;
using CodeOutputPlugin.ViewModels;
using Gum;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
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
            elementCategory.Members.Add(CreateGenerateLocalizeMethod());

            DataGrid.Categories.Add(elementCategory);

        }

        #region Project-wide UI

        private void CreateProjectWideUi()
        {
            var projectCategory = new MemberCategory("Project-Wide Code Generation");
            projectCategory.Members.Add(CreateProjectTypeSelectionMember());
            projectCategory.Members.Add(CreateGenerateObjectInstantiationTypeMember());
            projectCategory.Members.Add(CreateProjectUsingStatementsMember());
            projectCategory.Members.Add(CreateCodeProjectRootMember());
            projectCategory.Members.Add(CreateRootNamespaceMember());
            projectCategory.Members.Add(CreateDefaultScreenBaseMember());
            projectCategory.Members.Add(CreateAdjustPixelValuesForDensityMember());
            projectCategory.Members.Add(CreateBaseTypesNotCodeGenerated());
            projectCategory.Members.Add(CreateGenerateGumDataTypesCode());
            DataGrid.Categories.Add(projectCategory);
        }

        private InstanceMember CreateProjectTypeSelectionMember()
        {
            var member = new InstanceMember("Output Library", this);

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.OutputLibrary = (OutputLibrary)args.Value;
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

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.CommonUsingStatements = (string)args.Value;
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

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    var valueToSet = (string)args.Value;
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

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if(CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.RootNamespace = (string)args.Value;
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
            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.DefaultScreenBase = (string)args.Value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) => CodeOutputProjectSettings?.DefaultScreenBase;
            member.CustomGetTypeEvent += (owner) => typeof(string);

            return member;
        }

        private InstanceMember CreateAdjustPixelValuesForDensityMember()
        {
            var member = new InstanceMember("Adjust Pixel Values for Density", this);
            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.AdjustPixelValuesForDensity = (bool)args.Value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };


            member.CustomGetEvent += (owner) => CodeOutputProjectSettings?.AdjustPixelValuesForDensity;
            member.CustomGetTypeEvent += (owner) => typeof(bool);

            return member;
        }

        private InstanceMember CreateBaseTypesNotCodeGenerated()
        {
            var member = new InstanceMember("Base types ignored in code generation", this);
            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.BaseTypesNotCodeGenerated = (string)args.Value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.PreferredDisplayer = typeof(MultiLineTextBoxDisplay);
            member.CustomGetEvent += (owner) => CodeOutputProjectSettings?.BaseTypesNotCodeGenerated;
            member.CustomGetTypeEvent += (owner) => typeof(string);

            return member;

        }

        private InstanceMember CreateGenerateGumDataTypesCode()
        {
            var member = new InstanceMember("Generate Gum DataTypes Code", this);
            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if(CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.GenerateGumDataTypes = (bool)args.Value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.PreferredDisplayer = typeof(CheckBoxDisplay);
            member.CustomGetEvent += (owner) => CodeOutputProjectSettings?.GenerateGumDataTypes ?? false;
            member.CustomGetTypeEvent += (owner) => typeof(string);

            return member;
        }

        private InstanceMember CreateGenerateObjectInstantiationTypeMember()
        {
            var member = new InstanceMember("Object Instantiation Type", this);
            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.ObjectInstantiationType = (ObjectInstantiationType)args.Value;
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) => CodeOutputProjectSettings?.ObjectInstantiationType;
            member.CustomGetTypeEvent += (owner) => typeof(ObjectInstantiationType);

            return member;
        }

        #endregion

        #region Current Element UI

        private InstanceMember CreateAutoGenerateOnChangeMember()
        {
            var member = new InstanceMember("Generation Behavior", this);

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (codeOutputElementSettings != null && args.Value != null)
                {
                    codeOutputElementSettings.GenerationBehavior = (GenerationBehavior)args.Value;
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

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if(codeOutputElementSettings != null)
                {
                    codeOutputElementSettings.UsingStatements = (string)args.Value;
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

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (codeOutputElementSettings != null)
                {
                    codeOutputElementSettings.Namespace = (string)args.Value;
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

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (codeOutputElementSettings != null)
                {
                    var valueAsString = (string)args.Value;
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

        private InstanceMember CreateGenerateLocalizeMethod()
        {
            var member = new InstanceMember("Localize Element", this);

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (codeOutputElementSettings != null)
                {
                    codeOutputElementSettings.LocalizeElement = (bool)args.Value;

                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) => codeOutputElementSettings?.LocalizeElement ?? false;

            member.CustomGetTypeEvent += (owner) => typeof(bool);

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
