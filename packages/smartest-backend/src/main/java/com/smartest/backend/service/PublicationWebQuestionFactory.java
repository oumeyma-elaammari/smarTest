package com.smartest.backend.service;

import com.smartest.backend.dto.request.PublicationWebQuestionRequest;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;

import java.util.Locale;

/**
 * Fabrique des entités {@link Question} à partir du même DTO que la publication web du quiz.
 */
final class PublicationWebQuestionFactory {

    private PublicationWebQuestionFactory() {
    }

    static Question creerDepuisPublication(PublicationWebQuestionRequest src, Professeur professeur) {
        Question q = new Question();
        q.setEnonce(src != null && src.getEnonce() != null ? src.getEnonce().trim() : "");
        q.setType(TypeQuestion.QCM);
        q.setDifficulte(parseDifficulte(src != null ? src.getDifficulte() : null));
        q.setExplication(src != null && src.getExplication() != null ? src.getExplication().trim() : "");
        q.setProfesseur(professeur);

        String correcte = src != null && src.getReponseCorrecte() != null
                ? src.getReponseCorrecte().trim().toUpperCase(Locale.ROOT)
                : "";

        q.getReponses().add(buildReponse(q, src != null ? src.getOptionA() : null, "A".equals(correcte)));
        q.getReponses().add(buildReponse(q, src != null ? src.getOptionB() : null, "B".equals(correcte)));
        q.getReponses().add(buildReponse(q, src != null ? src.getOptionC() : null, "C".equals(correcte)));
        q.getReponses().add(buildReponse(q, src != null ? src.getOptionD() : null, "D".equals(correcte)));

        if (src != null && src.getDureeSecondesIndicative() != null) {
            int sec = src.getDureeSecondesIndicative();
            if (sec >= 5 && sec <= 7200) {
                q.setDureeSecondesIndicative(sec);
            }
        }
        return q;
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
