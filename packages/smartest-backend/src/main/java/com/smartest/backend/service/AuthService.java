package com.smartest.backend.service;

import com.smartest.backend.dto.request.RegisterEtudiantRequest;
import com.smartest.backend.dto.response.AuthResponse;
import com.smartest.backend.dto.LoginRequest;
import com.smartest.backend.dto.request.RegisterRequest;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.enumeration.Role;
import com.smartest.backend.entity.Utilisateur;
import com.smartest.backend.exception.AccountNotFoundException;
import com.smartest.backend.exception.EmailAlreadyUsedException;
import com.smartest.backend.exception.InvalidPasswordException;
import com.smartest.backend.exception.PasswordMismatchException;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.UtilisateurRepository;
import com.smartest.backend.security.JwtUtil;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

@Service
public class AuthService {

    private final ProfesseurRepository professeurRepository;
    private final UtilisateurRepository utilisateurRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtUtil jwtUtil;

    public AuthService(ProfesseurRepository professeurRepository,
                       UtilisateurRepository utilisateurRepository,
                       PasswordEncoder passwordEncoder,
                       JwtUtil jwtUtil) {
        this.professeurRepository = professeurRepository;
        this.utilisateurRepository = utilisateurRepository;
        this.passwordEncoder = passwordEncoder;
        this.jwtUtil = jwtUtil;
    }

    // ══════════════════════════════════════════════════════════
    //  REGISTER — PROFESSOR only
    // ══════════════════════════════════════════════════════════
    public String register(RegisterRequest request) {

        if (!request.getPassword().equals(request.getConfirmPassword())) {
            throw new PasswordMismatchException();
        }

        if (professeurRepository.existsByEmail(request.getEmail())) {
            throw new EmailAlreadyUsedException(request.getEmail());
        }

        Professeur professor = new Professeur();
        professor.setNom(request.getNom());
        professor.setEmail(request.getEmail());
        professor.setPassword(passwordEncoder.encode(request.getPassword()));
        professor.setRole(Role.PROFESSEUR);

        professeurRepository.save(professor);
        return "Inscription réussie !";
    }



//  REGISTER — STUDENT only
    public String registerEtudiant(RegisterEtudiantRequest request) {

        // Vérifier passwords
        if (!request.getPassword().equals(request.getConfirmPassword())) {
            throw new PasswordMismatchException();
        }

        // Vérifier email unique dans les deux tables
        if (utilisateurRepository.existsByEmail(request.getEmail())) {
            throw new EmailAlreadyUsedException(request.getEmail());
        }
        if (professeurRepository.existsByEmail(request.getEmail())) {
            throw new EmailAlreadyUsedException(request.getEmail());
        }

        // Créer l'étudiant
        Utilisateur etudiant = new Utilisateur();
        etudiant.setNom(request.getNom());
        etudiant.setEmail(request.getEmail());
        etudiant.setPassword(passwordEncoder.encode(request.getPassword()));
        etudiant.setRole(Role.ETUDIANT);

        utilisateurRepository.save(etudiant);
        return "Student registration successful !";
    }
    // ══════════════════════════════════════════════════════════
    //  LOGIN — PROFESSOR or STUDENT
    // ══════════════════════════════════════════════════════════
    public AuthResponse login(LoginRequest request) {

        // ── CASE 1 : PROFESSOR ────────────────────────────────
        var professor = professeurRepository.findByEmail(request.getEmail());
        if (professor.isPresent()) {

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

        // ── CASE 2 : STUDENT ─────────────────────────────────
        var student = utilisateurRepository.findByEmail(request.getEmail());
        if (student.isPresent()) {

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

        // ── CASE 3 : NOT FOUND ────────────────────────────────
        throw new AccountNotFoundException(request.getEmail());
    }
}