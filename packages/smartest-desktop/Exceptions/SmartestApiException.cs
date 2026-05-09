using System;
using System.Collections.Generic;
using System.Net;
using smartest_desktop.Helpers;

namespace smartest_desktop.Exceptions
{
    /// <summary>Réponse HTTP d'erreur de l'API Smartest (corps + statut), à afficher ou journaliser côté desktop.</summary>
    public sealed class SmartestApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string RawBody { get; }
        public IReadOnlyDictionary<string, string> ValidationErrors { get; }

        public SmartestApiException(
            HttpStatusCode statusCode,
            string rawBody,
            string userMessage,
            IReadOnlyDictionary<string, string>? validationErrors = null,
            Exception? innerException = null)
            : base(userMessage, innerException)
        {
            StatusCode = statusCode;
            RawBody = rawBody ?? string.Empty;
            ValidationErrors = validationErrors ?? new Dictionary<string, string>();
        }

        /// <param name="operationContext">Préfixe optionnel (ex. « Publication web »).</param>
        public static SmartestApiException FromHttpFailure(
            HttpStatusCode statusCode,
            string rawBody,
            string? operationContext = null)
        {
            var map = ApiFailureParser.TryParseValidationMap(rawBody);
            var core = ApiFailureParser.BuildMessage(statusCode, rawBody);
            var message = string.IsNullOrEmpty(operationContext)
                ? core
                : $"{operationContext} : {core}";
            return new SmartestApiException(statusCode, rawBody ?? string.Empty, message, map);
        }
    }
}
