using System;
using System.Collections.Generic;
using System.IO;
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
    private const string FullyInCode = "Fully in Code";
    private const string ReferenceGum = "Reference loaded Gum Project";

    // Also read by CodeFileLocationChange, so the migration prompt names a library as this row does.
    internal static readonly Dictionary<OutputLibrary, string> LibraryNames = new Dictionary<OutputLibrary, string>
    {
        { OutputLibrary.GumForms, "Gum Forms (recommended)" },
        { OutputLibrary.Skia, "SkiaSharp (deprecated)" },
        { OutputLibrary.MonoGame, "MonoGame (deprecated)" },
        { OutputLibrary.Raylib, "Raylib (deprecated)" },
        { OutputLibrary.Silk, "Silk.NET (deprecated)" },
    };

    private static readonly Dictionary<string, OutputLibrary> LibrariesByName =
        LibraryNames.ToDictionary(pair => pair.Value, pair => pair.Key);

    /// <summary>
    /// Width of the label column of the project rows: wide enough that every label stays on one line.
    /// Set per member, so no other property grid changes.
    /// </summary>
    public const double ProjectLabelColumnWidth = 190;

    private InstanceMember? _resolvedLocationsMember;
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
        SettingsChanged += (_, _) => _resolvedLocationsMember?.SimulateValueChanged();
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
        return new List<MemberCategory> { BuildProjectCategory(), BuildElementCategory() };
    }

    /// <summary>The project-wide rows as one flat category, for the frozen WPF Code tab.</summary>
    public MemberCategory BuildProjectCategory()
    {
        MemberCategory projectCategory = new MemberCategory("Project-Wide Code Generation");
        foreach (MemberCategory section in BuildProjectSections())
        {
            foreach (InstanceMember member in section.Members)
            {
                projectCategory.Members.Add(member);
            }
        }
        return projectCategory;
    }

    /// <summary>
    /// The project-wide rows grouped by what the user is thinking about, shown on the Code generation
    /// page of the Settings window: Output, Locations, Namespaces and usings, and, only for the legacy
    /// libraries, a collapsed Legacy section. Every row has a one-line hint, so a row's label and hint are
    /// its search terms.
    /// </summary>
    public List<MemberCategory> BuildProjectSections()
    {
        MemberCategory output = new MemberCategory("Output");
        output.Members.Add(CreateOutputLibrarySelectionMember());
        output.Members.Add(CreateObjectInstantiationTypeMember());
        output.Members.Add(CreateSyntaxVersionMember());

        MemberCategory locations = new MemberCategory("Locations");
        locations.Members.Add(CreateCodeProjectRootMember());
        locations.Members.Add(CreateGeneratedCodeFolderMember());
        locations.Members.Add(CreateCustomCodeFolderMember());
        locations.Members.Add(CreateGeneratedCodeFolderPrefixMember());
        locations.Members.Add(CreateResolvedLocationsMember());

        MemberCategory namespaces = new MemberCategory("Namespaces and usings");
        namespaces.Members.Add(CreateRootNamespaceMember());
        namespaces.Members.Add(CreateAppendFolderToNamespaceMember());
        namespaces.Members.Add(CreateDefaultScreenBaseMember());
        namespaces.Members.Add(CreateProjectUsingStatementsMember());

        List<MemberCategory> sections = new List<MemberCategory> { output, locations, namespaces };

        bool createLegacy =
            ProjectSettings?.OutputLibrary == OutputLibrary.XamarinForms ||
            ProjectSettings?.OutputLibrary == OutputLibrary.WPF ||
            ProjectSettings?.OutputLibrary == OutputLibrary.Maui;
        if (createLegacy)
        {
            MemberCategory legacy = new MemberCategory("Legacy output (Xamarin, WPF, Maui)") { IsExpanded = false };
            legacy.Members.Add(CreateAdjustPixelValuesForDensityMember());
            legacy.Members.Add(CreateBaseTypesNotCodeGeneratedMember());
            // Not sure if this should be here or not...
            legacy.Members.Add(CreateGenerateGumDataTypesCodeMember());
            sections.Add(legacy);
        }

        foreach (MemberCategory section in sections)
        {
            foreach (InstanceMember member in section.Members)
            {
                member.FirstGridLength = ProjectLabelColumnWidth;
            }
            MarkReadOnlyWhileSettingsAreUnreadable(section);
        }
        return sections;
    }

    /// <summary>
    /// The rows for the selected element, shown on the Code tab. Also updates the view model's
    /// <see cref="CodeWindowViewModel.CanGenerateCode"/>.
    /// </summary>
    public MemberCategory BuildElementCategory()
    {
        MemberCategory elementCategory = new MemberCategory("Element Code Generation");
        elementCategory.Members.Add(CreateGenerationBehaviorMember());
        elementCategory.Members.Add(CreateUsingStatementsMember());
        elementCategory.Members.Add(CreateNamespaceMember());
        elementCategory.Members.Add(CreateGeneratedFileNameMember());
        elementCategory.Members.Add(CreateLocalizeElementMember());

        _viewModel.CanGenerateCode =
            ElementSettings?.GenerationBehavior == GenerationBehavior.GenerateManually ||
            ElementSettings?.GenerationBehavior == GenerationBehavior.GenerateAutomaticallyOnPropertyChange;

        MarkReadOnlyWhileSettingsAreUnreadable(elementCategory);
        return elementCategory;
    }

    // An unreadable ProjectCodeSettings.codsj shows defaults in its place, which no edit may save.
    private void MarkReadOnlyWhileSettingsAreUnreadable(MemberCategory category)
    {
        if (_viewModel.HasProjectSettingsLoadError)
        {
            foreach (InstanceMember member in category.Members)
            {
                member.IsReadOnly = true;
            }
        }
    }

    /// <summary>Asks the views to rebuild the rows, as after the settings were replaced.</summary>
    public void RequestRebuild() => RebuildRequested?.Invoke(this, EventArgs.Empty);

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

    /// <summary>
    /// Forgets the Manual choice when a project opens: it is not saved, so it belonged to the
    /// project that was open, and a project with no code settings is offered setup again.
    /// </summary>
    public void HandleProjectLoaded() => HasClickedManualSetup = false;

    #region Project-wide members

    private InstanceMember CreateCodeProjectRootMember()
    {
        InstanceMember member = new InstanceMember("Code Project Root", this);
        member.DetailText = "The folder code is written under, usually your game's .csproj folder; relative to the .gumx";

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
                    // Native, since "\" is a file-name character on macOS/Linux.
                    valueToSet += Path.DirectorySeparatorChar;
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
        UseFolderPicker(member);

        // Setup would save over an unreadable file, so it is not offered then.
        _viewModel.NeedsSetup = !_viewModel.HasProjectSettingsLoadError
            && _viewModel.ShouldShowSetup(ProjectSettings, HasClickedManualSetup);

        return member;
    }

    private InstanceMember CreateGeneratedCodeFolderMember() => CreateCodeSubfolderMember(
        "Generated Code Folder", "Where the .Generated.cs files go, relative to the Code Project Root; Gum rewrites them, never edit them",
        settings => settings.GeneratedCodeFolder, (settings, value) => settings.GeneratedCodeFolder = value);

    private InstanceMember CreateCustomCodeFolderMember() => CreateCodeSubfolderMember(
        "Custom Code Folder", "Where your own .cs files go, relative to the Code Project Root; empty keeps them beside the generated files",
        settings => settings.CustomCodeFolder, (settings, value) => settings.CustomCodeFolder = value);

    // A folder relative to the Code Project Root, with a text field and a browse button.
    private InstanceMember CreateCodeSubfolderMember(string label, string detail,
        Func<CodeOutputProjectSettings, string> get, Action<CodeOutputProjectSettings, string> set)
    {
        InstanceMember member = new InstanceMember(label, this);
        member.DetailText = detail;

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                string valueToSet = ((string?)args.Value ?? string.Empty).Trim();
                string? codeProjectRoot = GetAbsoluteCodeProjectRoot();
                if (valueToSet.Length > 0 && !FileManager.IsRelative(valueToSet) && codeProjectRoot != null)
                {
                    // Kept relative, like Code Project Root, so the .codsj works on every machine.
                    valueToSet = FileManager.MakeRelative(valueToSet, codeProjectRoot, preserveCase: true);
                }
                set(ProjectSettings, valueToSet);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ProjectSettings == null ? null : get(ProjectSettings);
        member.CustomGetTypeEvent += (owner) => typeof(string);
        UseFolderPicker(member);
        // The value is relative to the Code Project Root, not the project folder the picker
        // resolves against by default. Read on each reveal so a root edit is picked up.
        member.PropertiesToSetOnDisplayer["RevealRelativeTo"] = (Func<string?>)GetAbsoluteCodeProjectRoot;

        return member;
    }

    // Null when no project or settings are loaded.
    private string? GetAbsoluteCodeProjectRoot()
    {
        string? projectDirectory = _projectState.ProjectDirectory;
        if (ProjectSettings == null || projectDirectory == null)
        {
            return null;
        }

        string codeProjectRoot = ProjectSettings.CodeProjectRoot;
        return FileManager.IsRelative(codeProjectRoot) ? projectDirectory + codeProjectRoot : codeProjectRoot;
    }

    // A text field plus a folder-browse button. The picked folder is absolute; the member's setter
    // makes it relative.
    private static void UseFolderPicker(InstanceMember member)
    {
        member.PreferredDisplayer = typeof(StandardDisplayers.FileSelection);
        member.PropertiesToSetOnDisplayer["IsFolderDialog"] = true;
    }

    private InstanceMember CreateGeneratedCodeFolderPrefixMember()
    {
        InstanceMember member = new InstanceMember("Generated Code Folder Prefix", this);
        member.DetailText = "Put in front of the Screens and Components folders in both folders: GumCodeGen/ nests them, Gum renames them GumScreens";

        member.CustomSetPropertyEvent += (owner, args) =>
        {
            if (ProjectSettings != null)
            {
                ProjectSettings.GeneratedCodeFolderPrefix = ((string?)args.Value ?? string.Empty).Trim();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        member.CustomGetEvent += (owner) => ProjectSettings?.GeneratedCodeFolderPrefix;
        member.CustomGetTypeEvent += (owner) => typeof(string);

        return member;
    }

    private InstanceMember CreateOutputLibrarySelectionMember()
    {
        InstanceMember member = new InstanceMember("Output Library", this);
        member.DetailText = "Which runtime the generated code targets; Gum Forms works on every runtime";

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
            LibraryNames[OutputLibrary.GumForms],
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
        member.DetailText = "Added at the top of every generated and custom file";

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
        member.DetailText = "Namespace of every generated class; empty writes no namespace";

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
        member.DetailText = "Adds the element's Gum folder to its namespace, such as MyGame.Components.Buttons";

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
        member.DetailText = "Base class for generated screens; empty picks the default for the Output Library";
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
        member.DetailText = "Scales emitted pixel values by the display density";
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
        member.DetailText = "Base types that get no generated class, one per line";
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
        member.DetailText = "Also emits Gum data types next to the UI classes";
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

            member.ToolTipText = result.Description;
            return $"{result.Version}  ({DescribeSyntaxSource(result.Source)})";
        };
        member.CustomGetTypeEvent += (owner) => typeof(string);
        member.IsReadOnly = true;
        member.DetailText = "Read-only; the Gum runtime version the generated code is written for";

        return member;
    }

    // Read-only, so the hint says where the number came from rather than what it does.
    private static string DescribeSyntaxSource(SyntaxVersionSource source) => source switch
    {
        SyntaxVersionSource.ManualOverride => "set in .codsj",
        SyntaxVersionSource.NuGetPackage => "NuGet package",
        SyntaxVersionSource.ProjectReference => "project reference",
        SyntaxVersionSource.AssemblyReference => "assembly reference",
        _ => "not detected, default",
    };

    private InstanceMember CreateResolvedLocationsMember()
    {
        InstanceMember member = new InstanceMember("Resolves to", this);
        member.DetailText = "Where a Screen and a Component land with the folders above, relative to the Code Project Root";
        member.CustomGetEvent += (owner) => ProjectSettings == null ? string.Empty : CodeLocationPreview.Describe(ProjectSettings);
        member.CustomGetTypeEvent += (owner) => typeof(string);
        member.PreferredDisplayer = typeof(StandardDisplayers.MultiLineTextBox);
        member.IsReadOnly = true;
        _resolvedLocationsMember = member;
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
            string detailText = "Fully in Code builds the UI from C#; Reference loaded Gum Project looks it up in a loaded .gumx";
            bool isWarning = false;

            if (ProjectSettings?.OutputLibrary == OutputLibrary.GumForms)
            {
                if (ProjectSettings?.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
                {
                    detailText = "Warning: Fully in Code is experimental with Gum Forms";
                    isWarning = true;
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
            member.IsDetailTextWarning = isWarning;
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
