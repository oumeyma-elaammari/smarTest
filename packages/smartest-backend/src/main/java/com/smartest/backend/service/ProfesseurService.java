package com.smartest.backend.service;

import com.smartest.backend.dto.request.ChangePasswordRequest;
import com.smartest.backend.dto.request.UpdateProfesseurRequest;
import com.smartest.backend.dto.response.ProfesseurResponse;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.exception.AccountNotFoundException;
import com.smartest.backend.exception.InvalidPasswordException;
import com.smartest.backend.exception.PasswordMismatchException;
import com.smartest.backend.repository.ProfesseurRepository;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

@Service
public class ProfesseurService {

    private final ProfesseurRepository professeurRepository;
    private final PasswordEncoder passwordEncoder;

    public ProfesseurService(ProfesseurRepository professeurRepository,
                             PasswordEncoder passwordEncoder) {
        this.professeurRepository = professeurRepository;
        this.passwordEncoder = passwordEncoder;
    }

    // Récupère le profil du professeur connecté
    public ProfesseurResponse getProfile(String email) {
        Professeur p = professeurRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));
        return toResponse(p);
    }

    // Met à jour le nom du professeur
    public ProfesseurResponse updateProfile(String email, UpdateProfesseurRequest request) {
        Professeur p = professeurRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));
        if (request.getNom() != null && !request.getNom().isBlank()) {
            p.setNom(request.getNom());
        }
        professeurRepository.save(p);
        return toResponse(p);
    }

    // Changement de mot de passe
    public void changePassword(String email, ChangePasswordRequest request) {
        if (!request.getNewPassword().equals(request.getConfirmPassword())) {
            throw new PasswordMismatchException();
        }
        Professeur p = professeurRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));
        if (!passwordEncoder.matches(request.getOldPassword(), p.getPassword())) {
            throw new InvalidPasswordException();
        }
        p.setPassword(passwordEncoder.encode(request.getNewPassword()));
        professeurRepository.save(p);
    }

    // Supprime le compte du professeur
    public void deleteAccount(String email) {
        Professeur p = professeurRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));
        professeurRepository.delete(p);
    }

    private ProfesseurResponse toResponse(Professeur p) {
        return new ProfesseurResponse(p.getId(), p.getNom(), p.getEmail(), p.isEmailVerifie());
    }
}