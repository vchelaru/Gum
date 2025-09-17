using CodeOutputPlugin.Models;
using Gum;
using Gum.Managers;
using Gum.Mvvm;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace CodeOutputPlugin.ViewModels;

#region WhatToView Enum
public enum WhatToView
{
    SelectedElement,
    SelectedState
}

#endregion

#region WhichElementsToGenerate Enum

public enum WhichElementsToGenerate
{
    SelectedOnly,
    AllInProject
}

#endregion

public class CodeWindowViewModel : ViewModel
{
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;

    public bool CanGenerateCode
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool NeedsSetup
    {
        get => Get<bool>();
        set => Set(value);
    }
    
    public WhatToView WhatToView
    {
        get => Get<WhatToView>();
        set => Set(value);
    }

    [DependsOn(nameof(WhatToView))]
    public bool IsSelectedObjectSelected
    {
        get => WhatToView == WhatToView.SelectedElement;
        set
        {
            if(value)
            {
                WhatToView = WhatToView.SelectedElement;
            }
        }
    }

    [DependsOn(nameof(WhatToView))]
    public bool IsSelectedStateSelected
    {
        get => WhatToView == WhatToView.SelectedState;
        set
        {
            if (value)
            {
                WhatToView = WhatToView.SelectedState;
            }
        }
    }

    // This exists in case we want to bring it back in the future, but the UI for it is gone.
    public InheritanceLocation InheritanceLocation
    {
        get => Get<InheritanceLocation>();
        set => Set(value);
    }

    [DependsOn(nameof(InheritanceLocation))]
    public bool IsInCustomCodeChecked
    {
        get => InheritanceLocation == InheritanceLocation.InCustomCode;
        set
        {
            if (value)
            {
                InheritanceLocation = InheritanceLocation.InCustomCode;
            }
        }
    }

    [DependsOn(nameof(InheritanceLocation))]
    public bool IsInGeneratedCodeChecked
    {
        get => InheritanceLocation == InheritanceLocation.InGeneratedCode;
        set
        {
            if (value)
            {
                InheritanceLocation = InheritanceLocation.InGeneratedCode;
            }
        }
    }

    public bool IsViewingStandardElement
    {
        get => Get<bool>();
        set => Set(value);
    }

    [DependsOn(nameof(IsViewingStandardElement))]
    public Visibility GenerateCodeUiVisibility => (IsViewingStandardElement == false).ToVisibility();

    [DependsOn(nameof(IsViewingStandardElement))]
    public Visibility ShowNoGenerationAvailableUiVisibility => IsViewingStandardElement.ToVisibility();

    public WhichElementsToGenerate WhichElementsToGenerate
    {
        get => Get<WhichElementsToGenerate>();
        set => Set(value);
    }

    [DependsOn(nameof(WhichElementsToGenerate))]
    public bool IsSelectedOnlyGenerating
    {
        get => WhichElementsToGenerate == WhichElementsToGenerate.SelectedOnly;
        set
        {
            if (value)
            {
                WhichElementsToGenerate = WhichElementsToGenerate.SelectedOnly;
            }
        }
    }

    [DependsOn(nameof(WhichElementsToGenerate))]
    public bool IsAllInProjectGenerating
    {
        get => WhichElementsToGenerate == WhichElementsToGenerate.AllInProject;
        set
        {
            if (value)
            {
                WhichElementsToGenerate = WhichElementsToGenerate.AllInProject;
            }
        }
    }

    public string Code
    {
        get => Get<string>();
        set => Set(value);
    }

    public CodeWindowViewModel()
    {
        _dialogService = Locator.GetRequiredService<IDialogService>();
        _guiCommands = Locator.GetRequiredService<IGuiCommands>();
    }

    public FilePath? GetCsprojDirectoryAboveGumx()
    {
        FilePath gumDirectory = GumState.Self.ProjectState.ProjectDirectory;

        return GetCsprojDirectoryAboveGumx(gumDirectory);
    }

    FilePath? GetCsprojDirectoryAboveGumx(FilePath filePath)
    {
        if (filePath == null)
        {
            return null;
        }

        var files = System.IO.Directory.GetFiles(filePath.FullPath)
            .Select(item => new FilePath(item));

        if (files.Any(item => item.Extension == "csproj"))
        {
            return filePath;
        }

        var parentDirectory = filePath.GetDirectoryContainingThis();

        if (parentDirectory == null)
        {
            return null;
        }
        else
        {
            return GetCsprojDirectoryAboveGumx(parentDirectory);
        }
    }

    public bool HandleAutoSetupClicked(CodeOutputProjectSettings codeOutputProjectSettings)
    {
        var csprojLocation = GetCsprojDirectoryAboveGumx();

        var shouldContinue = true;

        if (csprojLocation == null)
        {
            _dialogService.ShowMessage("No .csproj file found, so cannot automatically set up code generation.");
            shouldContinue = false;
        }

        if (shouldContinue)
        {


            codeOutputProjectSettings.CodeProjectRoot = csprojLocation.FullPath;

            // we're going to load the project, so let's set it to find by name:
            codeOutputProjectSettings.ObjectInstantiationType = ObjectInstantiationType.FindByName;

            try
            {
                var csprojDirectory = codeOutputProjectSettings.CodeProjectRoot;

                var csproj = System.IO.Directory.GetFiles(csprojDirectory, "*.csproj", System.IO.SearchOption.TopDirectoryOnly)
                    .Select(item => new FilePath(item))
                    .FirstOrDefault();

                if (csproj != null)
                {
                    var contents = System.IO.File.ReadAllText(csproj.FullPath);

                    var isMonoGameBased = contents.Contains("<PackageReference Include=\"MonoGame.Framework.") ||
                        contents.Contains("<PackageReference Include=\"nkast.Xna.Framework");

                    if (isMonoGameBased)
                    {
                        // if the user has added forms, let's default to Forms
                        // Otherwise, fall back to normal monogame.
                        var project = ObjectFinder.Self.GumProjectSave;

                        // This is arbitrary, but let's pick 2 behaviors which are common in forms
                        // and use those to determine if this project has forms:
                        //var hasForms = project.Behaviors.Any(item => item.Name == "ButtonBehavior") &&
                        //    project.Behaviors.Any(item => item.Name == "TextBoxBehavior");
                        // Update - why not always use forms? This seems like it will cause less confusion
                        //if(hasForms)
                        //{
                            codeOutputProjectSettings.OutputLibrary = OutputLibrary.MonoGameForms;
                        //}
                    }

                    var namespaceName = csproj.CaseSensitiveNoPathNoExtension
                        .Replace(".", "_")
                        .Replace("-", "_")
                        .Replace(" ", "_")
                        ;

                    if (contents.Contains("<RootNamespace>"))
                    {
                        var startIndex = contents.IndexOf("<RootNamespace>") + "<RootNamespace>".Length;
                        var endIndex = contents.IndexOf("</RootNamespace>");
                        namespaceName = contents.Substring(startIndex, endIndex - startIndex);
                    }

                    codeOutputProjectSettings.RootNamespace = namespaceName;
                }


            }
            catch (Exception ex)
            {
                _guiCommands.PrintOutput($"Error: {ex}");
            }
        }

        return shouldContinue;
    }
}
