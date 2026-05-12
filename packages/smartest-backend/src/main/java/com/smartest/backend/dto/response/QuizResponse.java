package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;
import java.time.LocalDateTime;
import java.util.List;
import com.smartest.backend.entity.enumeration.StatutQuiz;
@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class QuizResponse {

    private Long id;
    private String titre;
    private String description;

    private Long professeurId;
    private String professeurNom;


    private LocalDateTime dateCreation;

    // 🔥 AJOUTS
    private StatutQuiz statut;
    private LocalDateTime datePublication;

    private Integer nombreQuestions;
    private List<QuestionResponse> questions;

    /** Renseigné pour {@code GET /api/quizs/mes-publications-web} : aucune soumission encore enregistrée pour ce quiz. */
    private Boolean premiereTentative;

    /** Renseigné pour {@code GET /api/quizs/mes-publications-web} : meilleur pourcentage parmi les tentatives (hors examen). */
    private Double meilleurScore;
}