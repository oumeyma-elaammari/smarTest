package com.smartest.backend.dto.qr;

import com.smartest.backend.dto.response.QuizPassageWebResponse;
import com.smartest.backend.dto.response.QuizQrLiveStatsResponse;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class QrLiveStreamEnvelope {

    private QrLiveStreamMessageType type;
    private QuizPassageWebResponse quiz;
    private QuizQrLiveStatsResponse stats;
    private QrAnswerFeedbackPayload feedback;
}
