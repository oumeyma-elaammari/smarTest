package com.smartest.backend.service;

import com.smartest.backend.dto.LoginRequest;
import com.smartest.backend.dto.request.RegisterEtudiantRequest;
import com.smartest.backend.dto.request.RegisterRequest;
import com.smartest.backend.dto.response.AuthResponse;
import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.exception.*;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.security.JwtUtil;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.UUID;

@Service
public class AuthService {

    private final ProfesseurRepository professeurRepository;
    private final EtudiantRepository etudiantRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtUtil jwtUtil;

    public AuthService(
            ProfesseurRepository professeurRepository,
            EtudiantRepository etudiantRepository,
            PasswordEncoder passwordEncoder,
            JwtUtil jwtUtil,
            EmailService emailService) {
        this.professeurRepository = professeurRepository;
        this.etudiantRepository = etudiantRepository;
        this.passwordEncoder = passwordEncoder;
        this.jwtUtil = jwtUtil;
    }

    // ══════════════════════════════════════════════════════════
    //  REGISTER — PROFESSOR only
    // ══════════════════════════════════════════════════════════
    public String register(RegisterRequest request) {

        if (!request.getPassword().equals(request.getConfirmPassword()))
            throw new PasswordMismatchException();

        if (professeurRepository.existsByEmail(request.getEmail()))
            throw new EmailAlreadyUsedException(request.getEmail());

        Professeur professor = new Professeur();
        professor.setNom(request.getNom());
        professor.setEmail(request.getEmail());
        professor.setPassword(passwordEncoder.encode(request.getPassword()));
        professor.setRole(Role.PROFESSEUR);

        Professeur professeur = new Professeur();
        professeur.setNom(request.getNom());
        professeur.setEmail(request.getEmail());
        professeur.setPassword(passwordEncoder.encode(request.getPassword()));
        professeur.setEmailVerifie(false);
        professeur.setTokenVerification(token);

        professeurRepository.save(professeur);
        emailService.sendVerificationEmail(
                request.getEmail(), token, "PROFESSEUR");

        return "Inscription réussie ! Vérifiez votre email.";
    }

    // ══════════════════════════════════════════════════════
    //  REGISTER ETUDIANT
    // ══════════════════════════════════════════════════════
    public String registerEtudiant(RegisterEtudiantRequest request) {

        if (!request.getPassword().equals(request.getConfirmPassword()))
            throw new PasswordMismatchException();

        if (etudiantRepository.existsByEmail(request.getEmail()))
            throw new EmailAlreadyUsedException(request.getEmail());

        if (professeurRepository.existsByEmail(request.getEmail()))
            throw new EmailAlreadyUsedException(request.getEmail());

        String token = UUID.randomUUID().toString();

        Etudiant etudiant = new Etudiant();
        etudiant.setNom(request.getNom());
        etudiant.setEmail(request.getEmail());
        etudiant.setPassword(passwordEncoder.encode(request.getPassword()));
        etudiant.setEmailVerifie(false);
        etudiant.setTokenVerification(token);

        etudiantRepository.save(etudiant);
        emailService.sendVerificationEmail(
                request.getEmail(), token, "ETUDIANT");

        return "Inscription réussie ! Vérifiez votre email.";
    }

