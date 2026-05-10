package com.smartest.backend.dto.qr;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

/** Réponse HTTP après soumission d’une réponse participant (session QR). */
@Data
@NoArgsConstructor
@AllArgsConstructor
public class QrLiveSubmitAnswerResponse {
    private boolean correct;
    private long bonneReponseId;
}
