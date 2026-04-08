package com.smartest.backend.service;

import com.smartest.backend.dto.request.RegisterEtudiantRequest;
import com.smartest.backend.dto.request.RegisterRequest;
import com.smartest.backend.dto.LoginRequest;
import com.smartest.backend.dto.response.AuthResponse;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Utilisateur;
import com.smartest.backend.entity.enumeration.Role;
import com.smartest.backend.exception.*;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.UtilisateurRepository;
import com.smartest.backend.security.JwtUtil;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import java.time.LocalDateTime;


import java.util.UUID;

@Service
public class AuthService {

    private final ProfesseurRepository professeurRepository;
    private final UtilisateurRepository utilisateurRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtUtil jwtUtil;
    private final EmailService emailService;

    public AuthService(
            ProfesseurRepository professeurRepository,
            UtilisateurRepository utilisateurRepository,
            PasswordEncoder passwordEncoder,
            JwtUtil jwtUtil,
            EmailService emailService) {
        this.professeurRepository = professeurRepository;
        this.utilisateurRepository = utilisateurRepository;
        this.passwordEncoder = passwordEncoder;
        this.jwtUtil = jwtUtil;
        this.emailService = emailService;
    }

    // ══════════════════════════════════════════════════════
    //  REGISTER PROFESSEUR
    // ══════════════════════════════════════════════════════
    public String register(RegisterRequest request) {

        if (!request.getPassword().equals(request.getConfirmPassword())) {
            throw new PasswordMismatchException();
        }

        if (professeurRepository.existsByEmail(request.getEmail())) {
            throw new EmailAlreadyUsedException(request.getEmail());
        }

        String token = UUID.randomUUID().toString();

        Professeur professeur = new Professeur();
        professeur.setNom(request.getNom());
        professeur.setEmail(request.getEmail());
        professeur.setPassword(passwordEncoder.encode(request.getPassword()));
        professeur.setRole(Role.PROFESSEUR);
        professeur.setEmailVerifie(false);
        professeur.setTokenVerification(token);

        professeurRepository.save(professeur);

        // Envoyer email de confirmation
        emailService.sendVerificationEmail(
                request.getEmail(),
                token,
                "PROFESSEUR"
        );

        return "Inscription réussie ! Vérifiez votre email pour confirmer votre compte.";
    }

    // ══════════════════════════════════════════════════════
    //  REGISTER ETUDIANT
    // ══════════════════════════════════════════════════════
    public String registerEtudiant(RegisterEtudiantRequest request) {

        if (!request.getPassword().equals(request.getConfirmPassword())) {
            throw new PasswordMismatchException();
        }

        if (utilisateurRepository.existsByEmail(request.getEmail())) {
            throw new EmailAlreadyUsedException(request.getEmail());
        }

        if (professeurRepository.existsByEmail(request.getEmail())) {
            throw new EmailAlreadyUsedException(request.getEmail());
        }

        String token = UUID.randomUUID().toString();

        Utilisateur etudiant = new Utilisateur();
        etudiant.setNom(request.getNom());
        etudiant.setEmail(request.getEmail());
        etudiant.setPassword(passwordEncoder.encode(request.getPassword()));
        etudiant.setRole(Role.ETUDIANT);
        etudiant.setEmailVerifie(false);
        etudiant.setTokenVerification(token);

        utilisateurRepository.save(etudiant);

        // Envoyer email de confirmation
        emailService.sendVerificationEmail(
                request.getEmail(),
                token,
                "ETUDIANT"
        );

        return "Inscription réussie ! Vérifiez votre email pour confirmer votre compte.";
    }

    // ══════════════════════════════════════════════════════
    //  VERIFY EMAIL
    // ══════════════════════════════════════════════════════
    public void verifyEmail(String token, String role) {

        if (role.equals("PROFESSEUR")) {
            var prof = professeurRepository.findByTokenVerification(token)
                    .orElseThrow(() -> new InvalidTokenException());

            prof.setEmailVerifie(true);
            prof.setTokenVerification(null);
            professeurRepository.save(prof);

        } else {
            var etudiant = utilisateurRepository.findByTokenVerification(token)
                    .orElseThrow(() -> new InvalidTokenException());

            etudiant.setEmailVerifie(true);
            etudiant.setTokenVerification(null);
            utilisateurRepository.save(etudiant);
        }
    }

