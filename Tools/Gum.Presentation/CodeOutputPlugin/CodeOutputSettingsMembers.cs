using System;
using System.Collections.Generic;
using System.Linq;
using CodeOutputPlugin.ViewModels;
using Gum.ProjectServices.CodeGeneration;
using Gum.ToolStates;
using ToolsUtilities;
using WpfDataUi.DataTypes;

namespace CodeOutputPlugin;

/// <summary>
/// The Code tab's settings grid as neutral members: the project-wide settings and the selected
/// element's settings, each read from and written to <see cref="ProjectSettings"/> and
/// <see cref="ElementSettings"/>. Both heads' Code tab views show <see cref="BuildCategories"/>; a
/// setting written through a member raises <see cref="SettingsChanged"/>, and one that changes which
/// rows exist also raises <see cref="RebuildRequested"/>.
/// </summary>
public class CodeOutputSettingsMembers
{
    private const string FullyInCode = "Fully in Code (no loaded Gum Project)";
    private const string ReferenceGum = "Reference loaded Gum Project";

    private static readonly Dictionary<OutputLibrary, string> LibraryNames = new Dictionary<OutputLibrary, string>
    {
        { OutputLibrary.MonoGameForms, "MonoGame + Forms" },
        { OutputLibrary.Skia, "SkiaSharp" },
        { OutputLibrary.MonoGame, "MonoGame (no forms, deprecated)" },
        { OutputLibrary.Raylib, "Raylib" },
        { OutputLibrary.Silk, "Silk.NET" },
    };

    private static readonly Dictionary<string, OutputLibrary> LibrariesByName =
        LibraryNames.ToDictionary(pair => pair.Value, pair => pair.Key);

    private readonly IProjectState _projectState;
    private readonly SyntaxVersionDetectionService _syntaxVersionDetectionService;
    private readonly CodeWindowViewModel _viewModel;

    /// <summary>Creates the members over the Code tab's view model.</summary>
    public CodeOutputSettingsMembers(
        IProjectState projectState,
        SyntaxVersionDetectionService syntaxVersionDetectionService,
        CodeWindowViewModel viewModel)
    {
        _projectState = projectState;
        _syntaxVersionDetectionService = syntaxVersionDetectionService;
        _viewModel = viewModel;
    }

    /// <summary>The loaded project's code generation settings the project rows edit.</summary>
    public CodeOutputProjectSettings? ProjectSettings { get; set; }

    /// <summary>The selected element's code generation settings the element rows edit.</summary>
    public CodeOutputElementSettings? ElementSettings { get; set; }

    /// <summary>Whether the user chose manual setup, which hides the setup prompt.</summary>
    public bool HasClickedManualSetup { get; private set; }

    /// <summary>Raised after a setting is written through a member or by auto setup.</summary>
    public event EventHandler? SettingsChanged;

    /// <summary>Raised when the rows should be rebuilt, because a change alters which rows exist.</summary>
    public event EventHandler? RebuildRequested;

