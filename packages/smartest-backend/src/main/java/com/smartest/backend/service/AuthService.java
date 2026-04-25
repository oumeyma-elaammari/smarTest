package com.smartest.backend.service;

import com.smartest.backend.dto.request.LoginRequest;
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
import java.util.Random;
import java.util.UUID;

@Service
public class AuthService {

    private final ProfesseurRepository professeurRepository;
    private final EtudiantRepository etudiantRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtUtil jwtUtil;
    private final EmailService emailService;

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
        this.emailService = emailService;
    }

    // ══════════════════════════════════════════════
    //  HELPER — Générer un code à 6 chiffres
    // ══════════════════════════════════════════════
    private String generateCode() {
        return String.format("%06d", new Random().nextInt(999999));
    }

    // ══════════════════════════════════════════════
    //  REGISTER PROFESSEUR
    // ══════════════════════════════════════════════
    public String register(RegisterRequest request) {
        if (!request.getPassword().equals(request.getConfirmPassword()))
            throw new PasswordMismatchException();

        if (professeurRepository.existsByEmail(request.getEmail()))
            throw new EmailAlreadyUsedException(request.getEmail());
        if (etudiantRepository.existsByEmail(request.getEmail()))
            throw new EmailAlreadyUsedException(request.getEmail());


        String code = generateCode();

        Professeur professeur = new Professeur();
        professeur.setNom(request.getNom());
        professeur.setEmail(request.getEmail());
        professeur.setPassword(passwordEncoder.encode(request.getPassword()));
        professeur.setEmailVerifie(false);
        professeur.setTokenVerification(code);
        // Expiration 15 minutes
        professeur.setTokenVerificationExpiry(LocalDateTime.now().plusMinutes(15));

        professeurRepository.save(professeur);

        // Envoyer le code par email (pas un lien)
        emailService.sendVerificationCode(request.getEmail(), code);

        return "Inscription réussie ! Vérifiez votre email.";
    }

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
        emailService.sendVerificationEmail(request.getEmail(), token, "ETUDIANT");
        return "Inscription réussie ! Vérifiez votre email.";
    }

    // ══════════════════════════════════════════════
    //  VERIFY EMAIL — ancien système (lien web)
    //  conservé pour les étudiants
    // ══════════════════════════════════════════════
    public void verifyEmail(String token, String role) {
        if (role.equals("PROFESSEUR")) {
            var prof = professeurRepository.findByTokenVerification(token)
                    .orElseThrow(InvalidTokenException::new);
            prof.setEmailVerifie(true);
            prof.setTokenVerification(null);
            prof.setTokenVerificationExpiry(null);
            professeurRepository.save(prof);
        } else {
            var etudiant = etudiantRepository.findByTokenVerification(token)
                    .orElseThrow(InvalidTokenException::new);
            etudiant.setEmailVerifie(true);
            etudiant.setTokenVerification(null);
            etudiantRepository.save(etudiant);
        }
    }

    // ══════════════════════════════════════════════
    //  VERIFY EMAIL PAR CODE — nouveau système desktop
    // ══════════════════════════════════════════════
    public void verifyEmailByCode(String email, String code) {
        var prof = professeurRepository.findByEmail(email)
                .orElseThrow(InvalidTokenException::new);

        // Vérifier que le code correspond
        if (!code.equals(prof.getTokenVerification()))
            throw new InvalidTokenException();

        // Vérifier que le code n'est pas expiré
        if (prof.getTokenVerificationExpiry() == null ||
                prof.getTokenVerificationExpiry().isBefore(LocalDateTime.now()))
            throw new InvalidTokenException();

        prof.setEmailVerifie(true);
        prof.setTokenVerification(null);
        prof.setTokenVerificationExpiry(null);
        professeurRepository.save(prof);
    }

    // ══════════════════════════════════════════════
    //  RENVOYER LE CODE de vérification
    // ══════════════════════════════════════════════
    public void resendVerificationCode(String email) {
        var prof = professeurRepository.findByEmail(email)
                .orElseThrow(InvalidTokenException::new);

        if (prof.isEmailVerifie()) return;

        String code = generateCode();
        prof.setTokenVerification(code);
        prof.setTokenVerificationExpiry(LocalDateTime.now().plusMinutes(15));
        professeurRepository.save(prof);

        emailService.sendVerificationCode(email, code);
    }

    // ══════════════════════════════════════════════
    //  LOGIN
    // ══════════════════════════════════════════════
    public AuthResponse login(LoginRequest request) {
        var prof = professeurRepository.findByEmail(request.getEmail());
        if (prof.isPresent()) {
            if (!prof.get().isEmailVerifie())
                throw new EmailNotVerifiedException();
            if (!passwordEncoder.matches(request.getPassword(), prof.get().getPassword()))
                throw new InvalidPasswordException();

            String token = jwtUtil.generateToken(prof.get().getEmail(), "PROFESSEUR");
            return new AuthResponse(token, "PROFESSEUR",
                    prof.get().getNom(), prof.get().getEmail());
        }

        var etudiant = etudiantRepository.findByEmail(request.getEmail());
        if (etudiant.isPresent()) {
            if (!etudiant.get().isEmailVerifie())
                throw new EmailNotVerifiedException();
            if (!passwordEncoder.matches(request.getPassword(), etudiant.get().getPassword()))
                throw new InvalidPasswordException();

            String token = jwtUtil.generateToken(etudiant.get().getEmail(), "ETUDIANT");
            return new AuthResponse(token, "ETUDIANT",
                    etudiant.get().getNom(), etudiant.get().getEmail());
        }

        throw new AccountNotFoundException(request.getEmail());
    }

    // ══════════════════════════════════════════════
    //  FORGOT / RESET PASSWORD
    // ══════════════════════════════════════════════
    public void forgotPasswordEtudiant(String email) {
        if (professeurRepository.existsByEmail(email)) return;
        var etudiant = etudiantRepository.findByEmail(email);
        if (etudiant.isPresent()) {
            String token = UUID.randomUUID().toString();
            etudiant.get().setResetPasswordToken(token);
            etudiant.get().setResetPasswordExpiry(LocalDateTime.now().plusMinutes(15));
            etudiantRepository.save(etudiant.get());
            emailService.sendResetPasswordEmail(email, token, "ETUDIANT");
        }
    }

    public void forgotPasswordProfesseur(String email) {
        if (etudiantRepository.existsByEmail(email)) return;
        var prof = professeurRepository.findByEmail(email);
        if (prof.isPresent()) {
            String token = UUID.randomUUID().toString();
            prof.get().setResetPasswordToken(token);
            prof.get().setResetPasswordExpiry(LocalDateTime.now().plusMinutes(15));
            professeurRepository.save(prof.get());
            emailService.sendResetPasswordEmail(email, token, "PROFESSEUR");
        }
    }

    public void resetPasswordEtudiant(String token, String newPassword, String confirmPassword) {
        if (!newPassword.equals(confirmPassword)) throw new PasswordMismatchException();
        var etudiant = etudiantRepository.findByResetPasswordToken(token)
                .orElseThrow(InvalidTokenException::new);
        if (etudiant.getResetPasswordExpiry().isBefore(LocalDateTime.now()))
            throw new InvalidTokenException();
        etudiant.setPassword(passwordEncoder.encode(newPassword));
        etudiant.setResetPasswordToken(null);
        etudiant.setResetPasswordExpiry(null);
        etudiantRepository.save(etudiant);
    }

    public void resetPasswordProfesseur(String token, String newPassword, String confirmPassword) {
        if (!newPassword.equals(confirmPassword)) throw new PasswordMismatchException();
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