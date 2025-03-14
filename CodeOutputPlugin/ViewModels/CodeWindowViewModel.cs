using CodeOutputPlugin.Models;
using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CodeOutputPlugin.ViewModels;

public enum WhatToView
{
    SelectedElement,
    SelectedState
}

public enum WhichElementsToGenerate
{
    SelectedOnly,
    AllInProject
}

public class CodeWindowViewModel : ViewModel
{
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
}
