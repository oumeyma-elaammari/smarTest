package com.smartest.backend.dto.request;

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

    @NotNull(message = "L'identifiant du professeur est obligatoire")
    private Long professeurId;

    private List<Long> questionsIds;

    public Iterable<Long> getQuestionsIds() {
        return questionsIds;
    }
}