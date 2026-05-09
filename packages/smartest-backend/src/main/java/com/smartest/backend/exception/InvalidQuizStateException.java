package com.smartest.backend.exception;

public class InvalidQuizStateException extends RuntimeException {

    public InvalidQuizStateException(String message) {
        super(message);
    }
}
