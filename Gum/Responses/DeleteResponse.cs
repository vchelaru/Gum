using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Responses
{
    public class DeleteResponse
    {
        public bool ShouldDelete { get; set; }
        public bool ShouldShowMessage { get; set; }
        public string Message { get; set; }
    }
}
