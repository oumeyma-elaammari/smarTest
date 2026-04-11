package com.smartest.backend.repository;

import com.smartest.backend.entity.Professeur;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface ProfesseurRepository extends JpaRepository<Professeur, Long> {
    Optional<Professeur> findByEmail(String email);
    boolean existsByEmail(String email);
    Optional<Professeur> findByTokenVerification(String token);
    Optional<Professeur> findByResetPasswordToken(String token);
}