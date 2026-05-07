package com.smartest.backend.exception;


import jakarta.validation.ConstraintViolationException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.FieldError;
import org.springframework.validation.ObjectError;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.server.ResponseStatusException;
import org.springframework.web.servlet.resource.NoResourceFoundException;

import java.util.HashMap;
import java.util.Map;
import java.util.stream.Collectors;

@RestControllerAdvice
public class GlobalExceptionHandler {

    private static final String CODE_INTERNAL_ERROR = "INTERNAL_ERROR";
    private static final String PREFIX_ERREUR_INTERNE = "Erreur interne : ";

    private static Map<String, String> body(String code, String message) {
        Map<String, String> map = new HashMap<>();
        map.put("error", code);
        map.put("message", message);
        return map;
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<Map<String, String>> handleValidationErrors(
            MethodArgumentNotValidException ex) {

        Map<String, String> errors = new HashMap<>();

        ex.getBindingResult().getAllErrors().forEach(error -> {
            if (error instanceof FieldError fieldError) {
                errors.put(fieldError.getField(), fieldError.getDefaultMessage());
            } else if (error instanceof ObjectError objectError) {
                errors.put(objectError.getObjectName(), objectError.getDefaultMessage());
            }
        });

        return ResponseEntity
                .status(HttpStatus.BAD_REQUEST)
                .body(errors);
    }

    @ExceptionHandler(ConstraintViolationException.class)
    public ResponseEntity<Map<String, String>> handleConstraintViolation(ConstraintViolationException ex) {
        String msg = ex.getConstraintViolations().stream()
                .map(v -> v.getPropertyPath() + ": " + v.getMessage())
                .collect(Collectors.joining("; "));
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(body("VALIDATION_ERROR", msg));
    }

    @ExceptionHandler(IllegalArgumentException.class)
    public ResponseEntity<Map<String, String>> handleIllegalArgument(IllegalArgumentException ex) {
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(body("BAD_REQUEST", ex.getMessage()));
    }

    // ── Email déjà utilisé ─────────────────────────────────────
    @ExceptionHandler(EmailAlreadyUsedException.class)
    public ResponseEntity<Map<String, String>> handleEmailAlreadyUsed(
            EmailAlreadyUsedException ex) {
        return ResponseEntity
                .status(HttpStatus.CONFLICT)
                .body(body("EMAIL_ALREADY_USED", ex.getMessage()));
    }

    // ── Passwords différents ───────────────────────────────────
    @ExceptionHandler(PasswordMismatchException.class)
    public ResponseEntity<Map<String, String>> handlePasswordMismatch(
            PasswordMismatchException ex) {
        return ResponseEntity
                .status(HttpStatus.BAD_REQUEST)
                .body(body("PASSWORD_MISMATCH", ex.getMessage()));
    }

    @ExceptionHandler(EmailNotVerifiedException.class)
    public ResponseEntity<Map<String, String>> handleEmailNotVerified(
            EmailNotVerifiedException ex) {
        return ResponseEntity
                .status(HttpStatus.FORBIDDEN)
                .body(body("EMAIL_NOT_VERIFIED", ex.getMessage()));
    }

    @ExceptionHandler(InvalidTokenException.class)
    public ResponseEntity<Map<String, String>> handleInvalidToken(
            InvalidTokenException ex) {
        return ResponseEntity
                .status(HttpStatus.BAD_REQUEST)
                .body(body("INVALID_TOKEN", ex.getMessage()));
    }

    // ── Mot de passe incorrect ─────────────────────────────────
    @ExceptionHandler(InvalidPasswordException.class)
    public ResponseEntity<Map<String, String>> handleInvalidPassword(
            InvalidPasswordException ex) {
        return ResponseEntity
                .status(HttpStatus.UNAUTHORIZED)
                .body(body("INVALID_PASSWORD", ex.getMessage()));
    }

    // ── Compte introuvable ─────────────────────────────────────
    @ExceptionHandler(AccountNotFoundException.class)
    public ResponseEntity<Map<String, String>> handleAccountNotFound(
            AccountNotFoundException ex) {
        return ResponseEntity
                .status(HttpStatus.UNAUTHORIZED)
                .body(body("ACCOUNT_NOT_FOUND", ex.getMessage()));
    }

    @ExceptionHandler(QuizNotFoundException.class)
    public ResponseEntity<Map<String, String>> handleQuizNotFound(QuizNotFoundException ex) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).body(body("QUIZ_NOT_FOUND", ex.getMessage()));
    }

    @ExceptionHandler(SessionNotFoundException.class)
    public ResponseEntity<Map<String, String>> handleSessionNotFound(SessionNotFoundException ex) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).body(body("SESSION_NOT_FOUND", ex.getMessage()));
    }

    @ExceptionHandler(QuestionNotFoundException.class)
    public ResponseEntity<Map<String, String>> handleQuestionNotFound(QuestionNotFoundException ex) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).body(body("QUESTION_NOT_FOUND", ex.getMessage()));
    }

    @ExceptionHandler(UnauthorizedAccessException.class)
    public ResponseEntity<Map<String, String>> handleUnauthorizedAccess(UnauthorizedAccessException ex) {
        return ResponseEntity.status(HttpStatus.FORBIDDEN).body(body("FORBIDDEN", ex.getMessage()));
    }

    @ExceptionHandler(InvalidQuizStateException.class)
    public ResponseEntity<Map<String, String>> handleInvalidQuizState(InvalidQuizStateException ex) {
        return ResponseEntity.status(HttpStatus.CONFLICT).body(body("INVALID_QUIZ_STATE", ex.getMessage()));
    }

    @ExceptionHandler(InvalidSessionStateException.class)
    public ResponseEntity<Map<String, String>> handleInvalidSessionState(InvalidSessionStateException ex) {
        return ResponseEntity.status(HttpStatus.CONFLICT).body(body("INVALID_SESSION_STATE", ex.getMessage()));
    }

    @ExceptionHandler(EmailSendException.class)
    public ResponseEntity<Map<String, String>> handleEmailSend(EmailSendException ex) {
        return ResponseEntity.status(HttpStatus.SERVICE_UNAVAILABLE).body(body("EMAIL_SEND_FAILED", ex.getMessage()));
    }

    @ExceptionHandler(QuizPublicationLimitExceededException.class)
    public ResponseEntity<Map<String, String>> handleQuizPublicationLimit(
            QuizPublicationLimitExceededException ex) {
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(body("PUBLICATION_LIMIT_EXCEEDED", ex.getMessage()));
    }

    /** Route ou fichier statique inexistant — ne pas masquer en 500 générique. */
    @ExceptionHandler(NoResourceFoundException.class)
    public ResponseEntity<Map<String, String>> handleNoResource(NoResourceFoundException ex) {
        return ResponseEntity
                .status(HttpStatus.NOT_FOUND)
                .body(body("NOT_FOUND", "Ressource introuvable : " + ex.getResourcePath()));
    }

    @ExceptionHandler(ResponseStatusException.class)
    public ResponseEntity<Map<String, String>> handleResponseStatus(ResponseStatusException ex) {
        HttpStatus status = HttpStatus.resolve(ex.getStatusCode().value());
        if (status == null) {
            status = HttpStatus.INTERNAL_SERVER_ERROR;
        }
        String bodyMsg = ex.getReason() != null ? ex.getReason() : ex.getMessage();
        return ResponseEntity.status(status).body(body("HTTP_ERROR", bodyMsg != null ? bodyMsg : ""));
    }

    @ExceptionHandler(RuntimeException.class)
    public ResponseEntity<Map<String, String>> handleUnhandledRuntime(RuntimeException ex) {
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(body(CODE_INTERNAL_ERROR, PREFIX_ERREUR_INTERNE + messageOuVide(ex)));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<Map<String, String>> handleGeneric(Exception ex) {
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(body(CODE_INTERNAL_ERROR, PREFIX_ERREUR_INTERNE + messageOuVide(ex)));
    }

    private static String messageOuVide(Throwable ex) {
        return ex.getMessage() != null ? ex.getMessage() : "";
    }
}
