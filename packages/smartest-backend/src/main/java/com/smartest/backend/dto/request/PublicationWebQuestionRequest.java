package com.smartest.backend.dto.request;

import lombok.Data;

@Data
public class PublicationWebQuestionRequest {
    
    private String type;
    private String enonce;
    private String optionA;
    private String optionB;
    private String optionC;
    private String optionD;
    private String reponseCorrecte;
    private String explication;
    private String difficulte;

    /** Secondes (≥ 5), optionnel — défini depuis le bureau lors de la publication web de l'examen. */
    private Integer dureeSecondesIndicative;

    private Double baremePoints;

    // CHECKBOX 
    private String reponsesCorrectesJson;

    // REDACTION 
    private String reponseModele;

    // Image embarquée 
    private String imageBase64;

    // MIME jpeg,png 
    private String imageType;
}
