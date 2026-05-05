package com.smartest.backend.dto.response;

import lombok.Data;

import java.util.List;

@Data
public class ResultatQuizWebResponse {
    private double score;
    private int bonnesReponses;
    private int totalQuestions;
    private boolean estPremiereTentative;
    private List<QuestionCorrectionWebResponse> corrections;
}
