package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

/**
 * Résumé minimal d’un quiz appartenant au professeur connecté (corrélation desktop / suppression).
 */
@Data
@NoArgsConstructor
@AllArgsConstructor
public class QuizProfesseurListeResponse {

    private Long id;
    private String titre;
    private Integer nombreQuestions;
}
