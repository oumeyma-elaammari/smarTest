using System;

namespace smartest_desktop.Exceptions
{
    public sealed class SmartestNetworkException : Exception
    {
        private SmartestNetworkException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public static SmartestNetworkException ServerUnreachable(Exception inner)
        {
            return new SmartestNetworkException(
                "Impossible de joindre le serveur. Vérifiez que l'API est démarrée et l'URL.",
                inner);
        }
    }
}
