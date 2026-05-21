using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using smartest_desktop.Exceptions;

namespace smartest_desktop.Services
{
    /// <summary>Synchronisation quiz local → backend (création MySQL + lien SQLite).</summary>
    public sealed class SyncService
    {
        private readonly LocalQuizService _localQuiz;
        private readonly QuizWebPublicationApiService _api;

        public SyncService(LocalQuizService localQuiz, QuizWebPublicationApiService api)
        {
            _localQuiz = localQuiz ?? throw new ArgumentNullException(nameof(localQuiz));
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        /// <summary>Crée le quiz sur le serveur si aucun id publication n’est encore connu.</summary>
        public async Task<long> PousserQuizVersBackendAsync(
            int quizLocalId,
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            var quiz = await _localQuiz.GetByIdAsync(quizLocalId, cancellationToken);
            if (quiz == null)
                throw new InvalidOperationException("Quiz local introuvable.");

            if (quiz.BackendQuizIdPublicationWeb is long existant && existant > 0)
                return existant;

            long profId = await _api.GetProfesseurIdAsync(bearerToken, cancellationToken);
            long backendId = await _api.CreateQuizAsync(
                bearerToken,
                quiz.Titre ?? "Quiz",
                profId,
                cancellationToken);

            await _localQuiz.DefinirBackendQuizIdSiAbsenteAsync(quizLocalId, backendId, cancellationToken);
            return backendId;
        }

        /// <summary>Si le quiz serveur n’existe plus (404), efface les liens locaux.</summary>
        public async Task ReconcilierApresSuppressionServeurAsync(
            int quizLocalId,
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            var quiz = await _localQuiz.GetByIdAsync(quizLocalId, cancellationToken);
            if (quiz == null)
                return;

            long? id = quiz.BackendQuizIdPublicationWeb ?? quiz.BackendQuizId;
            if (id is not long lid || lid <= 0)
                return;

            var (found, _) = await _api.TryGetQuizAuteurAsync(lid, bearerToken, cancellationToken);
            if (!found)
                await _localQuiz.EffacerIdentifiantsServeurAsync(quizLocalId, cancellationToken);
        }

        /// <summary>409 ou conflit métier lors de la création : message explicite.</summary>
        public static bool EstConflitCreation(SmartestApiException ex) =>
            ex.StatusCode == HttpStatusCode.Conflict;
    }
}
