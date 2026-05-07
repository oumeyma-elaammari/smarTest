package com.smartest.backend.exception;

import jakarta.validation.ConstraintViolationException;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.BindingResult;
import org.springframework.validation.FieldError;
import org.springframework.web.bind.MethodArgumentNotValidException;

import java.util.List;
import java.util.Map;
import java.util.Set;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("GlobalExceptionHandler — Tests complets")
class GlobalExceptionHandlerTest {
    private static final String KEY_ERROR = "error";
    private static final String KEY_MESSAGE = "message";
    private static final String FIELD_EMAIL = "email";

    @InjectMocks
    private GlobalExceptionHandler handler;

    @Test
    @DisplayName("409 — EmailAlreadyUsedException")
    void handleEmailAlreadyUsedReturns409() {
        EmailAlreadyUsedException ex = new EmailAlreadyUsedException("test@test.com");

        ResponseEntity<Map<String, String>> response = handler.handleEmailAlreadyUsed(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.CONFLICT);
        assertThat(response.getBody()).containsEntry(KEY_ERROR, "EMAIL_ALREADY_USED");
        assertThat(response.getBody()).containsKey(KEY_MESSAGE);
    }

    @Test
    @DisplayName("400 — PasswordMismatchException")
    void handlePasswordMismatchReturns400() {
        PasswordMismatchException ex = new PasswordMismatchException();

        ResponseEntity<Map<String, String>> response = handler.handlePasswordMismatch(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
    }

    @Test
    @DisplayName("401 — InvalidPasswordException")
    void handleInvalidPasswordReturns401() {
        InvalidPasswordException ex = new InvalidPasswordException();

        ResponseEntity<Map<String, String>> response = handler.handleInvalidPassword(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.UNAUTHORIZED);
    }

    @Test
    @DisplayName("401 — AccountNotFoundException")
    void handleAccountNotFoundReturns401() {
        AccountNotFoundException ex = new AccountNotFoundException("inconnu@test.com");

        ResponseEntity<Map<String, String>> response = handler.handleAccountNotFound(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.UNAUTHORIZED);
    }

    @Test
    @DisplayName("403 — EmailNotVerifiedException")
    void handleEmailNotVerifiedReturns403() {
        EmailNotVerifiedException ex = new EmailNotVerifiedException();

        ResponseEntity<Map<String, String>> response = handler.handleEmailNotVerified(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.FORBIDDEN);
    }

    @Test
    @DisplayName("400 — InvalidTokenException")
    void handleInvalidTokenReturns400() {
        InvalidTokenException ex = new InvalidTokenException();

        ResponseEntity<Map<String, String>> response = handler.handleInvalidToken(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
    }

    @Test
    @DisplayName("400 — MethodArgumentNotValidException")
    void handleValidationErrorsReturns400() {
        BindingResult bindingResult = mock(BindingResult.class);
        FieldError fieldError = new FieldError("req", FIELD_EMAIL, "L'email est obligatoire");
        when(bindingResult.getAllErrors()).thenReturn(List.of(fieldError));

        MethodArgumentNotValidException ex = mock(MethodArgumentNotValidException.class);
        when(ex.getBindingResult()).thenReturn(bindingResult);

        ResponseEntity<Map<String, String>> response =
                handler.handleValidationErrors(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
        assertThat(response.getBody()).containsKey(FIELD_EMAIL);
        assertThat(response.getBody().get(FIELD_EMAIL)).isEqualTo("L'email est obligatoire");
    }

    @Test
    @DisplayName("400 — ConstraintViolationException")
    void handleConstraintViolationReturns400() {
        ConstraintViolationException ex = new ConstraintViolationException(Set.of());

        ResponseEntity<Map<String, String>> response = handler.handleConstraintViolation(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
        assertThat(response.getBody()).containsEntry(KEY_ERROR, "VALIDATION_ERROR");
    }

    @Test
    @DisplayName("404 — QuizNotFoundException")
    void handleQuizNotFoundReturns404() {
        QuizNotFoundException ex = new QuizNotFoundException("Quiz absent");

        ResponseEntity<Map<String, String>> response = handler.handleQuizNotFound(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.NOT_FOUND);
        assertThat(response.getBody()).containsEntry(KEY_ERROR, "QUIZ_NOT_FOUND");
        assertThat(response.getBody().get(KEY_MESSAGE)).contains("absent");
    }

    @Test
    @DisplayName("403 — UnauthorizedAccessException")
    void handleUnauthorizedAccessReturns403() {
        UnauthorizedAccessException ex = new UnauthorizedAccessException("Ressource d'un autre professeur");

        ResponseEntity<Map<String, String>> response = handler.handleUnauthorizedAccess(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.FORBIDDEN);
        assertThat(response.getBody()).containsEntry(KEY_ERROR, "FORBIDDEN");
    }

    @Test
    @DisplayName("409 — InvalidQuizStateException")
    void handleInvalidQuizStateReturns409() {
        InvalidQuizStateException ex = new InvalidQuizStateException("Déjà publié");

        ResponseEntity<Map<String, String>> response = handler.handleInvalidQuizState(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.CONFLICT);
        assertThat(response.getBody()).containsEntry(KEY_ERROR, "INVALID_QUIZ_STATE");
    }

    @Test
    @DisplayName("503 — EmailSendException")
    void handleEmailSendReturns503() {
        EmailSendException ex = new EmailSendException("SMTP indisponible", new RuntimeException("cause"));

        ResponseEntity<Map<String, String>> response = handler.handleEmailSend(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.SERVICE_UNAVAILABLE);
        assertThat(response.getBody()).containsEntry(KEY_ERROR, "EMAIL_SEND_FAILED");
    }

    @Test
    @DisplayName("400 — QuizPublicationLimitExceededException")
    void handleQuizPublicationLimitReturns400() {
        QuizPublicationLimitExceededException ex =
                new QuizPublicationLimitExceededException(2000);

        ResponseEntity<Map<String, String>> response = handler.handleQuizPublicationLimit(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
        assertThat(response.getBody()).containsEntry(KEY_ERROR, "PUBLICATION_LIMIT_EXCEEDED");
    }

    @Test
    @DisplayName("500 — RuntimeException non gérée")
    void handleUnhandledRuntimeReturns500() {
        RuntimeException ex = new RuntimeException("bug inattendu");

        ResponseEntity<Map<String, String>> response = handler.handleUnhandledRuntime(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.INTERNAL_SERVER_ERROR);
        assertThat(response.getBody()).containsEntry(KEY_ERROR, "INTERNAL_ERROR");
        assertThat(response.getBody().get(KEY_MESSAGE)).contains("bug inattendu");
    }
}
