package com.smartest.backend.dto.examen;

import java.util.Collections;
import java.util.Set;

/**
 * Copie des réponses étudiant au moment de la soumission finale (hors état runtime mutable).
 */
public record StudentAnswerSnapshot(
        Long questionId,
        Long singleReponseId,
        Set<Long> reponseIdsMulti,
        String texteLibre) {

    public boolean hasReponse() {
        if (texteLibre != null && !texteLibre.isBlank()) {
            return true;
        }
        if (singleReponseId != null) {
            return true;
        }
        return reponseIdsMulti != null && !reponseIdsMulti.isEmpty();
    }

    public Set<Long> reponseIdsMultiOrEmpty() {
        return reponseIdsMulti == null ? Collections.emptySet() : reponseIdsMulti;
    }
}
