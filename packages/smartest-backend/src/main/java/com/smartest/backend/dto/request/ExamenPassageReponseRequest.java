package com.smartest.backend.dto.request;

import lombok.Data;

import java.util.List;

/**
 * Corps JSON POST {@code /passage/reponse} : une seule forme active selon le type de question.
 */
@Data
public class ExamenPassageReponseRequest {
    private Long questionId;
    /** QCM ou Vrai/Faux : une seule réponse (id entité {@link com.smartest.backend.entity.Reponse}). */
    private Long reponseId;
    /** Cases à cocher : plusieurs ids de réponses sélectionnées. */
    private List<Long> reponseIds;
    /** Rédaction : texte libre. */
    private String reponseTexte;
}
