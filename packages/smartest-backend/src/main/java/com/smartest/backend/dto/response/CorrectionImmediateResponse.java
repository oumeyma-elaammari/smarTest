package com.smartest.backend.dto.response;

import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class CorrectionImmediateResponse {

    private Boolean estCorrecte;

    private String message;           // "✅ Bonne réponse !" ou "❌ Mauvaise réponse"

    private String bonneReponse;      // ← La bonne réponse à afficher

    private String progression;      // "3/10"

    private Boolean estTermine;

    private Double noteActuelle;
    private Double noteFinale;
    private String messageFinal;
    private Integer score;
    private Integer totalQuestions;

    public CorrectionImmediateResponse(boolean estCorrecte, String message, String bonneReponse, String progression, boolean estTermine, Double noteActuelle) {
        this.estCorrecte = estCorrecte;
        this.message = message;
        this.bonneReponse = bonneReponse;
        this.progression = progression;
        this.estTermine = estTermine;
        this.noteActuelle = noteActuelle;
    }
}