using System;
using System.Collections.Generic;
using System.Linq;
using smartest_desktop.Models;

namespace smartest_desktop.Services
{
    /// <summary>
    /// Corrélation entre un quiz local et un id MySQL via la liste <c>GET /api/professeur/mes-quiz</c>.
    /// </summary>
    public static class MesQuizCorrelation
    {
        public static bool TitreNonVide(string? titre) =>
            !string.IsNullOrWhiteSpace(titre?.Trim());

        /// <summary>
        /// Résout l’identifiant serveur ou renvoie <c>null</c> si aucun candidat sans ambiguïté ;
        /// lève <see cref="InvalidOperationException"/> si plusieurs candidats pour le même critère.
        /// </summary>
        public static long? TryResolveQuizId(
            IReadOnlyList<QuizProfesseurListeItem> liste,
            string titreLocal,
            int nombreQuestionsLocal)
        {
            if (!TitreNonVide(titreLocal))
                return null;

            string titreLoc = titreLocal.Trim();

            static bool TitreEq(string? a, string? b) =>
                string.Equals(
                    (a ?? string.Empty).Trim(),
                    (b ?? string.Empty).Trim(),
                    StringComparison.OrdinalIgnoreCase);

            var candidats = liste
                .Where(x => x.Id > 0 && TitreEq(x.Titre, titreLoc) && (x.NombreQuestions ?? -1) == nombreQuestionsLocal)
                .ToList();

            if (candidats.Count > 1)
            {
                throw new InvalidOperationException(
                    "Plusieurs quiz sur le serveur ont le même titre et le même nombre de questions pour votre compte. " +
                    "Supprimez le doublon sur la plateforme ou renommez un des quiz, puis réessayez.");
            }

            if (candidats.Count == 1)
                return candidats[0].Id;

            var parTitreSeul = liste
                .Where(x => x.Id > 0 && TitreEq(x.Titre, titreLoc))
                .ToList();

            if (parTitreSeul.Count == 1)
                return parTitreSeul[0].Id;

            if (parTitreSeul.Count > 1)
            {
                throw new InvalidOperationException(
                    "Plusieurs quiz sur le serveur portent ce titre pour votre compte. " +
                    "Renommez ou supprimez les doublons sur la plateforme, puis réessayez.");
            }

            return null;
        }
    }
}
