package com.smartest.backend.repository;

import java.util.List;
import java.util.Optional;

import com.smartest.backend.entity.enumeration.Role;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.Utilisateur;

@Repository
public interface UtilisateurRepository extends JpaRepository<Utilisateur, Long> {

    // Find by email
    Optional<Utilisateur> findByEmail(String email);

    // Check if email exists
    boolean existsByEmail(String email);

    // Search by name
    List<Utilisateur> findByNomContainingIgnoreCase(String nom);

    // Find by role (enum)
    List<Utilisateur> findByRole(Role role);

    default List<Utilisateur> findAllEtudiants() {
        return findByRole(Role.ETUDIANT);
    }

    default List<Utilisateur> findAllProfesseurs() {
        return findByRole(Role.PROFESSEUR);
    }
}