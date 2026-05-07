package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class QuizQrLiveStatsResponse {
    private Long quizId;
    private String quizTitre;
    private Integer nombreParticipants;
    private Integer totalSoumissionsQuestions;
    private Double tauxReussiteGlobal;
    private List<QuestionQrLiveStatResponse> statistiquesParQuestion;
}
