package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class ExamenCorrectionLigneResponse {
    private Long questionId;
    private String enonce;
    private String typeQuestion;
    private String reponseEtudiantLibelle;
    private String reponseCorrecte;
    private String reponseJson;
    private Double scorePartiel;
    private Double noteFinaleLigne;
    private Double pointsQuestionMax;
    private boolean corrigeParIa;
    private String groqStatut;
}
