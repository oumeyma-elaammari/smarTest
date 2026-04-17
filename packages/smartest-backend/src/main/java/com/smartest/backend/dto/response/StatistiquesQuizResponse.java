package com.smartest.backend.dto.response;

import com.smartest.backend.entity.StatistiqueQuestion;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;
import java.util.List;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class StatistiquesQuizResponse {
    private Long quizId;
    private String quizTitre;
    private Integer nombreParticipants;
    private Double moyenneGenerale;
    private Double tauxReussiteGlobal;
    private Long questionsAlerteCount;
    private List<StatistiqueQuestionResponse> statistiquesParQuestion;
}