package com.smartest.backend.dto.request;

import lombok.Data;

@Data
public class PublicationWebQuestionRequest {
    /**
     * QCM | VF | CHECKBOX | REDACTION | IMAGE (IMAGE traité comme QCM côté serveur).
     * Absent ou inconnu : QCM.
     */
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

    /** Points de la question dans le barème de l'examen (ex. 2.0). */
    private Double baremePoints;

    /** CHECKBOX : JSON du type {@code ["A","C"]} (lettres des options correctes). */
    private String reponsesCorrectesJson;

    /** REDACTION : corrigé type « réponse modèle » (stocké côté prof, non diffusé aux étudiants comme tel). */
    private String reponseModele;

    /** Image embarquée (base64 brut ou préfixe data:…;base64,). */
    private String imageBase64;

    /** MIME ex. image/jpeg, image/png */
    private String imageType;
}
