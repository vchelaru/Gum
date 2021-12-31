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

        public virtual void SetFrom(GeneralResponse generalResponse)
        {
            this.Succeeded = generalResponse.Succeeded;
            this.Message = generalResponse.Message;
        }

    }


    public class GeneralResponse<T> : GeneralResponse
    {
        public static new GeneralResponse<T> SuccessfulResponse => new GeneralResponse<T> { Succeeded = true };
        public static new GeneralResponse<T> UnsuccessfulResponse => new GeneralResponse<T> { Succeeded = false };

        public static new GeneralResponse<T> UnsuccessfulWith(string message) =>
            new GeneralResponse<T> { Succeeded = false, Message = message };

        public T Data { get; set; }

        public GeneralResponse()
        {
            Data = default(T);
        }

        public override void SetFrom(GeneralResponse nonGenericResponse)
        {
            Data = default(T);

            this.Succeeded = nonGenericResponse.Succeeded;
            this.Message = nonGenericResponse.Message;
        }

        public GeneralResponse(GeneralResponse nonGenericResponse)
        {
            SetFrom(nonGenericResponse);
        }

    }
}
