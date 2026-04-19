package com.smartest.backend.dto.response;

import lombok.Data;

@Data
public class ResultatQuizResponse {
    private double score;
    private double note;
    private int bonnesReponses;
    private int totalQuestions;
    private Boolean estPremiereTentative;
}