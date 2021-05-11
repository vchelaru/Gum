using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolsUtilities
{
    public class GeneralResponse
    {
        public static GeneralResponse SuccessfulResponse => new GeneralResponse { Succeeded = true };
        public static GeneralResponse UnsuccessfulResponse => new GeneralResponse { Succeeded = false };

        public static GeneralResponse UnsuccessfulWith(string message) =>
            new GeneralResponse { Succeeded = false, Message = message };

        public bool Succeeded { get; set; }
        public string Message { get; set; }

        public void Fail(string failureMessage)
        {
            Succeeded = false;
            Message = failureMessage;
        }
    }
}
