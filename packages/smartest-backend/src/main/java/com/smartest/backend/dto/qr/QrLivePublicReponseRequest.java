package com.smartest.backend.dto.qr;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

@Data
public class QrLivePublicReponseRequest {

    /** Identifiant stable côté client (navigateur) pour dédoublonner les participants. */
    @NotBlank
    private String participantId;

    @NotBlank
    private String correlationId;

    /** Identifiant question ({@link QuestionPassageWebResponse#getId()}). */
    private long questionId;

    /** Identifiant réponse choisie ({@link ReponsePassageWebResponse#getId()}). */
    private long reponseId;
}
