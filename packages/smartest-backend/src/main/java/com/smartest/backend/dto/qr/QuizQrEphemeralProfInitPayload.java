package com.smartest.backend.dto.qr;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

import lombok.Data;

import java.util.List;

@Data
public class QuizQrEphemeralProfInitPayload {

    @NotBlank
    private String sessionToken;

    @NotBlank
    private String titre;

    @NotNull
    @Valid
    private List<QrEphemeralQuestionPayload> questions;

    @Data
    public static class QrEphemeralQuestionPayload {
        private String enonce;
        private String optionA;
        private String optionB;
        private String optionC;
        private String optionD;
        /** A, B, C ou D. */
        private String reponseCorrecte;
    }
}
