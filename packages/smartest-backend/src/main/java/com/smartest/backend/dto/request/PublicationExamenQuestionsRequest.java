package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotEmpty;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class PublicationExamenQuestionsRequest {

    @NotEmpty(message = "Au moins une question est requise pour publier le contenu de l'examen")
    private List<PublicationWebQuestionRequest> questions;
}
