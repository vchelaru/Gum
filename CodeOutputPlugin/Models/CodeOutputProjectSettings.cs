using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Models
{
    public enum OutputLibrary
    {
        XamarinForms,
        WPF,
        RawSkia,
        Maui,
        MonoGame
    }

    public enum ObjectInstantiationType
    {
        FullyInCode,
        FindByName
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
        public string CodeProjectRoot { get; set; }

        public string RootNamespace { get; set; }

        public bool IsCodeGenPluginEnabled { get; set; }

        public bool IsShowCodegenPreviewChecked { get; set; }

        public InheritanceLocation InheritanceLocation { get; set; }

        public ObjectInstantiationType ObjectInstantiationType { get; set; }

        /// <summary>
        /// For XamarinForms this would be the base screen type like
        /// MyProjectNamespace.Screens.MyBaseGumScreen. For other types
        /// like a PDF renderer, this might just be GraphicalUiElement.
        /// </summary>
        public string DefaultScreenBase { get; set; } = "Gum.Wireframe.GraphicalUiElement";

        public OutputLibrary OutputLibrary { get; set; }

        public bool AdjustPixelValuesForDensity { get; set; } = false;

        public string BaseTypesNotCodeGenerated { get; set; }

        public bool GenerateGumDataTypes { get; set; }
    }
}
