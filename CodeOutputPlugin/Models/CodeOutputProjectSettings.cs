using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Models
{
    public class CodeOutputProjectSettings
    {
        public string CommonUsingStatements { get; set; }

        public string CodeProjectRoot { get; set; }

        public string RootNamespace { get; set; }
    }
}
