package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.Size;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

import com.smartest.backend.constants.QuizPublicationLimits;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class PublicationWebRequest {

    @NotEmpty(message = "Au moins un email autorisé est requis")
    @Size(max = QuizPublicationLimits.MAX_AUTHORIZED_STUDENT_EMAILS,
            message = "La liste ne peut pas dépasser 2000 emails")
    private List<String> emails;

    /**
     * Optionnel : questions du quiz envoyées par le desktop pour synchronisation serveur.
     */
    private List<PublicationWebQuestionRequest> questions;
}
