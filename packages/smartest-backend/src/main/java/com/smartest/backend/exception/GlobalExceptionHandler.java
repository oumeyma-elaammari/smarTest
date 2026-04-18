package com.smartest.backend.exception;


import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.FieldError;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

import java.util.HashMap;
import java.util.Map;

@RestControllerAdvice
public class GlobalExceptionHandler {

    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<Map<String, String>> handleValidationErrors(
            MethodArgumentNotValidException ex) {

        Map<String, String> errors = new HashMap<>();

        ex.getBindingResult().getAllErrors().forEach(error -> {
            String field = ((FieldError) error).getField();
            String message = error.getDefaultMessage();
            errors.put(field,message);
        });

        return ResponseEntity
                .status(HttpStatus.BAD_REQUEST)  // 400
                .body(errors);
    }

    // ── Email déjà utilisé ─────────────────────────────────────
    @ExceptionHandler(EmailAlreadyUsedException.class)
    public ResponseEntity<String> handleEmailAlreadyUsed(
            EmailAlreadyUsedException ex) {
        return ResponseEntity
                .status(HttpStatus.CONFLICT)  // 409
                .body(ex.getMessage());
    }

    // ── Passwords différents ───────────────────────────────────
    @ExceptionHandler(PasswordMismatchException.class)
    public ResponseEntity<String> handlePasswordMismatch(
            PasswordMismatchException ex) {
        return ResponseEntity
                .status(HttpStatus.BAD_REQUEST)  // 400
                .body(ex.getMessage());
    }

    @ExceptionHandler(EmailNotVerifiedException.class)
    public ResponseEntity<String> handleEmailNotVerified(
            EmailNotVerifiedException ex) {
        return ResponseEntity
                .status(HttpStatus.FORBIDDEN)   // 403
                .body(ex.getMessage());
    }

    @ExceptionHandler(InvalidTokenException.class)
    public ResponseEntity<String> handleInvalidToken(
            InvalidTokenException ex) {
        return ResponseEntity
                .status(HttpStatus.BAD_REQUEST)  // 400
                .body(ex.getMessage());
    }

    // ── Mot de passe incorrect ─────────────────────────────────
    @ExceptionHandler(InvalidPasswordException.class)
    public ResponseEntity<String> handleInvalidPassword(
            InvalidPasswordException ex) {
        return ResponseEntity
                .status(HttpStatus.UNAUTHORIZED)  // 401
                .body(ex.getMessage());
    }

    // ── Compte introuvable ─────────────────────────────────────
    @ExceptionHandler(AccountNotFoundException.class)
    public ResponseEntity<String> handleAccountNotFound(
            AccountNotFoundException ex) {
        return ResponseEntity
                .status(HttpStatus.UNAUTHORIZED)  // 401
                .body(ex.getMessage());
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<String> handleGeneric(Exception ex) {
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body("Erreur interne : " + ex.getMessage());
    }


}