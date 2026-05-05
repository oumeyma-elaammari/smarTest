package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
public class VerificationQuestionWebRequest {

    @NotNull
    private Long questionId;

    @NotNull
    private Long reponseId;
}
