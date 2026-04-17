package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class QuestionRequest {

    @NotBlank(message = "L'énoncé de la question est obligatoire")
    private String enonce;

    @NotBlank(message = "Le type de la question est obligatoire")
    private String type;  // QCM, OUVERTE, VF

    private String difficulte;  // FACILE, MOYEN, DIFFICILE

    private String explication;  // Explication pédagogique

    @NotNull(message = "L'ID du professeur est obligatoire")
    private Long professeurId;

    private Long coursId;

    private java.util.List<ReponseRequest> reponses;
}