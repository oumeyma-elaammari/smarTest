package com.smartest.backend.controller;

import com.smartest.backend.dto.request.RegisterEtudiantRequest;
import com.smartest.backend.dto.response.AuthResponse;
import com.smartest.backend.dto.request.LoginRequest;
import com.smartest.backend.dto.request.RegisterRequest;
import com.smartest.backend.exception.AccountNotFoundException;
import com.smartest.backend.exception.EmailAlreadyUsedException;
import com.smartest.backend.exception.InvalidPasswordException;
import com.smartest.backend.exception.PasswordMismatchException;
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
}