package com.smartest.backend.dto.response;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import java.util.List;

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
