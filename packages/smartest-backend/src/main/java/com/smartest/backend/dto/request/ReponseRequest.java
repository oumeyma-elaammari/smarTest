package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
public class ReponseRequest {

    // Pour la soumission d'une réponse par l'étudiant
    private Long questionId;
    private Long reponseId;
    private Long etudiantId;
    private Long sessionId;

    // Pour la création/modification d'une réponse par le professeur
    private String contenu;
    private Boolean correcte;
}