package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ForgotPasswordRequest;
import com.smartest.backend.dto.request.RegisterEtudiantRequest;
import com.smartest.backend.dto.request.ResetPasswordRequest;
import com.smartest.backend.dto.response.AuthResponse;
import com.smartest.backend.dto.request.LoginRequest;
import com.smartest.backend.dto.request.RegisterRequest;
import com.smartest.backend.exception.*;
import com.smartest.backend.service.AuthService;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/auth")
@CrossOrigin(origins = "*")
public class AuthController {

    private final AuthService authService;

    public AuthController(AuthService authService) {
        this.authService = authService;
    }

    @PostMapping("/register")
    public ResponseEntity<String> register(@Valid @RequestBody RegisterRequest request) {
        try {
            String message = authService.register(request);
            return ResponseEntity.status(HttpStatus.CREATED).body(message);
        } catch (PasswordMismatchException e) {
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(e.getMessage());
        } catch (EmailAlreadyUsedException e) {
            return ResponseEntity.status(HttpStatus.CONFLICT).body(e.getMessage());
        }
    }

    @PostMapping("/register/etudiant")
    public ResponseEntity<String> registerEtudiant(@Valid @RequestBody RegisterEtudiantRequest request) {
        try {
            String message = authService.registerEtudiant(request);
            return ResponseEntity.status(HttpStatus.CREATED).body(message);
        } catch (PasswordMismatchException e) {
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(e.getMessage());
        } catch (EmailAlreadyUsedException e) {
            return ResponseEntity.status(HttpStatus.CONFLICT).body(e.getMessage());
        }
    }

    @PostMapping("/login")
    public ResponseEntity<?> login(@Valid @RequestBody LoginRequest request) {
        try {
            AuthResponse response = authService.login(request);
            return ResponseEntity.status(HttpStatus.OK).body(response);
        } catch (InvalidPasswordException e) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body(e.getMessage());
        } catch (AccountNotFoundException e) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body(e.getMessage());
        }
    }

    // ══════════════════════════════════════════════
    //  Ancien système — lien web (conservé pour étudiants)
    // ══════════════════════════════════════════════
    @GetMapping("/verify-email")
    public ResponseEntity<Void> verifyEmail(
            @RequestParam String token,
            @RequestParam String role) {
        try {
            authService.verifyEmail(token, role);
            String redirectUrl = "http://localhost:5173/verify-email?status=success";
            return ResponseEntity.status(HttpStatus.FOUND)
                    .header("Location", redirectUrl)
                    .build();
        } catch (InvalidTokenException e) {
            String redirectUrl = "http://localhost:5173/verify-email?status=error";
            return ResponseEntity.status(HttpStatus.FOUND)
                    .header("Location", redirectUrl)
                    .build();
        }
    }

    // ══════════════════════════════════════════════
    //  Nouveau système — code 6 chiffres (desktop)
    // ══════════════════════════════════════════════
    @PostMapping("/verify-email/code")
    public ResponseEntity<String> verifyEmailByCode(
            @RequestParam String email,
            @RequestParam String code) {
        try {
            authService.verifyEmailByCode(email, code);
            return ResponseEntity.ok("Email vérifié avec succès.");
        } catch (InvalidTokenException e) {
            return ResponseEntity.status(HttpStatus.BAD_REQUEST)
                    .body("Code invalide ou expiré.");
        }
    }

    // ══════════════════════════════════════════════
    //  Renvoyer le code de vérification
    // ══════════════════════════════════════════════
    @PostMapping("/verify-email/resend")
    public ResponseEntity<String> resendVerificationCode(@RequestParam String email) {
        try {
            authService.resendVerificationCode(email);
            return ResponseEntity.ok("Code renvoyé avec succès.");
        } catch (InvalidTokenException e) {
            return ResponseEntity.status(HttpStatus.NOT_FOUND)
                    .body("Compte introuvable.");
        }
    }

    @PostMapping("/forgot-password/etudiant")
    public ResponseEntity<String> forgotPasswordEtudiant(@RequestBody ForgotPasswordRequest request) {
        authService.forgotPasswordEtudiant(request.getEmail());
        return ResponseEntity.ok("Si cet email existe, un lien de réinitialisation a été envoyé.");
    }

    @PostMapping("/reset-password/etudiant")
    public ResponseEntity<String> resetPasswordEtudiant(@RequestBody ResetPasswordRequest request) {
        authService.resetPasswordEtudiant(
                request.getToken(),
                request.getNewPassword(),
                request.getConfirmPassword()
        );
        return ResponseEntity.ok("Mot de passe réinitialisé avec succès.");
    }

    @PostMapping("/forgot-password/professeur")
    public ResponseEntity<String> forgotPasswordProfesseur(@RequestBody ForgotPasswordRequest request) {
        authService.forgotPasswordProfesseur(request.getEmail());
        return ResponseEntity.ok("Si cet email existe, un lien de réinitialisation a été envoyé.");
    }

    @PostMapping("/reset-password/professeur")
    public ResponseEntity<String> resetPasswordProfesseur(@RequestBody ResetPasswordRequest request) {
        authService.resetPasswordProfesseur(
                request.getToken(),
                request.getNewPassword(),
                request.getConfirmPassword()
        );
        return ResponseEntity.ok("Mot de passe réinitialisé avec succès.");
    }
}