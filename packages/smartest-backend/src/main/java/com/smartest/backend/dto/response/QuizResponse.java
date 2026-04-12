package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class QuizResponse {

    private Long id;
    private String titre;
    private Integer duree;
    private Long professeurId;
    private String professeurNom;
    private Long coursId;
    private String coursTitre;
    //private List<QuestionResponse> questions;
}
