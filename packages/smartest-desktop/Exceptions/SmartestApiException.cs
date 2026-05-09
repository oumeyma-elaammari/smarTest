using System;
using System.Net;

namespace smartest_desktop.Exceptions
{
    public sealed class SmartestApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }

        public SmartestApiException(HttpStatusCode statusCode, string responseBody, string message)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody ?? string.Empty;
        }

        public static SmartestApiException FromHttpFailure(HttpStatusCode statusCode, string body, string context)
        {
            return new SmartestApiException(statusCode, body ?? string.Empty,
                $"{context} : échec HTTP {(int)statusCode}");
        }
    }
}
