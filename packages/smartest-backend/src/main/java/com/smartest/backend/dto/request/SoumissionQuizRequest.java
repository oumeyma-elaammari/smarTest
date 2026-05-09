package com.smartest.backend.dto.request;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotNull;
import lombok.Data;

import java.util.List;

@Data
public class SoumissionQuizRequest {

    @NotNull(message = "L'identifiant de l'étudiant est obligatoire")
    private Long etudiantId;

    @NotNull(message = "La liste des réponses est obligatoire")
    @Valid
    private List<ReponseQuizDTO> reponses;
}