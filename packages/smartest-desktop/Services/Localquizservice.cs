using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace smartest_desktop.Services
{
    public enum QuizListSort
    {
        DateCreation,
        Titre,
        NombreQuestions
    }

    public class LocalQuizService
    {
        private readonly LocalDbContext _db;

        public LocalQuizService(LocalDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        private static async Task<T> InvokeDbAsync<T>(Func<Task<T>> action, string contexte)
        {
            try
            {
                return await action();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException(
                    $"{contexte} : données modifiées ou supprimées depuis le dernier chargement.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException(
                    $"{contexte} : la base locale n’a pas pu enregistrer les changements.", ex);
            }
        }

        private static async Task InvokeDbAsync(Func<Task> action, string contexte) =>
            await InvokeDbAsync(async () =>
            {
                await action();
                return 0;
            }, contexte);

        public Task<List<QuizLocal>> GetAllAsync(CancellationToken cancellationToken = default) =>
            InvokeDbAsync(() => _db.Quiz
                .OrderByDescending(q => q.DateCreation)
                .ToListAsync(cancellationToken), "Liste des quiz");

        /// <summary>Recherche texte (titre, cours source, description), tri et pagination.</summary>
        public Task<List<QuizLocal>> QueryQuizzesAsync(
            string? rechercheTexte,
            QuizListSort tri,
            bool ordreDescendant,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var query = _db.Quiz.AsQueryable();
                if (!string.IsNullOrWhiteSpace(rechercheTexte))
                {
                    string t = rechercheTexte.Trim();
                    query = query.Where(q =>
                        (q.Titre != null && q.Titre.Contains(t))
                        || (q.CoursSourceTitre != null && q.CoursSourceTitre.Contains(t))
                        || (q.Description != null && q.Description.Contains(t)));
                }

                query = tri switch
                {
                    QuizListSort.Titre => ordreDescendant
                        ? query.OrderByDescending(q => q.Titre)
                        : query.OrderBy(q => q.Titre),
                    QuizListSort.NombreQuestions => ordreDescendant
                        ? query.OrderByDescending(q => q.NombreQuestions)
                        : query.OrderBy(q => q.NombreQuestions),
                    _ => ordreDescendant
                        ? query.OrderByDescending(q => q.DateCreation)
                        : query.OrderBy(q => q.DateCreation)
                };

                return await query.Skip(Math.Max(0, skip)).Take(Math.Max(0, take)).ToListAsync(cancellationToken);
            }, "Recherche / pagination des quiz");

        /// <summary>Réinitialise les identifiants serveur lorsque le quiz n’existe plus côté API.</summary>
        public Task EffacerIdentifiantsServeurAsync(int quizLocalId, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz.FindAsync(new object[] { quizLocalId }, cancellationToken);
                if (quiz == null)
                    return;
                quiz.BackendQuizId = null;
                quiz.BackendQuizIdPublicationWeb = null;
                quiz.BackendQuizIdQr = null;
                quiz.QrLiveSessionToken = string.Empty;
                await _db.SaveChangesAsync(cancellationToken);
            }, "Effacement des liens serveur");

        public Task<QuizLocal?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(() => _db.Quiz
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id, cancellationToken), "Chargement du quiz");

        public Task<QuizLocal> AjouterAsync(QuizLocal quiz, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                _db.Quiz.Add(quiz);
                await _db.SaveChangesAsync(cancellationToken);
                return quiz;
            }, "Création du quiz");

        public Task ModifierAsync(QuizLocal quiz, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                _db.Quiz.Update(quiz);
                await _db.SaveChangesAsync(cancellationToken);
            }, "Mise à jour du quiz");

        /// <summary>
        /// Supprime le quiz et <b>toutes</b> les données locales associées : liaisons cours (N-N), questions, réponses.
        /// Ordre explicite pour SQLite / EF (pas d’orphelins dans <c>reponse_locale</c>, <c>question_locale</c>, table de liaison).
        /// </summary>
        /// <summary>Alias métier après suppression backend confirmée (ou suppression locale forcée).</summary>
        public Task SupprimerLocalement(int quizLocalId, CancellationToken cancellationToken = default) =>
            SupprimerAsync(quizLocalId, cancellationToken);

        public Task SupprimerAsync(int id, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz
                    .Include(q => q.Cours)
                    .Include(q => q.Questions)
                    .ThenInclude(qu => qu.Reponses)
                    .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

                if (quiz == null)
                    return;

                quiz.Cours.Clear();

                foreach (var question in quiz.Questions.ToList())
                {
                    if (question.Reponses != null && question.Reponses.Count > 0)
                        _db.Reponses.RemoveRange(question.Reponses);
                }

                _db.Questions.RemoveRange(quiz.Questions.ToList());
                _db.Quiz.Remove(quiz);
                await _db.SaveChangesAsync(cancellationToken);
            }, "Suppression du quiz");

        /// <summary>
        /// Lie le quiz serveur « publication web » (sans écraser une valeur déjà connue).
        /// Met aussi à jour <see cref="QuizLocal.BackendQuizId"/> pour compatibilité avec l’ancienne colonne unique.
        /// </summary>
        public Task DefinirBackendQuizIdPublicationWebSiAbsenteAsync(
            int quizLocalId,
            long backendQuizId,
            CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz
                    .FirstOrDefaultAsync(q => q.Id == quizLocalId, cancellationToken);
                if (quiz == null)
                    throw new InvalidOperationException("Quiz introuvable ou déjà supprimé.");

                if (quiz.BackendQuizIdPublicationWeb is long existant && existant > 0)
                    return;

                quiz.BackendQuizIdPublicationWeb = backendQuizId;
                quiz.BackendQuizId = backendQuizId;
                await _db.SaveChangesAsync(cancellationToken);
            }, "Lien quiz local / serveur (publication web)");

        /// <summary>Lie le quiz serveur « miroir QR » (sans écraser une valeur déjà connue).</summary>
        public Task DefinirBackendQuizIdQrSiAbsenteAsync(
            int quizLocalId,
            long backendQuizId,
            CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz
                    .FirstOrDefaultAsync(q => q.Id == quizLocalId, cancellationToken);
                if (quiz == null)
                    throw new InvalidOperationException("Quiz introuvable ou déjà supprimé.");

                if (quiz.BackendQuizIdQr is long existant && existant > 0)
                    return;

                quiz.BackendQuizIdQr = backendQuizId;
                await _db.SaveChangesAsync(cancellationToken);
            }, "Lien quiz local / serveur (QR)");

        /// <summary>Enregistre le dernier token de session QR éphémère (remplacé à chaque nouveau « Code QR »).</summary>
        public Task DefinirQrLiveSessionTokenAsync(
            int quizLocalId,
            string? sessionToken,
            CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz
                    .FirstOrDefaultAsync(q => q.Id == quizLocalId, cancellationToken);
                if (quiz == null)
                    throw new InvalidOperationException("Quiz introuvable ou déjà supprimé.");

                quiz.QrLiveSessionToken = string.IsNullOrWhiteSpace(sessionToken)
                    ? null
                    : sessionToken.Trim();
                await _db.SaveChangesAsync(cancellationToken);
            }, "Token session QR-live");

        /// <summary>Alias historique : équivalent à <see cref="DefinirBackendQuizIdPublicationWebSiAbsenteAsync"/>.</summary>
        public Task DefinirBackendQuizIdSiAbsenteAsync(
            int quizLocalId,
            long backendQuizId,
            CancellationToken cancellationToken = default) =>
            DefinirBackendQuizIdPublicationWebSiAbsenteAsync(quizLocalId, backendQuizId, cancellationToken);

        /// <summary>
        /// Marque qu’une copie serveur a pu être créée (QR, publication) pour éviter une suppression locale seule
        /// qui laisserait des lignes MySQL orphelines si <see cref="QuizLocal.BackendQuizId"/> est perdu.
        /// </summary>
        public Task MarquerServeurOuQrPotentiellementCreeAsync(
            int quizLocalId,
            CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz
                    .FirstOrDefaultAsync(q => q.Id == quizLocalId, cancellationToken);
                if (quiz == null)
                    return;

                quiz.ServeurOuQrToucheUtc ??= DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }, "Marquage quiz créé ou utilisé côté serveur");

        private static async Task<long?> TrouverIdServeurParCorrespondanceAsync(
            QuizWebPublicationApiService api,
            string bearerToken,
            QuizLocal quiz,
            CancellationToken cancellationToken)
        {
            var liste = await api.GetMesQuizProfesseurListeAsync(bearerToken, cancellationToken);
            return MesQuizCorrelation.TryResolveQuizId(liste, quiz.Titre ?? string.Empty, quiz.NombreQuestions);
        }

        /// <summary>
        /// Supprime d’abord sur le serveur chaque id distinct (<see cref="QuizLocal.BackendQuizIdPublicationWeb"/>,
        /// <see cref="QuizLocal.BackendQuizIdQr"/>, <see cref="QuizLocal.BackendQuizId"/>),
        /// ou un id retrouvé via <c>GET /api/professeur/mes-quiz</c>.
        /// Puis suppression locale (SQLite).
        /// Si un <c>DELETE</c> serveur est requis mais échoue, aucun appel à <see cref="SupprimerAsync"/>.
        /// </summary>
        /// <param name="forceLocalDeleteIfNoServerMatch">
        /// Si <c>true</c>, après une corrélation infructueuse (prof connecté, pas de signe fort serveur), supprime quand même en local.
        /// </param>
        public async Task SupprimerLocalesEtServeurAsync(
            int quizLocalId,
            string? bearerToken,
            bool forceLocalDeleteIfNoServerMatch = false,
            CancellationToken cancellationToken = default)
        {
            var quiz = await GetByIdAsync(quizLocalId, cancellationToken);
            if (quiz == null)
                return;

            bool signeFortCopieServeur =
                (quiz.BackendQuizIdPublicationWeb is long pw && pw > 0)
                || (quiz.BackendQuizIdQr is long pq && pq > 0)
                || (quiz.BackendQuizId is long id0 && id0 > 0)
                || quiz.ServeurOuQrToucheUtc.HasValue;

            var idsServeurDistincts = new HashSet<long>();
            void AjouterIdServeur(long? id)
            {
                if (id is long x && x > 0)
                    idsServeurDistincts.Add(x);
            }

            AjouterIdServeur(quiz.BackendQuizIdPublicationWeb);
            AjouterIdServeur(quiz.BackendQuizIdQr);
            AjouterIdServeur(quiz.BackendQuizId);

            var api = new QuizWebPublicationApiService();

            bool correlationTentee = false;
            if (idsServeurDistincts.Count == 0
                && !string.IsNullOrWhiteSpace(bearerToken)
                && MesQuizCorrelation.TitreNonVide(quiz.Titre))
            {
                correlationTentee = true;
                var corrélé = await TrouverIdServeurParCorrespondanceAsync(
                    api,
                    bearerToken.Trim(),
                    quiz,
                    cancellationToken);
                if (corrélé is long c && c > 0)
                    idsServeurDistincts.Add(c);
            }

            if (idsServeurDistincts.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    throw new InvalidOperationException(
                        "Ce quiz est lié au serveur (QR ou publication web). Connectez-vous pour le supprimer aussi sur la plateforme.");
                }

                foreach (long delId in idsServeurDistincts)
                    await api.DeleteQuizAsync(bearerToken.Trim(), delId, cancellationToken);
            }
            else if (signeFortCopieServeur)
            {
                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    throw new InvalidOperationException(
                        "Une copie de ce quiz a pu être créée sur la plateforme (code QR ou publication). " +
                        "Connectez-vous avec le même compte pour la supprimer côté serveur.");
                }

                throw new InvalidOperationException(
                    "Une copie de ce quiz existe probablement sur la plateforme, mais aucun quiz correspondant " +
                    "(titre / nombre de questions) n’a été trouvé dans votre liste. Renommez ce quiz comme sur le serveur, " +
                    "supprimez le doublon sur la plateforme puis réessayez.");
            }
            else if (!string.IsNullOrWhiteSpace(bearerToken)
                     && correlationTentee
                     && !forceLocalDeleteIfNoServerMatch)
            {
                throw new QuizDeleteLocalOnlyConfirmationRequiredException(
                    "Aucun quiz correspondant sur votre compte plateforme n’a été trouvé pour ce titre.");
            }

            await SupprimerAsync(quizLocalId, cancellationToken);
        }

        public Task ChangerStatutAsync(int id, string statut, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz.FindAsync(new object[] { id }, cancellationToken);
                if (quiz != null)
                {
                    quiz.Statut = statut;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }, "Changement de statut du quiz");

        public Task MettreAJourPublicationWebLocaleAsync(
            int quizLocalId,
            long? backendQuizId,
            string emailsPublicationWebJson,
            string statut,
            CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz.FindAsync(new object[] { quizLocalId }, cancellationToken);
                if (quiz == null)
                    throw new InvalidOperationException("Quiz introuvable ou déjà supprimé.");

                if (backendQuizId.HasValue)
                {
                    quiz.BackendQuizIdPublicationWeb = backendQuizId.Value;
                    quiz.BackendQuizId = backendQuizId.Value;
                }
                quiz.EmailsPublicationWebJson = emailsPublicationWebJson ?? string.Empty;
                quiz.Statut = statut;
                await _db.SaveChangesAsync(cancellationToken);
            }, "Mise à jour publication web locale");

        /// <summary>Met à jour un quiz existant : métadonnées et remplace toutes les questions liées.</summary>
        /// <param name="emailsPublicationWebJson">Si non null, remplace la liste JSON des emails publication web.</param>
        public Task MettreAJourContenuAsync(
            int quizId,
            string titre,
            string difficulte,
            string coursTitre,
            string statut,
            IReadOnlyList<QuestionLocale> nouvellesQuestions,
            string? emailsPublicationWebJson = null,
            CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz
                    .Include(q => q.Questions)
                    .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);
                if (quiz == null)
                    throw new InvalidOperationException("Quiz introuvable ou déjà supprimé.");

                quiz.Titre = titre;
                quiz.Difficulte = difficulte;
                quiz.CoursSourceTitre = coursTitre ?? string.Empty;
                quiz.Statut = statut;
                quiz.NombreQuestions = nouvellesQuestions.Count;

                if (emailsPublicationWebJson != null)
                    quiz.EmailsPublicationWebJson = emailsPublicationWebJson;

                var anciennes = quiz.Questions.ToList();
                if (anciennes.Count > 0)
                    _db.Questions.RemoveRange(anciennes);

                foreach (var q in nouvellesQuestions)
                {
                    q.Id = 0;
                    q.QuizLocalId = quizId;
                    _db.Questions.Add(q);
                }

                await _db.SaveChangesAsync(cancellationToken);
            }, "Mise à jour du contenu du quiz");
    }
}
