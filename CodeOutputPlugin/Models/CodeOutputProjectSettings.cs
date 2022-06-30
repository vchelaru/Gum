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
        RawSkia
    }

    public class CodeOutputProjectSettings
    {
        public string CommonUsingStatements { get; set; } =
@"using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using SkiaGum;
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
";

        /// <summary>
        /// The location of the project root (usually the .csproj folder), stored as a path relative to the .glux
        /// </summary>
        public string CodeProjectRoot { get; set; }

        public string RootNamespace { get; set; }

        public bool IsCodeGenPluginEnabled { get; set; }

        /// <summary>
        /// For XamarinForms this would be the base screen type like
        /// MyProjectNamespace.Screens.MyBaseGumScreen. For other types
        /// like a PDF renderer, this might just be BindableGraphicalUiElement. We'll default to that
        /// </summary>
        public string DefaultScreenBase { get; set; } = "SkiaGum.GueDeriving.ContainerRuntime";

        public OutputLibrary OutputLibrary { get; set; }

        public bool AdjustPixelValuesForDensity { get; set; } = false;
    }
}
