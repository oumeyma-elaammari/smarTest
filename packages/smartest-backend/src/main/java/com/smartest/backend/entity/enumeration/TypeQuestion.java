package com.smartest.backend.entity.enumeration;

public enum TypeQuestion {
    QCM,
    VRAI_FAUX,
    /** Plusieurs bonnes réponses (cases à cocher). */
    CASES_A_COCHER,
    /** Réponse libre (rédaction / dissertation). */
    REDACTION,
    /** Ancien libellé ; traité comme {@link #REDACTION} côté passage examen. */
    REPONSE_COURTE
}