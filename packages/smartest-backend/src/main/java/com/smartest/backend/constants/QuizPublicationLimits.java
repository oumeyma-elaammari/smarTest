package com.smartest.backend.constants;

/**
 * Publication web du quiz : liste d'emails autorisés stockée côté serveur.
 * Plafond pour l'import / l'assignation des étudiants autorisés à un quiz publié.
 */
public final class QuizPublicationLimits {

    private QuizPublicationLimits() {
    }

    public static final int MAX_AUTHORIZED_STUDENT_EMAILS = 2000;
}
