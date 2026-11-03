using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.ProjectServices.CodeGeneration
{
    public enum GenerationBehavior
    {
        NeverGenerate,
        GenerateManually,
        GenerateAutomaticallyOnPropertyChange
    }

    public enum VirtualOverride
    {
        None,
        Virtual,
        Override
    }

    public class CodeOutputElementSettings
    {
        public string Namespace { get; set; } = string.Empty;
        public string UsingStatements { get; set; } = string.Empty;
        public string GeneratedFileName { get; set; } = string.Empty;

        public GenerationBehavior GenerationBehavior { get; set; }



        // This is here for old projects, but should go away soon. Added June 21, 2021, but since
        // there aren't many projects that use this, this property can go away soon like July 2021
        public bool AutoGenerateOnChange 
        {
            get => GenerationBehavior == GenerationBehavior.GenerateAutomaticallyOnPropertyChange;
            set
            {
                if(value)
                {
                    GenerationBehavior = GenerationBehavior.GenerateAutomaticallyOnPropertyChange;
                }
            }
        }

        public bool LocalizeElement { get; set; }

        /// <summary>
        /// When true, the element's generated file is wrapped in <c>#if DEBUG</c> and every generated
        /// reference to the element from other elements is too, so release builds leave it out.
        /// Omitted from the .codsj file while false, so files written before this existed keep their bytes.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore)]
        public bool DebugOnly { get; set; }

   }
}
