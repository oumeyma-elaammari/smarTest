package com.smartest.backend.dto.qr;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

@Data
public class QuizQrEphemeralStudentAnswerPayload {

    @NotBlank
    private String sessionToken;

    @NotBlank
    private String participantId;

    @NotBlank
    private String correlationId;

    /** Même valeur que {@code QuestionPassageWebResponse.id} dans le snapshot envoyé aux élèves. */
    private long questionId;

    @NotBlank
    private String lettreChoix;
}
