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

        public override string ToString() => $"{(Succeeded ? "Success" : "Failure")}: {Message}";

    }

    public class OptionallyAttemptedGeneralResponse : GeneralResponse
    {
        public static OptionallyAttemptedGeneralResponse SuccessfulWithoutAttempt =>
            new OptionallyAttemptedGeneralResponse { Succeeded = true, DidAttempt = false };

        public static OptionallyAttemptedGeneralResponse UnsuccessfulWithoutAttempt =>
            new OptionallyAttemptedGeneralResponse { Succeeded = false, DidAttempt = false };

        public static OptionallyAttemptedGeneralResponse SuccessfulAttempt =>
            new OptionallyAttemptedGeneralResponse { Succeeded = true, DidAttempt = true };


        public bool DidAttempt { get; set; }

        public new void SetFrom(GeneralResponse generalResponse)
        {
            this.Message = generalResponse.Message;
            this.Succeeded = generalResponse.Succeeded;
            this.DidAttempt = true; // if we have a response, let's assume there was an attempt
        }

        public void SetFrom(OptionallyAttemptedGeneralResponse generalResponse)
        {
            this.Message = generalResponse.Message;
            this.Succeeded = generalResponse.Succeeded;
            this.DidAttempt = generalResponse.DidAttempt;
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
