package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
@SuppressWarnings("java:S1068")
public class ReponseQuizDTO {
    private Long questionId;

    @NotNull(message = "L'identifiant de la réponse est obligatoire pour chaque entrée")
    private Long reponseId;
}