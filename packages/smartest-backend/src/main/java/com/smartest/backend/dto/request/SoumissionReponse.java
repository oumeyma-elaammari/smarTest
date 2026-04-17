package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class SoumissionReponse {

    @NotNull(message = "L'ID de la question est obligatoire")
    private Long questionId;

    @NotBlank(message = "La réponse ne peut pas être vide")
    private String reponse;

    private Long sessionExamenId;

    private Long quizId;

    @Builder.Default
    private Boolean estReponseFinale = false;

    @Builder.Default
    private Double tempsReponseSecondes = 0.0;

    @Builder.Default
    private Integer tentativeNumero = 1;
}