package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;
import java.time.LocalDateTime;
import java.util.List;

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
    private Long coursId;
    private String coursTitre;
    private LocalDateTime dateCreation;
    private Integer nombreQuestions;
    private List<QuestionResponse> questions;
}