    /// <summary>
    /// The project-wide and element categories for the current settings. Also updates the view
    /// model's <see cref="CodeWindowViewModel.NeedsSetup"/> and <see cref="CodeWindowViewModel.CanGenerateCode"/>.
    /// </summary>
    public List<MemberCategory> BuildCategories()
    {
        MemberCategory projectCategory = new MemberCategory("Project-Wide Code Generation");
        projectCategory.Members.Add(CreateCodeProjectRootMember());
        projectCategory.Members.Add(CreateOutputLibrarySelectionMember());
        projectCategory.Members.Add(CreateObjectInstantiationTypeMember());
        projectCategory.Members.Add(CreateProjectUsingStatementsMember());
        projectCategory.Members.Add(CreateRootNamespaceMember());
        projectCategory.Members.Add(CreateAppendFolderToNamespaceMember());
        projectCategory.Members.Add(CreateDefaultScreenBaseMember());

        bool createAdjustPixelValues =
            ProjectSettings?.OutputLibrary == OutputLibrary.XamarinForms ||
            ProjectSettings?.OutputLibrary == OutputLibrary.WPF ||
            ProjectSettings?.OutputLibrary == OutputLibrary.Maui;
        if (createAdjustPixelValues)
        {
            projectCategory.Members.Add(CreateAdjustPixelValuesForDensityMember());
            projectCategory.Members.Add(CreateBaseTypesNotCodeGeneratedMember());
            // Not sure if this should be here or not...
            projectCategory.Members.Add(CreateGenerateGumDataTypesCodeMember());
        }

        projectCategory.Members.Add(CreateSyntaxVersionMember());

        MemberCategory elementCategory = new MemberCategory("Element Code Generation");
        elementCategory.Members.Add(CreateGenerationBehaviorMember());
        elementCategory.Members.Add(CreateUsingStatementsMember());
        elementCategory.Members.Add(CreateNamespaceMember());
        elementCategory.Members.Add(CreateGeneratedFileNameMember());
        elementCategory.Members.Add(CreateLocalizeElementMember());

        _viewModel.CanGenerateCode =
            ElementSettings?.GenerationBehavior == GenerationBehavior.GenerateManually ||
            ElementSettings?.GenerationBehavior == GenerationBehavior.GenerateAutomaticallyOnPropertyChange;

        return new List<MemberCategory> { projectCategory, elementCategory };
    }

    /// <summary>Fills in the project settings automatically, as the Auto setup button does.</summary>
    /// <returns>Whether setup went ahead.</returns>
    public bool ApplyAutoSetup()
    {
        bool shouldContinue = ProjectSettings != null && _viewModel.HandleAutoSetupClicked(ProjectSettings);

        if (shouldContinue)
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            RebuildRequested?.Invoke(this, EventArgs.Empty);
        }

