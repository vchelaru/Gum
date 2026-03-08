using Gum.ProjectServices.CodeGeneration;
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
    #region Fields/Properties

    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IProjectState _projectState;
    private readonly ICodeGenerationAutoSetupService _autoSetupService;

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

    #endregion

    public CodeWindowViewModel(IProjectState projectState,
        IFileCommands fileCommands,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        ICodeGenerationAutoSetupService autoSetupService)
    {
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _projectState = projectState;
        _autoSetupService = autoSetupService;
    }

    public FilePath? GetCsprojDirectoryAboveGumx()
    {
        if(_projectState.ProjectDirectory == null)
        {
            return null;
        }
        else
        {
            FilePath gumDirectory = _projectState.ProjectDirectory;
            return GetCsprojDirectoryAboveGumx(gumDirectory);
        }
    }

    FilePath? GetCsprojDirectoryAboveGumx(FilePath filePath)
    {
        if (filePath == null)
        {
            return null;
        }

        var files = _fileCommands.GetFiles(filePath.FullPath)
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
        var projectFilePath = _projectState.GumProjectSave?.FullFileName;

        if (string.IsNullOrEmpty(projectFilePath))
        {
            _dialogService.ShowMessage("No .csproj file found, so cannot automatically set up code generation.");
            return false;
        }

        AutoSetupResult result = _autoSetupService.Run(projectFilePath);

        if (!result.Success)
        {
            _dialogService.ShowMessage(result.ErrorMessage ?? "Auto setup failed.");
            return false;
        }

        var configured = result.Settings!;
        codeOutputProjectSettings.CodeProjectRoot = configured.CodeProjectRoot;
        codeOutputProjectSettings.ObjectInstantiationType = configured.ObjectInstantiationType;
        codeOutputProjectSettings.OutputLibrary = configured.OutputLibrary;
        codeOutputProjectSettings.RootNamespace = configured.RootNamespace;

        return true;
    }
}
