using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Models;

public enum OutputLibrary
{
    XamarinForms,
    WPF,
    Skia,
    Maui,
    MonoGame,
    MonoGameForms
}

public enum ObjectInstantiationType
{
    FullyInCode = 0,
    FindByName = 1
}

public enum InheritanceLocation
{
    InCustomCode,
    InGeneratedCode
}

public class CodeOutputProjectSettings
{
    public string CommonUsingStatements { get; set; } =
@"using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;
";

    /// <summary>
    /// The location of the project root (usually the .csproj folder), stored as a path relative to the .glux
    /// </summary>
    public string CodeProjectRoot { get; set; } = string.Empty;

    public string RootNamespace { get; set; } = string.Empty;

    public bool AppendFolderToNamespace { get; set; }

    public InheritanceLocation InheritanceLocation { get; set; } = InheritanceLocation.InGeneratedCode;

    public ObjectInstantiationType ObjectInstantiationType { get; set; }

    /// <summary>
    /// For XamarinForms this would be the base screen type like
    /// MyProjectNamespace.Screens.MyBaseGumScreen. For other types
    /// like a PDF renderer, this might just be GraphicalUiElement.
    /// </summary>
    public string DefaultScreenBase { get; set; } =
            "Gum.Wireframe.BindableGue";

    public OutputLibrary OutputLibrary { get; set; } = OutputLibrary.MonoGame;

    public bool AdjustPixelValuesForDensity { get; set; } = false;

    public string BaseTypesNotCodeGenerated { get; set; } = string.Empty;

    public bool GenerateGumDataTypes { get; set; }

    internal void SetDefaults()
    {
        this.AppendFolderToNamespace = true;
    }
}
