package com.smartest.backend.service;

import com.smartest.backend.dto.request.ChangePasswordRequest;
import com.smartest.backend.dto.request.UpdateEtudiantRequest;
import com.smartest.backend.dto.response.EtudiantResponse;
import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.exception.AccountNotFoundException;
import com.smartest.backend.exception.InvalidPasswordException;
import com.smartest.backend.exception.PasswordMismatchException;
import com.smartest.backend.repository.EtudiantRepository;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

@Service
public class EtudiantService {

    private final EtudiantRepository etudiantRepository;
    private final PasswordEncoder passwordEncoder;

    public EtudiantService(EtudiantRepository etudiantRepository,
                           PasswordEncoder passwordEncoder) {
        this.etudiantRepository = etudiantRepository;
        this.passwordEncoder = passwordEncoder;
    }

    // Récupère le profil de l'étudiant connecté
    public EtudiantResponse getProfile(String email) {
        Etudiant e = etudiantRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));
        return toResponse(e);
    }

    // Met à jour le nom de l'étudiant
    public EtudiantResponse updateProfile(String email, UpdateEtudiantRequest request) {
        Etudiant e = etudiantRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));
        if (request.getNom() != null && !request.getNom().isBlank()) {
            e.setNom(request.getNom());
        }
        etudiantRepository.save(e);
        return toResponse(e);
    }

    // Changement de mot de passe
    public void changePassword(String email, ChangePasswordRequest request) {
        if (!request.getNewPassword().equals(request.getConfirmPassword())) {
            throw new PasswordMismatchException();
        }
        Etudiant e = etudiantRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));
        if (!passwordEncoder.matches(request.getOldPassword(), e.getPassword())) {
            throw new InvalidPasswordException();
        }
        e.setPassword(passwordEncoder.encode(request.getNewPassword()));
        etudiantRepository.save(e);
    }

    // Supprime le compte de l'étudiant
    public void deleteAccount(String email) {
        Etudiant e = etudiantRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));
        etudiantRepository.delete(e);
    }

    private EtudiantResponse toResponse(Etudiant e) {
        return new EtudiantResponse(e.getId(), e.getNom(), e.getEmail(), e.isEmailVerifie());
    }
}