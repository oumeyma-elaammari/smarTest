package com.smartest.backend.dto.request;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Min;
import java.util.List;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class QuizRequest {

    @NotBlank(message = "Le titre est obligatoire")
    private String titre;

    @NotNull(message = "La durée est obligatoire")
    @Min(value = 1, message = "La durée doit être au moins 1 minute")
    private Integer duree;

    @NotNull(message = "L'ID du professeur est obligatoire")
    private Long professeurId;

    private Long coursId;

    private List<Long> questionsIds;
}
