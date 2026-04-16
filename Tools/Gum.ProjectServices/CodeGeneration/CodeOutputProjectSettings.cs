using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.ProjectServices.CodeGeneration;

public enum OutputLibrary
{
    XamarinForms = 0,
    WPF = 1,
    Skia = 2,
    Maui = 3,
    MonoGame = 4,
    MonoGameForms = 5
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
    /// <summary>
    /// The current schema version written by this code. New settings produced
    /// by <see cref="SetDefaults"/> are stamped with this value; legacy files
    /// without a Version field deserialize as 0 and are bumped to this value
    /// by <see cref="CodeOutputProjectSettingsManager.MigrateIfNeeded"/>.
    /// </summary>
    public const int CurrentVersion = 1;

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
    /// Optional base class override for generated screens. When empty,
    /// the code generator picks the appropriate default for the current
    /// OutputLibrary (e.g. FrameworkElement for MonoGameForms,
    /// GraphicalUiElement for MonoGame).
    /// </summary>
    public string DefaultScreenBase { get; set; } = "";

    public OutputLibrary OutputLibrary { get; set; } = OutputLibrary.MonoGame;

    public bool AdjustPixelValuesForDensity { get; set; } = false;

    public string BaseTypesNotCodeGenerated { get; set; } = string.Empty;

    public bool GenerateGumDataTypes { get; set; }

    /// <summary>
    /// Controls how the code generator determines the syntax version of the referenced Gum runtime.
    /// Set to <c>"*"</c> (the default) to auto-detect from the game project's Gum reference.
    /// Set to a numeric string (e.g., <c>"0"</c>, <c>"1"</c>) to override auto-detection.
    /// </summary>
    public string SyntaxVersion { get; set; } = "*";

    /// <summary>
    /// Schema version of this .codsj file. Used by
    /// <see cref="CodeOutputProjectSettingsManager"/> to run field-level
    /// migrations on load when the shape or semantics of a setting changes.
    /// <para>
    /// The C# default stays at 0 on purpose: legacy .codsj files written before
    /// this field existed have no Version entry in JSON, so they deserialize as
    /// 0 and correctly trigger <see cref="CodeOutputProjectSettingsManager.MigrateIfNeeded"/>.
    /// Raising the default to <see cref="CurrentVersion"/> would skip migration
    /// for those legacy files. New in-memory settings objects get stamped with
    /// <see cref="CurrentVersion"/> via <see cref="SetDefaults"/> instead.
    /// </para>
    /// </summary>
    public int Version { get; set; }

    internal void SetDefaults()
    {
        this.AppendFolderToNamespace = true;
        this.Version = CurrentVersion;
    }
}
