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
    /** Moyenne des notes sur 20 (1ʳᵉ tentative : bonnes réponses / nombre de réponses × 20 par étudiant). */
    private Double moyenneNoteSur20;
    /**
     * Effectifs par tranche de note sur 20 (ordre : 0–5, 5–8, 8–10, 10–12, 12–15, 15–18, 18–20).
     */
    private List<Integer> repartitionParTranche;
    private Long questionsAlerteCount;
    private List<StatistiqueQuestionResponse> statistiquesParQuestion;
}