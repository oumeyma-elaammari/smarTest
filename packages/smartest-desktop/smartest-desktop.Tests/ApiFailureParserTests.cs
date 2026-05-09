using System.Net;
using smartest_desktop.Exceptions;
using smartest_desktop.Helpers;
using Xunit;

namespace smartest_desktop.Tests
{
    public class ApiFailureParserTests
    {
        [Fact]
        public void TryParseValidationMap_retour_null_si_non_json_objet()
        {
            Assert.Null(ApiFailureParser.TryParseValidationMap("erreur brute"));
            Assert.Null(ApiFailureParser.TryParseValidationMap(""));
        }

        [Fact]
        public void TryParseValidationMap_lit_champs_validation_spring()
        {
            const string json = """{"professeurId":"L'identifiant du professeur est obligatoire","titre":"obligatoire"}""";

            var map = ApiFailureParser.TryParseValidationMap(json);

            Assert.NotNull(map);
            Assert.Equal(2, map!.Count);
            Assert.Contains("obligatoire", map["titre"]);
        }

        [Fact]
        public void BuildMessage_join_les_messages_validation()
        {
            var body = "{\"a\":\"erreur un\",\"b\":\"erreur deux\"}";

            var msg = ApiFailureParser.BuildMessage(HttpStatusCode.BadRequest, body);

            Assert.Contains("erreur un", msg);
            Assert.Contains("erreur deux", msg);
        }

        [Fact]
        public void BuildMessage_retourne_texte_plat_si_pas_de_json_objet()
        {
            const string plain = "Ce quiz n'appartient pas à votre compte";
            var msg = ApiFailureParser.BuildMessage(HttpStatusCode.Forbidden, plain);
            Assert.Equal(plain, msg);
        }

        [Fact]
        public void BuildMessage_dequote_chaine_json_serveur()
        {
            var body = "\"Mot de passe expiré\"";
            var msg = ApiFailureParser.BuildMessage(HttpStatusCode.BadRequest, body);
            Assert.Equal("Mot de passe expiré", msg);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized, "Reconnectez-vous")]
        [InlineData(HttpStatusCode.Forbidden, "Accès refusé")]
        [InlineData(HttpStatusCode.NotFound, "Ressource introuvable")]
        [InlineData(HttpStatusCode.BadRequest, "refusée")]
        [InlineData((HttpStatusCode)503, "Erreur serveur")]
        [InlineData((HttpStatusCode)418, "Réessayez")]
        public void BuildMessage_fallback_selon_statut_si_corps_vide(HttpStatusCode status, string fragmentAttendu)
        {
            var msg = ApiFailureParser.BuildMessage(status, "");
            Assert.Contains(fragmentAttendu, msg);
        }

        [Fact]
        public void SmartestApiException_prefixed_context_operation()
        {
            var ex = SmartestApiException.FromHttpFailure(
                HttpStatusCode.BadRequest,
                "liste emails refusée",
                "Publication web");

            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            Assert.Contains("Publication web", ex.Message);
            Assert.Contains("liste emails refusée", ex.Message);
        }

        [Fact]
        public void SmartestApiException_remplit_ValidationErrors_si_json_validation()
        {
            const string json = """{"titre":"Le titre est obligatoire","duree":"minimum 1"}""";

            var ex = SmartestApiException.FromHttpFailure(HttpStatusCode.BadRequest, json);

            Assert.Equal(2, ex.ValidationErrors.Count);
            Assert.Equal("Le titre est obligatoire", ex.ValidationErrors["titre"]);
            Assert.Contains("obligatoire", ex.Message);
            Assert.Contains("minimum", ex.Message);
        }

        [Fact]
        public void TryParseValidationMap_retour_null_si_corps_vide()
        {
            Assert.Null(ApiFailureParser.TryParseValidationMap(null));
            Assert.Null(ApiFailureParser.TryParseValidationMap("   "));
        }

        [Fact]
        public void BuildMessage_corps_HTML_retourne_html_trimme_quand_pas_objet_validation()
        {
            var html = "<html><body>erreur</body></html>";
            var msg = ApiFailureParser.BuildMessage(HttpStatusCode.BadRequest, html);
            Assert.Contains("html", msg, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildMessage_corps_vide_utilise_fallback()
        {
            var msg = ApiFailureParser.BuildMessage(HttpStatusCode.InternalServerError, null);
            Assert.Contains("serveur", msg, StringComparison.OrdinalIgnoreCase);
        }
    }
}
