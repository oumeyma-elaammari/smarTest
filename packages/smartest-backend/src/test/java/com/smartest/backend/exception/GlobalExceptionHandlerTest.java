package com.smartest.backend.exception;

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

import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("GlobalExceptionHandler — Tests complets")
class GlobalExceptionHandlerTest {

    @InjectMocks
    private GlobalExceptionHandler handler;

    @Test
    @DisplayName("✅ 409 — EmailAlreadyUsedException")
    void handleEmailAlreadyUsed_Returns409() {
        EmailAlreadyUsedException ex = new EmailAlreadyUsedException("test@test.com");

        ResponseEntity<String> response = handler.handleEmailAlreadyUsed(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.CONFLICT);
        assertThat(response.getBody()).isNotNull();
    }

    @Test
    @DisplayName("✅ 400 — PasswordMismatchException")
    void handlePasswordMismatch_Returns400() {
        PasswordMismatchException ex = new PasswordMismatchException();

        ResponseEntity<String> response = handler.handlePasswordMismatch(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
    }

    @Test
    @DisplayName("✅ 401 — InvalidPasswordException")
    void handleInvalidPassword_Returns401() {
        InvalidPasswordException ex = new InvalidPasswordException();

        ResponseEntity<String> response = handler.handleInvalidPassword(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.UNAUTHORIZED);
    }

    @Test
    @DisplayName("✅ 401 — AccountNotFoundException")
    void handleAccountNotFound_Returns401() {
        AccountNotFoundException ex = new AccountNotFoundException("inconnu@test.com");

        ResponseEntity<String> response = handler.handleAccountNotFound(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.UNAUTHORIZED);
    }

    @Test
    @DisplayName("✅ 403 — EmailNotVerifiedException")
    void handleEmailNotVerified_Returns403() {
        EmailNotVerifiedException ex = new EmailNotVerifiedException();

        ResponseEntity<String> response = handler.handleEmailNotVerified(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.FORBIDDEN);
    }

    @Test
    @DisplayName("✅ 400 — InvalidTokenException")
    void handleInvalidToken_Returns400() {
        InvalidTokenException ex = new InvalidTokenException();

        ResponseEntity<String> response = handler.handleInvalidToken(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
    }

    @Test
    @DisplayName("✅ 400 — MethodArgumentNotValidException")
    void handleValidationErrors_Returns400() {
        BindingResult bindingResult = mock(BindingResult.class);
        FieldError fieldError = new FieldError("req", "email", "L'email est obligatoire");
        when(bindingResult.getAllErrors()).thenReturn(List.of(fieldError));

        MethodArgumentNotValidException ex = mock(MethodArgumentNotValidException.class);
        when(ex.getBindingResult()).thenReturn(bindingResult);

        ResponseEntity<Map<String, String>> response =
                handler.handleValidationErrors(ex);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
        assertThat(response.getBody()).containsKey("email");
        assertThat(response.getBody().get("email")).isEqualTo("L'email est obligatoire");
    }
}