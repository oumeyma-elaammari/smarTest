package com.smartest.backend.exception;


import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.FieldError;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.server.ResponseStatusException;
import org.springframework.web.servlet.resource.NoResourceFoundException;

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

    /** Route ou fichier statique inexistant — ne pas masquer en 500 générique. */
    @ExceptionHandler(NoResourceFoundException.class)
    public ResponseEntity<Map<String, String>> handleNoResource(NoResourceFoundException ex) {
        return ResponseEntity
                .status(HttpStatus.NOT_FOUND)
                .body(Map.of(
                        "message",
                        "Ressource introuvable : " + ex.getResourcePath()));
    }

    @ExceptionHandler(ResponseStatusException.class)
    public ResponseEntity<String> handleResponseStatus(ResponseStatusException ex) {
        HttpStatus status = HttpStatus.resolve(ex.getStatusCode().value());
        if (status == null) {
            status = HttpStatus.INTERNAL_SERVER_ERROR;
        }
        String body = ex.getReason() != null ? ex.getReason() : ex.getMessage();
        return ResponseEntity.status(status).body(body);
    }

    /** Erreurs métier / paramètres (ex. créneau, transition d’état) — évite un 500 opaque côté client. */
    @ExceptionHandler(IllegalArgumentException.class)
    public ResponseEntity<Map<String, String>> handleIllegalArgument(IllegalArgumentException ex) {
        String msg = ex.getMessage() != null ? ex.getMessage() : "Requête invalide";
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("message", msg));
    }

    @ExceptionHandler(IllegalStateException.class)
    public ResponseEntity<Map<String, String>> handleIllegalState(IllegalStateException ex) {
        String msg = ex.getMessage() != null ? ex.getMessage() : "État incompatible";
        return ResponseEntity.status(HttpStatus.CONFLICT).body(Map.of("message", msg));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<String> handleGeneric(Exception ex) {
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body("Erreur interne : " + ex.getMessage());
    }


}