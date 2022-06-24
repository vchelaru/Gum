using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Models
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
        public string Namespace { get; set; }
        public string UsingStatements { get; set; }
        public string GeneratedFileName { get; set; }

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

   }
}
