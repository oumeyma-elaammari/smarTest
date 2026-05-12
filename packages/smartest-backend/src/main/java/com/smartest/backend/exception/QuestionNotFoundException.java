package com.smartest.backend.exception;

public class QuestionNotFoundException extends RuntimeException {

    public QuestionNotFoundException(Long questionId) {
        super("Question introuvable : " + questionId);
    }

    public QuestionNotFoundException(String message) {
        super(message);
    }
}
