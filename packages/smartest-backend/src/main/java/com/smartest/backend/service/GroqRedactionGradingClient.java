package com.smartest.backend.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.config.GroqProperties;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.OptionalDouble;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * Appel REST Groq (chat completions) — correction sémantique des questions rédaction.
 */
@Component
@Slf4j
public class GroqRedactionGradingClient {

    private static final URI GROQ_URI = URI.create("https://api.groq.com/openai/v1/chat/completions");
    private static final Pattern NOTE_JSON = Pattern.compile("\"note\"\\s*:\\s*([0-9]+(?:\\.[0-9]+)?)");

    private final GroqProperties properties;
    private final GroqApiKeyRegistry groqApiKeyRegistry;
    private final ObjectMapper objectMapper;

    public GroqRedactionGradingClient(
            GroqProperties properties,
            GroqApiKeyRegistry groqApiKeyRegistry,
            ObjectMapper objectMapper) {
        this.properties = properties;
        this.groqApiKeyRegistry = groqApiKeyRegistry;
        this.objectMapper = objectMapper;
    }

    /**
     * Note sur l'échelle {@code barèmePromptSur10} (10), convertie vers les points de la question.
     */
    public OptionalDouble demanderNoteSurBarèmePrompt(
            Long examenPublieId,
            String enonceQuestion,
            String reponseEtudiant,
            String reponseModele,
            double barèmePromptSur10,
            double pointsQuestion) {
        Optional<String> apiKey = resolveApiKey(examenPublieId);
        if (apiKey.isEmpty()) {
            log.warn("Groq: aucune clé API (variable GROQ_API_KEY ou en-tête X-Groq-Api-Key au lancement) — rédaction non notée.");
            return OptionalDouble.empty();
        }
        if (reponseEtudiant == null || reponseEtudiant.isBlank()) {
            return OptionalDouble.of(0.0);
        }

        String enonce = enonceQuestion != null ? enonceQuestion.trim() : "";
        String modele = reponseModele != null ? reponseModele.trim() : "";
        String etudiant = reponseEtudiant.trim();

        String system = """
                Tu es un correcteur d'examen bienveillant et rigoureux.
                Tu réponds UNIQUEMENT par un JSON compact sur une seule ligne, sans markdown, de la forme {"note":N}
                où N est un nombre décimal entre 0 et l'échelle demandée.""";

        String user = """
                Évalue la réponse de l'étudiant sur une échelle de 0 à %.2f (10 = excellent).

                Règles importantes :
                - N'utilise AUCUNE comparaison lettre par lettre, mot pour mot ou similarité de chaînes de caractères.
                - Évalue uniquement le sens, les idées et la pertinence par rapport à l'énoncé.
                - La réponse n'a PAS besoin d'être identique à la réponse modèle.
                - Accorde une bonne note si les concepts essentiels sont présents, même avec d'autres formulations.
                - Formulations synonymes ou exemples différents mais justes : note élevée.
                - Réponse vide, hors sujet ou sans contenu pertinent : note proche de 0.
                - Réponse partiellement juste : note proportionnelle.

                Énoncé de la question :
                %s

                Réponse modèle (référence pédagogique — guide d'évaluation, pas une copie exigée) :
                %s

                Réponse de l'étudiant :
                %s

                Réponds uniquement : {"note":<nombre>} avec N entre 0 et %.2f.
                """.formatted(
                barèmePromptSur10,
                enonce.isEmpty() ? "(non précisé)" : enonce,
                modele.isEmpty() ? "(aucune réponse modèle — évalue la pertinence par rapport à l'énoncé)" : modele,
                etudiant,
                barèmePromptSur10);

        List<Map<String, String>> messages = new ArrayList<>();
        messages.add(Map.of("role", "system", "content", system));
        messages.add(Map.of("role", "user", "content", user));

        Map<String, Object> payload = new LinkedHashMap<>();
        payload.put("model", properties.getModel());
        payload.put("temperature", 0.15);
        payload.put("messages", messages);

        HttpClient client = HttpClient.newBuilder()
                .connectTimeout(Duration.ofSeconds(Math.min(30, properties.getTimeoutSeconds())))
                .build();

        int attempts = 0;
        int max = Math.max(1, properties.getMaxRetries());
        while (attempts < max) {
            attempts++;
            try {
                String json = objectMapper.writeValueAsString(payload);
                HttpRequest req = HttpRequest.newBuilder(GROQ_URI)
                        .timeout(Duration.ofSeconds(properties.getTimeoutSeconds()))
                        .header("Authorization", "Bearer " + apiKey.get())
                        .header("Content-Type", "application/json")
                        .POST(HttpRequest.BodyPublishers.ofString(json, StandardCharsets.UTF_8))
                        .build();
                HttpResponse<String> res = client.send(req, HttpResponse.BodyHandlers.ofString(StandardCharsets.UTF_8));
                if (res.statusCode() / 100 != 2) {
                    log.warn("Groq HTTP {} — {}", res.statusCode(), truncate(res.body(), 400));
                    continue;
                }
                double note010 = extraireNote010(res.body());
                if (Double.isNaN(note010)) {
                    log.warn("Groq: impossible d'extraire la note du corps: {}", truncate(res.body(), 400));
                    continue;
                }
                note010 = Math.max(0, Math.min(barèmePromptSur10, note010));
                double scoreQuestion = (note010 / barèmePromptSur10) * pointsQuestion;
                scoreQuestion = Math.round(scoreQuestion * 100.0) / 100.0;
                return OptionalDouble.of(Math.max(0, Math.min(pointsQuestion, scoreQuestion)));
            } catch (Exception ex) {
                log.warn("Groq appel échoué (tentative {}/{}): {}", attempts, max, ex.getMessage());
            }
        }
        return OptionalDouble.empty();
    }

    public OptionalDouble noterRedaction(
            Long examenPublieId,
            String enonceQuestion,
            String reponseEtudiant,
            String reponseModele,
            double pointsQuestion) {
        return demanderNoteSurBarèmePrompt(
                examenPublieId, enonceQuestion, reponseEtudiant, reponseModele, 10.0, pointsQuestion);
    }

    boolean estCleDisponible(Long examenPublieId) {
        return resolveApiKey(examenPublieId).isPresent();
    }

    private Optional<String> resolveApiKey(Long examenPublieId) {
        Optional<String> fromSession = groqApiKeyRegistry.resolveForExamen(examenPublieId);
        if (fromSession.isPresent()) {
            return fromSession;
        }
        String env = properties.getApiKey();
        if (env != null && !env.isBlank()) {
            return Optional.of(env.trim());
        }
        return Optional.empty();
    }

    private double extraireNote010(String responseBody) {
        try {
            JsonNode root = objectMapper.readTree(responseBody);
            JsonNode content = root.path("choices").path(0).path("message").path("content");
            if (content.isTextual()) {
                String txt = content.asText().trim();
                Matcher m = NOTE_JSON.matcher(txt);
                if (m.find()) {
                    return Double.parseDouble(m.group(1));
                }
                if (txt.startsWith("{")) {
                    JsonNode inner = objectMapper.readTree(txt);
                    if (inner.has("note") && inner.get("note").isNumber()) {
                        return inner.get("note").asDouble();
                    }
                }
            }
        } catch (Exception ignored) {
        }
        return Double.NaN;
    }

    private static String truncate(String s, int max) {
        if (s == null) {
            return "";
        }
        return s.length() <= max ? s : s.substring(0, max) + "…";
    }
}
