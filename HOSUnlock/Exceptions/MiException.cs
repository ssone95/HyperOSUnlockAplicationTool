namespace HOSUnlock.Exceptions
{
    public class MiException : Exception
    {
        public int StatusCode { get; private set; }
        public MiException()
        {
        }

        public MiException(string message, int statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
        public MiException(string message, Exception inner, int statusCode)
            : base(message, inner)
        {
            StatusCode = statusCode;
        }
    }
}
