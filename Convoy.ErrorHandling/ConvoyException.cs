using System;
using System.Net;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Convoy.ErrorHandling
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ConvoyException : Exception
    {
        [JsonInclude]
        [JsonProperty("ErrorMessage")]
        public string ErrorMessage { get; private set; }

        [JsonInclude]
        [JsonProperty("StatusCode")]
        public HttpStatusCode StatusCode { get; private set; }

        public ConvoyException(string errorMessage, HttpStatusCode statusCode, string errorStackTrace = null) : base(errorMessage)
        {
            ErrorMessage = errorMessage;
            StatusCode = statusCode;
        }
        
    }
}