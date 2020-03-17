using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Models
{
    public class CodeOutputElementSettings
    {
        public string Namespace { get; set; }
        public string CodeFileLocation { get; set; }
        public string UsingStatements { get; set; }
    }
}
