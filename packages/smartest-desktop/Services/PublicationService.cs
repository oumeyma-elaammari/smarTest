using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using smartest_desktop.Data.LocalEntities;

namespace smartest_desktop.Services
{
    /// <summary>
    /// Façade « Publier sur le web » : délégation à <see cref="QuizWebPublicationApiService"/> sans dupliquer l’HTTP.
    /// </summary>
    public sealed class PublicationService
    {
        private readonly QuizWebPublicationApiService _api;

        public PublicationService(QuizWebPublicationApiService? api = null) =>
            _api = api ?? new QuizWebPublicationApiService();

        public Task<long> GetProfesseurIdAsync(string bearerToken, CancellationToken cancellationToken = default) =>
            _api.GetProfesseurIdAsync(bearerToken, cancellationToken);

        public Task<long> CreateQuizAsync(string bearerToken, string titre, long professeurId, CancellationToken cancellationToken = default) =>
            _api.CreateQuizAsync(bearerToken, titre, professeurId, cancellationToken);

        public Task PostPublicationWebAsync(string bearerToken, long backendQuizId, IReadOnlyList<string> emails, IReadOnlyList<QuestionLocale>? questions = null, CancellationToken cancellationToken = default) =>
            _api.PostPublicationWebAsync(bearerToken, backendQuizId, emails, questions, cancellationToken);

        public Task SyncQuestionsProfAsync(string bearerToken, long backendQuizId, IReadOnlyList<QuestionLocale>? questions, CancellationToken cancellationToken = default) =>
            _api.SyncQuestionsProfAsync(bearerToken, backendQuizId, questions, cancellationToken);

        public Task DeleteQuizAsync(string bearerToken, long backendQuizId, CancellationToken cancellationToken = default) =>
            _api.DeleteQuizAsync(bearerToken, backendQuizId, cancellationToken);
    }
}
