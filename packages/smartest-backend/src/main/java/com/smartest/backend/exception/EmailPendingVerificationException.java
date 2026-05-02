package com.smartest.backend.exception;

public class EmailPendingVerificationException extends RuntimeException {
    public EmailPendingVerificationException() {
        super("Email already registered but not verified. Verification email resent.");
    }
}
