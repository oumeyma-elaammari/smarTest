package com.smartest.backend.dto.response;

import lombok.Data;

@Data
public class QuestionCorrectionWebResponse {
    private Long questionId;
    private String enonce;
    private Long reponseChoisieId;
    private String reponseChoisieContenu;
    private Long reponseCorrecteId;
    private String reponseCorrecteContenu;
    private boolean correcte;
    private String explication;
}
