package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
public class ReponseEtudiantRequest {

    @NotNull(message = "L'identifiant de la question est obligatoire")
    private Long questionId;

    @NotNull(message = "L'identifiant de la réponse choisie est obligatoire")
    private Long reponseId;

    @NotNull(message = "L'identifiant de l'étudiant est obligatoire")
    private Long etudiantId;
}