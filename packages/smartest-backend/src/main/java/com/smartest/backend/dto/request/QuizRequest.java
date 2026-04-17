package com.smartest.backend.dto.request;

import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class QuizRequest {

    @NotBlank(message = "Le titre du quiz est obligatoire")
    private String titre;

    @NotNull(message = "La durée du quiz est obligatoire")
    @Min(value = 1, message = "La durée doit être au moins 1 minute")
    private Integer duree;

    private Long professeurId;

    private Long coursId;

    private String description;
    private List<Long> questionsIds;  // ← Champ


    public Iterable<Long> getQuestionsIds() {
        return questionsIds;
    }
}