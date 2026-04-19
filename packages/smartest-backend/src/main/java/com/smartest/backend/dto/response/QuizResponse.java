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
    private Integer duree;
    private String description;

    private Long professeurId;
    private String professeurNom;


    private LocalDateTime dateCreation;

    // 🔥 AJOUTS
    private StatutQuiz statut;
    private LocalDateTime datePublication;

    private Integer nombreQuestions;
    private List<QuestionResponse> questions;
}