package com.smartest.backend.dto.qr;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class QrAnswerFeedbackPayload {
    private String correlationId;
    private int questionIndex;
    private boolean correcte;
}
