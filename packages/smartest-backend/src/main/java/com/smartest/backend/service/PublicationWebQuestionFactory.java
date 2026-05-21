package com.smartest.backend.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.dto.request.PublicationWebQuestionRequest;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;

import java.util.LinkedHashSet;
import java.util.Locale;
import java.util.Set;

/**
 * Fabrique des entités {@link Question} à partir du même DTO que la publication web du quiz,
 * avec prise en charge des types d'examen (VF, cases à cocher, rédaction).
 */
final class PublicationWebQuestionFactory {

    private static final ObjectMapper OM = new ObjectMapper();

    private PublicationWebQuestionFactory() {
    }

    static Question creerDepuisPublication(PublicationWebQuestionRequest src, Professeur professeur) {
        TypeQuestion typeQuestion = resolveType(src);
        Question q = new Question();
        q.setEnonce(trimOrEmpty(src != null ? src.getEnonce() : null));
        q.setProfesseur(professeur);
        q.setDifficulte(parseDifficulte(src != null ? src.getDifficulte() : null));
        applyImage(q, src);

        if (typeQuestion == TypeQuestion.REDACTION) {
            q.setType(TypeQuestion.REDACTION);
            String expl = trimOrEmpty(src != null ? src.getExplication() : null);
            String model = trimOrEmpty(src != null ? src.getReponseModele() : null);
            if (!model.isEmpty() && !expl.isEmpty()) {
                q.setExplication(expl + "\n\n[Réponse modèle]\n" + model);
            } else if (!model.isEmpty()) {
                q.setExplication("[Réponse modèle]\n" + model);
            } else {
                q.setExplication(expl);
            }
            q.getReponses().clear();
            applyDuration(q, src);
            applyBareme(q, src);
            return q;
        }

        q.setType(typeQuestion);

        String correcte = src != null && src.getReponseCorrecte() != null
                ? src.getReponseCorrecte().trim().toUpperCase(Locale.ROOT)
                : "";

        if (typeQuestion == TypeQuestion.VRAI_FAUX) {
            String a = firstNonBlank(src != null ? src.getOptionA() : null, "Vrai");
            String b = firstNonBlank(src != null ? src.getOptionB() : null, "Faux");
            boolean bCorrect = "B".equals(correcte) || "FAUX".equals(correcte) || "FALSE".equals(correcte) || "2".equals(correcte);
            q.getReponses().add(buildReponse(q, a, !bCorrect));
            q.getReponses().add(buildReponse(q, b, bCorrect));
            q.setExplication(trimOrEmpty(src != null ? src.getExplication() : null));
            applyDuration(q, src);
            applyBareme(q, src);
            return q;
        }

        if (typeQuestion == TypeQuestion.CASES_A_COCHER) {
            Set<String> letters = parseCheckboxLetters(src != null ? src.getReponsesCorrectesJson() : null);
            q.getReponses().add(buildReponse(q, src != null ? src.getOptionA() : null, letters.contains("A")));
            q.getReponses().add(buildReponse(q, src != null ? src.getOptionB() : null, letters.contains("B")));
            q.getReponses().add(buildReponse(q, src != null ? src.getOptionC() : null, letters.contains("C")));
            q.getReponses().add(buildReponse(q, src != null ? src.getOptionD() : null, letters.contains("D")));
            q.setExplication(trimOrEmpty(src != null ? src.getExplication() : null));
            applyDuration(q, src);
            applyBareme(q, src);
            return q;
        }

        /* QCM / IMAGE : une seule bonne réponse parmi quatre propositions. */
        q.setType(TypeQuestion.QCM);
        q.getReponses().add(buildReponse(q, src != null ? src.getOptionA() : null, "A".equals(correcte)));
        q.getReponses().add(buildReponse(q, src != null ? src.getOptionB() : null, "B".equals(correcte)));
        q.getReponses().add(buildReponse(q, src != null ? src.getOptionC() : null, "C".equals(correcte)));
        q.getReponses().add(buildReponse(q, src != null ? src.getOptionD() : null, "D".equals(correcte)));
        q.setExplication(trimOrEmpty(src != null ? src.getExplication() : null));
        applyDuration(q, src);
        applyBareme(q, src);
        return q;
    }