    // ══════════════════════════════════════════════════════
    //  VERIFY EMAIL
    // ══════════════════════════════════════════════════════
    public void verifyEmail(String token, String role) {

        if (role.equals("PROFESSEUR")) {
            var prof = professeurRepository.findByTokenVerification(token)
                    .orElseThrow(InvalidTokenException::new);
            prof.setEmailVerifie(true);
            prof.setTokenVerification(null);
            professeurRepository.save(prof);

        } else {
            var etudiant = etudiantRepository.findByTokenVerification(token)
                    .orElseThrow(InvalidTokenException::new);
            etudiant.setEmailVerifie(true);
            etudiant.setTokenVerification(null);
            etudiantRepository.save(etudiant);
        }
    }
    // ══════════════════════════════════════════════════════════
    //  LOGIN — PROFESSOR or STUDENT
    // ══════════════════════════════════════════════════════════
    public AuthResponse login(LoginRequest request) {

        // ── PROFESSEUR ────────────────────────────────────
        var prof = professeurRepository.findByEmail(request.getEmail());
        if (prof.isPresent()) {
            if (!prof.get().isEmailVerifie())
                throw new EmailNotVerifiedException();
            if (!passwordEncoder.matches(
                    request.getPassword(), prof.get().getPassword()))
                throw new InvalidPasswordException();

            String token = jwtUtil.generateToken(
                    prof.get().getEmail(), "PROFESSEUR");

            return new AuthResponse(
                    token, "PROFESSEUR",
                    prof.get().getNom(), prof.get().getEmail());
        }

        // ── ETUDIANT ──────────────────────────────────────
        var etudiant = etudiantRepository.findByEmail(request.getEmail());
        if (etudiant.isPresent()) {
            if (!etudiant.get().isEmailVerifie())
                throw new EmailNotVerifiedException();
            if (!passwordEncoder.matches(
                    request.getPassword(), etudiant.get().getPassword()))
                throw new InvalidPasswordException();

            String token = jwtUtil.generateToken(
                    etudiant.get().getEmail(), "ETUDIANT");

            return new AuthResponse(
                    token, "ETUDIANT",
                    etudiant.get().getNom(), etudiant.get().getEmail());
        }

        // ── CASE 3 : NOT FOUND ────────────────────────────────
        throw new AccountNotFoundException(request.getEmail());
    }

    // ══════════════════════════════════════════════════════
    //  FORGOT PASSWORD ETUDIANT
    // ══════════════════════════════════════════════════════
    public void forgotPasswordEtudiant(String email) {

        if (professeurRepository.existsByEmail(email)) return;

        var etudiant = etudiantRepository.findByEmail(email);
        if (etudiant.isPresent()) {
            String token = UUID.randomUUID().toString();
            LocalDateTime expiry = LocalDateTime.now().plusMinutes(15);
            etudiant.get().setResetPasswordToken(token);
            etudiant.get().setResetPasswordExpiry(expiry);
            etudiantRepository.save(etudiant.get());
            emailService.sendResetPasswordEmail(email, token);
        }
    }

    // ══════════════════════════════════════════════════════
    //  FORGOT PASSWORD PROFESSEUR
    // ══════════════════════════════════════════════════════
    public void forgotPasswordProfesseur(String email) {

        if (etudiantRepository.existsByEmail(email)) return;

        var prof = professeurRepository.findByEmail(email);
        if (prof.isPresent()) {
            String token = UUID.randomUUID().toString();
            LocalDateTime expiry = LocalDateTime.now().plusMinutes(15);
            prof.get().setResetPasswordToken(token);
            prof.get().setResetPasswordExpiry(expiry);
            professeurRepository.save(prof.get());
            emailService.sendResetPasswordEmail(email, token);
        }
    }

    // ══════════════════════════════════════════════════════
    //  RESET PASSWORD ETUDIANT
    // ══════════════════════════════════════════════════════
    public void resetPasswordEtudiant(
            String token, String newPassword, String confirmPassword) {

        if (!newPassword.equals(confirmPassword))
            throw new PasswordMismatchException();

        var etudiant = etudiantRepository.findByResetPasswordToken(token)
                .orElseThrow(InvalidTokenException::new);

        if (etudiant.getResetPasswordExpiry().isBefore(LocalDateTime.now()))
            throw new InvalidTokenException();

        etudiant.setPassword(passwordEncoder.encode(newPassword));
        etudiant.setResetPasswordToken(null);
        etudiant.setResetPasswordExpiry(null);
        etudiantRepository.save(etudiant);
    }

    // ══════════════════════════════════════════════════════
    //  RESET PASSWORD PROFESSEUR
    // ══════════════════════════════════════════════════════
    public void resetPasswordProfesseur(
            String token, String newPassword, String confirmPassword) {

        if (!newPassword.equals(confirmPassword))
            throw new PasswordMismatchException();

        var prof = professeurRepository.findByResetPasswordToken(token)
                .orElseThrow(InvalidTokenException::new);

        if (prof.getResetPasswordExpiry().isBefore(LocalDateTime.now()))
            throw new InvalidTokenException();

        prof.setPassword(passwordEncoder.encode(newPassword));
        prof.setResetPasswordToken(null);
        prof.setResetPasswordExpiry(null);
        professeurRepository.save(prof);
    }
}