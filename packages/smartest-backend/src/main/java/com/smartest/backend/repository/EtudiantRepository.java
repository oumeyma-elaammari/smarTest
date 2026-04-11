package com.smartest.backend.repository;

import com.smartest.backend.entity.Etudiant;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface EtudiantRepository extends JpaRepository<Etudiant, Long> {
    Optional<Etudiant> findByEmail(String email);
    boolean existsByEmail(String email);
    Optional<Etudiant> findByTokenVerification(String token);
    Optional<Etudiant> findByResetPasswordToken(String token);
}