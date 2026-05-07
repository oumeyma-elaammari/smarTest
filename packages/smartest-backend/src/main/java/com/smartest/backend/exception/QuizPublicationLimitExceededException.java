package com.smartest.backend.exception;

public class QuizPublicationLimitExceededException extends RuntimeException {

    public QuizPublicationLimitExceededException(int maxEmails) {
        super("La liste d'emails dépasse la limite autorisée (" + maxEmails + " adresses maximum).");
    }
}
