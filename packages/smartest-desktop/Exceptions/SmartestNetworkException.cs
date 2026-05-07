using System;

namespace smartest_desktop.Exceptions
{
    /// <summary>Échec de connexion ou de transport HTTP vers l'API (backend injoignable, DNS, TLS, etc.).</summary>
    public sealed class SmartestNetworkException : Exception
    {
        public SmartestNetworkException(string message)
            : base(message)
        {
        }

        public SmartestNetworkException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public static SmartestNetworkException ServerUnreachable(Exception? innerException = null)
        {
            const string msg =
                "Connexion impossible pour le moment. Verifiez votre connexion internet puis reessayez.";
            return innerException == null
                ? new SmartestNetworkException(msg)
                : new SmartestNetworkException(msg, innerException);
        }
    }
}