    // ══════════════════════════════════════════════════════
    //  LOGIN
    // ══════════════════════════════════════════════════════
    public AuthResponse login(LoginRequest request) {

        // ── PROFESSEUR ────────────────────────────────────
        var professor = professeurRepository.findByEmail(request.getEmail());
        if (professor.isPresent()) {

            // Email non vérifié
            if (!professor.get().isEmailVerifie()) {
                throw new EmailNotVerifiedException();
            }

            if (!passwordEncoder.matches(request.getPassword(),
                    professor.get().getPassword())) {
                throw new InvalidPasswordException();
            }

            String token = jwtUtil.generateToken(
                    professor.get().getEmail(),
                    Role.PROFESSEUR.name()
            );

            return new AuthResponse(
                    token,
                    Role.PROFESSEUR.name(),
                    professor.get().getNom(),
                    professor.get().getEmail()
            );
        }

        // ── ETUDIANT ──────────────────────────────────────
        var student = utilisateurRepository.findByEmail(request.getEmail());
        if (student.isPresent()) {

            // Email non vérifié
            if (!student.get().isEmailVerifie()) {
                throw new EmailNotVerifiedException();
            }

            if (!passwordEncoder.matches(request.getPassword(),
                    student.get().getPassword())) {
                throw new InvalidPasswordException();
            }

            String token = jwtUtil.generateToken(
                    student.get().getEmail(),
                    Role.ETUDIANT.name()
            );

            return new AuthResponse(
                    token,
                    Role.ETUDIANT.name(),
                    student.get().getNom(),
                    student.get().getEmail()
            );
        }

        throw new AccountNotFoundException(request.getEmail());
    }
    // ══════════════════════════════════════════════════════
//  FORGOT PASSWORD — Étudiant uniquement (Web)
// ══════════════════════════════════════════════════════
    public void forgotPasswordEtudiant(String email) {

        // ❌ Si c'est un professeur → silencieux
        if (professeurRepository.existsByEmail(email)) {
            return;
        }

        var user = utilisateurRepository.findByEmail(email);
        if (user.isPresent()) {
            String token = UUID.randomUUID().toString();
            LocalDateTime expiry = LocalDateTime.now().plusMinutes(15);
            user.get().setResetPasswordToken(token);
            user.get().setResetPasswordExpiry(expiry);
            utilisateurRepository.save(user.get());
            emailService.sendResetPasswordEmail(email, token);
        }
        // ✅ Silencieux si email inconnu
    }

    // ══════════════════════════════════════════════════════
//  FORGOT PASSWORD — Professeur uniquement (Desktop)
// ══════════════════════════════════════════════════════
    public void forgotPasswordProfesseur(String email) {

        // ❌ Si c'est un étudiant → silencieux
        if (utilisateurRepository.existsByEmail(email)) {
            return;
        }

        var prof = professeurRepository.findByEmail(email);
        if (prof.isPresent()) {
            String token = UUID.randomUUID().toString();
            LocalDateTime expiry = LocalDateTime.now().plusMinutes(15);
            prof.get().setResetPasswordToken(token);
            prof.get().setResetPasswordExpiry(expiry);
            professeurRepository.save(prof.get());
            emailService.sendResetPasswordEmail(email, token);
        }
        // ✅ Silencieux si email inconnu
    }

    // ══════════════════════════════════════════════════════
//  RESET PASSWORD — Étudiant uniquement (Web)
// ══════════════════════════════════════════════════════
    public void resetPasswordEtudiant(String token, String newPassword, String confirmPassword) {

        if (!newPassword.equals(confirmPassword)) {
            throw new PasswordMismatchException();
        }

        var user = utilisateurRepository.findByResetPasswordToken(token)
                .orElseThrow(InvalidTokenException::new);

        if (user.getResetPasswordExpiry().isBefore(LocalDateTime.now())) {
            throw new InvalidTokenException();
        }

        user.setPassword(passwordEncoder.encode(newPassword));
        user.setResetPasswordToken(null);
        user.setResetPasswordExpiry(null);
        utilisateurRepository.save(user);
    }

    // ══════════════════════════════════════════════════════
//  RESET PASSWORD — Professeur uniquement (Desktop)
// ══════════════════════════════════════════════════════
    public void resetPasswordProfesseur(String token, String newPassword, String confirmPassword) {

        if (!newPassword.equals(confirmPassword)) {
            throw new PasswordMismatchException();
        }

        var prof = professeurRepository.findByResetPasswordToken(token)
                .orElseThrow(InvalidTokenException::new);

        if (prof.getResetPasswordExpiry().isBefore(LocalDateTime.now())) {
            throw new InvalidTokenException();
        }

        prof.setPassword(passwordEncoder.encode(newPassword));
        prof.setResetPasswordToken(null);
        prof.setResetPasswordExpiry(null);
        professeurRepository.save(prof);
    }


}