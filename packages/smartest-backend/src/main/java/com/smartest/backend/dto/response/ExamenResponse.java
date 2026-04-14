package com.smartest.backend.dto.response;

import lombok.Data;

import java.util.List;

@Data
public class ExamenResponse {

    private Long id;
    private String titre;
    private Integer duree;

    private Long professeurId;
    private String professeurNom;

    private Long coursId;
    private String coursTitre;

    private List<QuestionResponse> questions;
}