package com.smartest.backend.dto.request;

import lombok.Data;

@Data
public class PublicationWebQuestionRequest {
    private String enonce;
    private String optionA;
    private String optionB;
    private String optionC;
    private String optionD;
    private String reponseCorrecte;
    private String explication;
    private String difficulte;
}
