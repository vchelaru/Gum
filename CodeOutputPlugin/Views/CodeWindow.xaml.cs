using CodeOutputPlugin.Models;
using CodeOutputPlugin.ViewModels;
using Gum;
using Gum.Mvvm;
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

        bool HasClickedManualSetup;

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

                FullRefreshDataGrid();

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

            FullRefreshDataGrid();
        }


        private void FullRefreshDataGrid()
        {
            DataGrid.Categories.Clear();

            CreateProjectWideUi();

            var elementCategory = new MemberCategory("Element Code Generation");

            elementCategory.Members.Add(CreateAutoGenerateOnChangeMember());
            elementCategory.Members.Add(CreateUsingStatementMember());
            elementCategory.Members.Add(CreateNamespaceMember());
            elementCategory.Members.Add(CreateFileLocationMember());
            elementCategory.Members.Add(CreateGenerateLocalizeMethod());

            GenerateCodeButton.Visibility = this.AutoAndManualButtonStack.Visibility == Visibility.Visible 
				? Visibility.Collapsed 
				: Visibility.Visible;

            DataGrid.Categories.Add(elementCategory);
        }

        #region Project-wide UI

        private void CreateProjectWideUi()
        {
            var projectCategory = new MemberCategory("Project-Wide Code Generation");
            projectCategory.Members.Add(CreateCodeProjectRootMember());
            projectCategory.Members.Add(CreateOutputLibrarySelectionMember());
            projectCategory.Members.Add(CreateGenerateObjectInstantiationTypeMember());
            projectCategory.Members.Add(CreateProjectUsingStatementsMember());
            projectCategory.Members.Add(CreateRootNamespaceMember());
            projectCategory.Members.Add(CreateDefaultScreenBaseMember());

            var createAdjustPixelValues =
                CodeOutputProjectSettings?.OutputLibrary == OutputLibrary.XamarinForms ||
                CodeOutputProjectSettings?.OutputLibrary == OutputLibrary.WPF ||
                CodeOutputProjectSettings?.OutputLibrary == OutputLibrary.Maui;
            if(createAdjustPixelValues)
            {
                projectCategory.Members.Add(CreateAdjustPixelValuesForDensityMember());
                projectCategory.Members.Add(CreateBaseTypesNotCodeGenerated());
                // Not sure if this should be here or not...
                projectCategory.Members.Add(CreateGenerateGumDataTypesCode());
            }

            DataGrid.Categories.Add(projectCategory);
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
                        var projectDirectory = GumState.Self.ProjectState.ProjectDirectory;
                        valueToSet = FileManager.MakeRelative(valueToSet, projectDirectory, preserveCase:true);

                        if(string.IsNullOrEmpty(valueToSet))
                        {
                            valueToSet = "./";
                        }
                    }

                    var wasOldempty = string.IsNullOrEmpty(CodeOutputProjectSettings.CodeProjectRoot);

                    CodeOutputProjectSettings.CodeProjectRoot = valueToSet;

                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);

                }
            };

            member.CustomGetEvent += (owner) =>
            {
                var projectRoot = CodeOutputProjectSettings?.CodeProjectRoot;
                if (string.IsNullOrEmpty(projectRoot))
                {
                    return String.Empty;
                }
                else if(projectRoot == "./")
                {
                    return GumState.Self.ProjectState.ProjectDirectory;
                }
                else if (projectRoot != null && FileManager.IsRelative(projectRoot))
                {
                    return FileManager.RemoveDotDotSlash( GumState.Self.ProjectState.ProjectDirectory + projectRoot);
                }
                else
                {
                    return projectRoot;
                }
            };
            member.CustomGetTypeEvent += (owner) => typeof(string);
            // Don't use a FileSelectionDisplay since it currently only supports
            // selecting files, and we want to select a folder. Maybe at some point 
            // in the future this could have a property for selecting folder, but until then....
            //member.PreferredDisplayer = typeof(FileSelectionDisplay);

            //var value = member.Value as string;
            //if(string.IsNullOrEmpty(value))
            //{
            //    // let's see if we have a csproj:
            var csproj = CodeRootSelectionDisplay.GetCsprojDirectory();

            var areSetupButtonsVisible = 
                csproj != null && 
                string.IsNullOrEmpty(CodeOutputProjectSettings.CodeProjectRoot) &&
                !HasClickedManualSetup;

            this.AutoAndManualButtonStack.Visibility = areSetupButtonsVisible.ToVisibility();
            this.FullGenerationUiStack.Visibility = (!areSetupButtonsVisible).ToVisibility();

            return member;
        }


        private InstanceMember CreateOutputLibrarySelectionMember()
        {
            var member = new InstanceMember("Output Library", this);

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    CodeOutputProjectSettings.OutputLibrary = (OutputLibrary)args.Value;

                    // Check for required using statements:
                    if(CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGame)
                    {
                        // This should have MonoGameGum.GueDeriving
                        if(CodeOutputProjectSettings.CommonUsingStatements.Contains("using MonoGameGum.GueDeriving;")== false)
                        {
                            CodeOutputProjectSettings.CommonUsingStatements += "\nusing MonoGameGum.GueDeriving;";
                        }
                    }
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);

                    FullRefreshDataGrid();
                }
            };

            member.CustomGetEvent += (owner) => CodeOutputProjectSettings?.OutputLibrary;



            var optionsArray = Enum.GetValues(typeof(OutputLibrary));
            List<object> options = new List<object>();
            //foreach(var option in optionsArray)
            //{
            //    options.Add(option);
            //}

            options.Add(OutputLibrary.MonoGame);
            options.Add(OutputLibrary.Skia);

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

            const string FullyInCode = "Fully in Code (no loaded Gum Project)";
            const string ReferenceGum = "Reference loaded Gum Project";

            member.CustomSetPropertyEvent += (owner, args) =>
            {
                if (CodeOutputProjectSettings != null)
                {
                    if((args.Value as string) == FullyInCode)
                    {
                        CodeOutputProjectSettings.ObjectInstantiationType = ObjectInstantiationType.FullyInCode;
                    }
                    else
                    {
                        CodeOutputProjectSettings.ObjectInstantiationType = ObjectInstantiationType.FindByName;
                    }
                    CodeOutputSettingsPropertyChanged?.Invoke(this, null);
                }
            };

            member.CustomGetEvent += (owner) =>
            {
                switch(CodeOutputProjectSettings?.ObjectInstantiationType)
                {
                    case ObjectInstantiationType.FullyInCode: return FullyInCode;
                    case ObjectInstantiationType.FindByName: return ReferenceGum;
                }
                return "";

            };
            member.CustomGetTypeEvent += (owner) => typeof(ObjectInstantiationType);

            member.CustomOptions = new List<object>
            {
                FullyInCode,
                ReferenceGum
            };

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

                    FullRefreshDataGrid();
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

        // maybe we'll bring this back later?
        //private void CopyButtonClicked(object sender, RoutedEventArgs e)
        //{
        //    TextBoxInstance.Focus();
        //    TextBoxInstance.SelectAll();
        //    if (!string.IsNullOrEmpty(TextBoxInstance.Text))
        //    {
        //        // from: https://stackoverflow.com/questions/68666/clipbrd-e-cant-open-error-when-setting-the-clipboard-from-net
        //        for (int i = 0; i < 11; i++)
        //        {
        //            try
        //            {
        //                Clipboard.SetText(TextBoxInstance.Text);
        //                return;
        //            }
        //            catch { }
        //            System.Threading.Thread.Sleep(15);
        //        }
        //    }
        //}


        private void HandleAutoSetupClicked(object sender, RoutedEventArgs e)
        {
            var csprojLocation = CodeRootSelectionDisplay.GetCsprojDirectory();

            if(csprojLocation == null)
            {
                GumCommands.Self.GuiCommands.ShowMessage("No .csproj file found, so cannot automatically set up code generation.");
                return;
            }

            CodeOutputProjectSettings.CodeProjectRoot = csprojLocation.FullPath;

            // we're going to load the project, so let's set it to find by name:
            CodeOutputProjectSettings.ObjectInstantiationType = ObjectInstantiationType.FindByName;

            try
            {
                var csprojDirectory = CodeOutputProjectSettings.CodeProjectRoot;

                var csproj = System.IO.Directory.GetFiles(csprojDirectory, "*.csproj", System.IO.SearchOption.TopDirectoryOnly)
                    .Select(item => new FilePath(item))
                    .FirstOrDefault();

                if (csproj != null)
                {
                    var contents = System.IO.File.ReadAllText(csproj.FullPath);

                    if (contents.Contains("<PackageReference Include=\"MonoGame.Framework.") ||
                        contents.Contains("<PackageReference Include=\"nkast.Xna.Framework"))
                    {
                        CodeOutputProjectSettings.OutputLibrary = OutputLibrary.MonoGame;
                    }
                    // todo - add more here for automatically setting the output library


                    var namespaceName = csproj.CaseSensitiveNoPathNoExtension
                        .Replace(".", "_")
                        .Replace("-", "_")
                        .Replace(" ", "_")
                        ;

                    if(contents.Contains("<RootNamespace>"))
                    {
                        var startIndex = contents.IndexOf("<RootNamespace>") + "<RootNamespace>".Length;
                        var endIndex = contents.IndexOf("</RootNamespace>");
                        namespaceName = contents.Substring(startIndex, endIndex - startIndex);
                    }

                    CodeOutputProjectSettings.RootNamespace = namespaceName;
                }

                CodeOutputSettingsPropertyChanged?.Invoke(this, null);


                FullRefreshDataGrid();

            }
            catch (Exception ex)
            {
                GumCommands.Self.GuiCommands.PrintOutput($"Error: {ex}");
            }
        }

        private void HandleManualSetupClicked(object sender, RoutedEventArgs e)
        {
            HasClickedManualSetup = true;
            FullRefreshDataGrid();
        }

        #endregion
    }
}
