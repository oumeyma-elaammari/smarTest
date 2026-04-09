package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ForgotPasswordRequest;
import com.smartest.backend.dto.request.RegisterEtudiantRequest;
import com.smartest.backend.dto.request.ResetPasswordRequest;
import com.smartest.backend.dto.response.AuthResponse;
import com.smartest.backend.dto.LoginRequest;
import com.smartest.backend.dto.request.RegisterRequest;
import com.smartest.backend.exception.*;
import com.smartest.backend.service.AuthService;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/auth")
@CrossOrigin(origins = "*")//  autorise les appels depuis le frontend

public class AuthController {

    private final AuthService authService;
//for prof
    public AuthController(AuthService authService) {
        this.authService = authService;
    }
   @PostMapping("/register")
    public ResponseEntity<String> register(
            @Valid @RequestBody RegisterRequest request) {

        try {
            String message = authService.register(request);
            return ResponseEntity
                    .status(HttpStatus.CREATED)   // 201
                    .body(message);

        } catch (PasswordMismatchException e) {
            return ResponseEntity
                    .status(HttpStatus.BAD_REQUEST)   // 400
                    .body(e.getMessage());

        } catch (EmailAlreadyUsedException e) {
            return ResponseEntity
                    .status(HttpStatus.CONFLICT)   // 409
                    .body(e.getMessage());
        }
    }

    // POST /auth/register/etudiant
    @PostMapping("/register/etudiant")
    public ResponseEntity<String> registerEtudiant(
            @Valid @RequestBody RegisterEtudiantRequest request) {

        try {
            String message = authService.registerEtudiant(request);
            return ResponseEntity
                    .status(HttpStatus.CREATED)  // 201
                    .body(message);

        } catch (PasswordMismatchException e) {
            return ResponseEntity
                    .status(HttpStatus.BAD_REQUEST)  // 400
                    .body(e.getMessage());

        } catch (EmailAlreadyUsedException e) {
            return ResponseEntity
                    .status(HttpStatus.CONFLICT)  // 409
                    .body(e.getMessage());
        }
    }

    //  POST /auth/login
    //  Connexion professeur ou étudiant
    @PostMapping("/login")
    public ResponseEntity<?> login(
            @Valid @RequestBody LoginRequest request) {

        try {
            AuthResponse response = authService.login(request);
            return ResponseEntity
                    .status(HttpStatus.OK)   // 200
                    .body(response);

        } catch (InvalidPasswordException e) {
            return ResponseEntity
                    .status(HttpStatus.UNAUTHORIZED)   // 401
                    .body(e.getMessage());

        } catch (AccountNotFoundException e) {
            return ResponseEntity
                    .status(HttpStatus.UNAUTHORIZED)   // 401
                    .body(e.getMessage());
        }
    }

    // GET /auth/verify-email?token=xxx&role=PROFESSEUR
    @GetMapping("/verify-email")
    public ResponseEntity<Void> verifyEmail(
            @RequestParam String token,
            @RequestParam String role) {

        try {
            authService.verifyEmail(token, role);

            //  Redirection vers login avec message succès
            String redirectUrl = "http://localhost:5173/login?verified=true";
            return ResponseEntity
                    .status(HttpStatus.FOUND)
                    .header("Location", redirectUrl)
                    .build();

        } catch (InvalidTokenException e) {
            //  Redirection vers login avec message erreur
            String redirectUrl = "http://localhost:5173/login?verified=false";
            return ResponseEntity
                    .status(HttpStatus.FOUND)
                    .header("Location", redirectUrl)
                    .build();
        }
    }

    // ── Étudiant (Web) ─────────────────────────────────────
    @PostMapping("/forgot-password/etudiant")
    public ResponseEntity<String> forgotPasswordEtudiant(
            @RequestBody ForgotPasswordRequest request) {
        authService.forgotPasswordEtudiant(request.getEmail());
        return ResponseEntity.ok(
                "Si cet email existe, un lien de réinitialisation a été envoyé."
        );
    }

    @PostMapping("/reset-password/etudiant")
    public ResponseEntity<String> resetPasswordEtudiant(
            @RequestBody ResetPasswordRequest request) {
        authService.resetPasswordEtudiant(
                request.getToken(),
                request.getNewPassword(),
                request.getConfirmPassword()
        );
        return ResponseEntity.ok("Mot de passe réinitialisé avec succès.");
    }

    // ── Professeur (Desktop) ───────────────────────────────
    @PostMapping("/forgot-password/professeur")
    public ResponseEntity<String> forgotPasswordProfesseur(
            @RequestBody ForgotPasswordRequest request) {
        authService.forgotPasswordProfesseur(request.getEmail());
        return ResponseEntity.ok(
                "Si cet email existe, un lien de réinitialisation a été envoyé."
        );
    }

    @PostMapping("/reset-password/professeur")
    public ResponseEntity<String> resetPasswordProfesseur(
            @RequestBody ResetPasswordRequest request) {
        authService.resetPasswordProfesseur(
                request.getToken(),
                request.getNewPassword(),
                request.getConfirmPassword()
        );
        return ResponseEntity.ok("Mot de passe réinitialisé avec succès.");
    }
}