        return shouldContinue;
    }

    /// <summary>Skips the setup prompt and shows the settings, as the Manual setup button does.</summary>
    public void ChooseManualSetup()
    {
        HasClickedManualSetup = true;
        RebuildRequested?.Invoke(this, EventArgs.Empty);
    }

    #region Project-wide members

    private InstanceMember CreateCodeProjectRootMember()
    {
        InstanceMember member = new InstanceMember("Code Project Root", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                string valueToSet = (string?)args.Value ?? string.Empty;
                bool needsAppendedSlash = !string.IsNullOrEmpty(valueToSet) &&
                    !valueToSet.EndsWith("\\") &&
                    !valueToSet.EndsWith("/");
                if (needsAppendedSlash)
                {
                    valueToSet += "\\";
                }

                if (!string.IsNullOrWhiteSpace(valueToSet) && FileManager.IsRelative(valueToSet) == false && _projectState.ProjectDirectory != null)
                {
                    string projectDirectory = _projectState.ProjectDirectory;
                    valueToSet = FileManager.MakeRelative(valueToSet, projectDirectory, preserveCase: true);

                    if (string.IsNullOrEmpty(valueToSet))
                    {
                        valueToSet = "./";
                    }
                }

                ProjectSettings.CodeProjectRoot = valueToSet;

                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) =>
        {
            string? projectRoot = ProjectSettings?.CodeProjectRoot;
            if (string.IsNullOrEmpty(projectRoot))
            {
                return string.Empty;
            }
            else if (projectRoot == "./")
            {
                return _projectState.ProjectDirectory;
            }
            // Absolute file paths confuse people, so the root stays relative.
            else
            {
                return projectRoot;
            }
        };
        member.CustomGetTypeEvent += (owner) => typeof(string);
        // Not a file selection editor: that only picks files, and this is a folder.

        _viewModel.NeedsSetup = _viewModel.ShouldShowSetup(ProjectSettings, HasClickedManualSetup);

        return member;
    }

    private InstanceMember CreateOutputLibrarySelectionMember()
    {
        InstanceMember member = new InstanceMember("Output Library", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                string? asString = (string?)args.Value;
                if (!string.IsNullOrEmpty(asString))
                {
                    ProjectSettings.OutputLibrary = LibrariesByName[asString];
                }

                SettingsChanged?.Invoke(this, EventArgs.Empty);

                RebuildRequested?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) =>
            ProjectSettings != null && LibraryNames.TryGetValue(ProjectSettings.OutputLibrary, out string? name)
                ? name
                : string.Empty;

        member.CustomOptions = new List<object>
        {
            LibraryNames[OutputLibrary.MonoGameForms],
            LibraryNames[OutputLibrary.Skia],
            LibraryNames[OutputLibrary.MonoGame],
            LibraryNames[OutputLibrary.Raylib],
            LibraryNames[OutputLibrary.Silk],
        };

        return member;
    }

    private InstanceMember CreateProjectUsingStatementsMember()
    {
        InstanceMember member = new InstanceMember("Project-wide Using Statements", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                ProjectSettings.CommonUsingStatements = (string)args.Value!;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ProjectSettings?.CommonUsingStatements;
        member.CustomGetTypeEvent += (owner) => typeof(string);
        member.PreferredDisplayer = typeof(StandardDisplayers.MultiLineTextBox);

        return member;
    }

    private InstanceMember CreateRootNamespaceMember()
    {
        InstanceMember member = new InstanceMember("Root Namespace", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                ProjectSettings.RootNamespace = (string)args.Value!;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ProjectSettings?.RootNamespace;
        member.CustomGetTypeEvent += (owner) => typeof(string);

        return member;
    }

    private InstanceMember CreateAppendFolderToNamespaceMember()
    {
        InstanceMember member = new InstanceMember("Append Folder to Namespace", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                ProjectSettings.AppendFolderToNamespace = (bool)args.Value!;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ProjectSettings?.AppendFolderToNamespace ?? false;
        member.CustomGetTypeEvent += (owner) => typeof(bool);

        return member;
    }

    private InstanceMember CreateDefaultScreenBaseMember()
    {
        InstanceMember member = new InstanceMember("Default Screen Base", this);
        member.DetailText = "Base class for screens";
        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                ProjectSettings.DefaultScreenBase = (string)args.Value!;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ProjectSettings?.DefaultScreenBase;
        member.CustomGetTypeEvent += (owner) => typeof(string);

        return member;
    }

    private InstanceMember CreateAdjustPixelValuesForDensityMember()
    {
        InstanceMember member = new InstanceMember("Adjust Pixel Values for Density", this);
        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                ProjectSettings.AdjustPixelValuesForDensity = (bool)args.Value!;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ProjectSettings?.AdjustPixelValuesForDensity;
        member.CustomGetTypeEvent += (owner) => typeof(bool);

        return member;
    }

    private InstanceMember CreateBaseTypesNotCodeGeneratedMember()
    {
        InstanceMember member = new InstanceMember("Base types ignored in code generation", this);
        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                ProjectSettings.BaseTypesNotCodeGenerated = (string)args.Value!;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.PreferredDisplayer = typeof(StandardDisplayers.MultiLineTextBox);
        member.CustomGetEvent += (owner) => ProjectSettings?.BaseTypesNotCodeGenerated;
        member.CustomGetTypeEvent += (owner) => typeof(string);

        return member;
    }

    private InstanceMember CreateGenerateGumDataTypesCodeMember()
    {
        InstanceMember member = new InstanceMember("Generate Gum DataTypes Code", this);
        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                ProjectSettings.GenerateGumDataTypes = (bool)args.Value!;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.PreferredDisplayer = typeof(StandardDisplayers.CheckBox);
        member.CustomGetEvent += (owner) => ProjectSettings?.GenerateGumDataTypes ?? false;
        member.CustomGetTypeEvent += (owner) => typeof(string);

        return member;
    }

    private InstanceMember CreateSyntaxVersionMember()
    {
        InstanceMember member = new InstanceMember("Syntax Version", this);

        member.CustomGetEvent += (owner) =>
        {
            if (ProjectSettings == null)
            {
                return "N/A";
            }

            SyntaxVersionResult result = _syntaxVersionDetectionService.Detect(
                ProjectSettings, _projectState.ProjectDirectory);

            return result.Description;
        };
        member.CustomGetTypeEvent += (owner) => typeof(string);
        member.IsReadOnly = true;

        return member;
    }

    private InstanceMember CreateObjectInstantiationTypeMember()
    {
        InstanceMember member = new InstanceMember("Object Instantiation Type", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                if ((args.Value as string) == FullyInCode)
                {
                    ProjectSettings.ObjectInstantiationType = ObjectInstantiationType.FullyInCode;
                }
                else
                {
                    ProjectSettings.ObjectInstantiationType = ObjectInstantiationType.FindByName;
                }
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            RefreshDetailText();
        };

        member.CustomGetEvent += (owner) =>
        {
            switch (ProjectSettings?.ObjectInstantiationType)
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
            ReferenceGum,
        };

        RefreshDetailText();

        void RefreshDetailText()
        {
            string detailText = string.Empty;

            if (ProjectSettings?.OutputLibrary == OutputLibrary.MonoGameForms)
            {
                if (ProjectSettings?.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
                {
                    detailText = "Full code generation in MonoGame + Forms is considered experimental";
                }
            }
            else if (ProjectSettings?.OutputLibrary == OutputLibrary.Raylib)
            {
                if (ProjectSettings?.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
                {
                    detailText = "Raylib code generation only supports \"Reference loaded Gum Project\" (Fully in Code is not yet supported)";
                }
            }
            else if (ProjectSettings?.OutputLibrary == OutputLibrary.Silk)
            {
                if (ProjectSettings?.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
                {
                    detailText = "Silk.NET code generation only supports \"Reference loaded Gum Project\" (Fully in Code is not yet supported)";
                }
            }

            member.DetailText = detailText;
        }

        return member;
    }

    #endregion

    #region Element members

    private InstanceMember CreateGenerationBehaviorMember()
    {
        InstanceMember member = new InstanceMember("Generation Behavior", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ElementSettings != null && args.Value != null)
            {
                ElementSettings.GenerationBehavior = (GenerationBehavior)args.Value;
                SettingsChanged?.Invoke(this, EventArgs.Empty);

                RebuildRequested?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ElementSettings?.GenerationBehavior;
        member.CustomGetTypeEvent += (owner) => typeof(GenerationBehavior);

        return member;
    }

    private InstanceMember CreateUsingStatementsMember()
    {
        InstanceMember member = new InstanceMember("Using Statements", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ElementSettings != null)
            {
                ElementSettings.UsingStatements = (string?)args.Value ?? string.Empty;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ElementSettings?.UsingStatements;
        member.CustomGetTypeEvent += (owner) => typeof(string);
        member.PreferredDisplayer = typeof(StandardDisplayers.MultiLineTextBox);

        return member;
    }

    private InstanceMember CreateNamespaceMember()
    {
        InstanceMember member = new InstanceMember("Namespace", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ElementSettings != null)
            {
                ElementSettings.Namespace = (string?)args.Value ?? string.Empty;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ElementSettings?.Namespace;
        member.CustomGetTypeEvent += (owner) => typeof(string);

        return member;
    }

    private InstanceMember CreateGeneratedFileNameMember()
    {
        InstanceMember member = new InstanceMember("Generated File Name", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ElementSettings != null)
            {
                string valueAsString = (string?)args.Value ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(_projectState.ProjectDirectory) && FileManager.IsRelative(valueAsString) == false)
                {
                    valueAsString = FileManager.MakeRelative(valueAsString, _projectState.ProjectDirectory, preserveCase: true);
                }
                ElementSettings.GeneratedFileName = valueAsString;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ElementSettings?.GeneratedFileName;
        member.CustomGetTypeEvent += (owner) => typeof(string);

        return member;
    }

    private InstanceMember CreateLocalizeElementMember()
    {
        InstanceMember member = new InstanceMember("Localize Element", this);

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ElementSettings != null)
            {
                ElementSettings.LocalizeElement = (bool)args.Value!;

                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ElementSettings?.LocalizeElement ?? false;
        member.CustomGetTypeEvent += (owner) => typeof(bool);

        return member;
    }

    #endregion
}
