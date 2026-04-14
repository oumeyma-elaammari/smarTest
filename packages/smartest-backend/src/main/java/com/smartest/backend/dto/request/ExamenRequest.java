package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Positive;
import lombok.Data;

import java.util.List;

@Data
public class ExamenRequest {

    @NotBlank(message = "Le titre est obligatoire")
    private String titre;

    @Positive(message = "La durée doit être positive")
    private Integer duree;

    @NotNull(message = "L'identifiant du professeur est obligatoire")
    private Long professeurId;

    private Long coursId;

    private List<Long> questionsIds;
}