    private static TypeQuestion resolveType(PublicationWebQuestionRequest src) {
        if (src == null || src.getType() == null || src.getType().isBlank()) {
            return TypeQuestion.QCM;
        }
        String t = src.getType().trim().toUpperCase(Locale.ROOT);
        return switch (t) {
            case "VF", "VRAI_FAUX" -> TypeQuestion.VRAI_FAUX;
            case "CHECKBOX", "CASES_A_COCHER" -> TypeQuestion.CASES_A_COCHER;
            case "REDACTION", "DISSERTATION", "ESSAY", "LIBRE", "REPONSE_COURTE" -> TypeQuestion.REDACTION;
            case "IMAGE" -> TypeQuestion.QCM;
            default -> TypeQuestion.QCM;
        };
    }

    private static Set<String> parseCheckboxLetters(String json) {
        Set<String> out = new LinkedHashSet<>();
        if (json == null || json.isBlank()) {
            return out;
        }
        String trimmed = json.trim();
        try {
            JsonNode root = OM.readTree(trimmed);
            if (root.isArray()) {
                for (JsonNode n : root) {
                    if (n.isTextual()) {
                        addLetter(out, n.asText());
                    } else if (n.isIntegralNumber()) {
                        int v = n.intValue();
                        if (v >= 1 && v <= 4) {
                            out.add(String.valueOf((char) ('A' + v - 1)));
                        }
                    }
                }
            }
        } catch (Exception ignored) {
            String cleaned = trimmed.replace("[", "").replace("]", "").replace("\"", "");
            for (String part : cleaned.split(",")) {
                addLetter(out, part);
            }
        }
        return out;
    }

    private static void addLetter(Set<String> out, String raw) {
        if (raw == null) {
            return;
        }
        String u = raw.trim().toUpperCase(Locale.ROOT);
        if (u.isEmpty()) {
            return;
        }
        char c = u.charAt(0);
        if (c >= 'A' && c <= 'D') {
            out.add(String.valueOf(c));
        }
    }

    private static void applyImage(Question q, PublicationWebQuestionRequest src) {
        if (src == null) {
            q.setImageBase64(null);
            q.setImageType(null);
            return;
        }
        String normalized = normalizeImageBase64(src.getImageBase64());
        if (normalized == null) {
            q.setImageBase64(null);
            q.setImageType(null);
            return;
        }
        q.setImageBase64(normalized);
        String mime = trimOrEmpty(src.getImageType());
        q.setImageType(mime.isEmpty() ? "image/jpeg" : mime);
    }

    private static String normalizeImageBase64(String raw) {
        if (raw == null || raw.isBlank()) {
            return null;
        }
        String s = raw.trim();
        int idx = s.indexOf("base64,");
        if (idx >= 0) {
            s = s.substring(idx + "base64,".length()).trim();
        }
        return s.isEmpty() ? null : s;
    }

    private static void applyDuration(Question q, PublicationWebQuestionRequest src) {
        if (src != null && src.getDureeSecondesIndicative() != null) {
            int sec = src.getDureeSecondesIndicative();
            if (sec >= 5 && sec <= 7200) {
                q.setDureeSecondesIndicative(sec);
            }
        }
    }

    private static void applyBareme(Question q, PublicationWebQuestionRequest src) {
        if (src == null || src.getBaremePoints() == null) {
            return;
        }
        double pts = src.getBaremePoints();
        if (pts > 0) {
            q.setBaremePoints(pts);
        }
    }

    private static String trimOrEmpty(String s) {
        return s == null ? "" : s.trim();
    }

    private static String firstNonBlank(String preferred, String fallback) {
        String p = trimOrEmpty(preferred);
        return p.isEmpty() ? fallback : p;
    }

    private static Reponse buildReponse(Question q, String contenu, boolean correcte) {
        Reponse r = new Reponse();
        r.setQuestion(q);
        r.setContenu(contenu == null ? "" : contenu.trim());
        r.setCorrecte(correcte);
        return r;
    }

    private static Difficulte parseDifficulte(String raw) {
        if (raw == null || raw.isBlank()) {
            return Difficulte.MOYEN;
        }
        String n = raw.trim().toUpperCase(Locale.ROOT);
        return switch (n) {
            case "FACILE" -> Difficulte.FACILE;
            case "DIFFICILE" -> Difficulte.DIFFICILE;
            default -> Difficulte.MOYEN;
        };
    }
}
