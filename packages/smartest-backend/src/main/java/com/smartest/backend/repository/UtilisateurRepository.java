package com.smartest.backend.repository;

import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.Utilisateur;

@Repository
public interface UtilisateurRepository extends JpaRepository<Utilisateur, Long> {

    // Trouver un utilisateur par email
    Optional<Utilisateur> findByEmail(String email);

    // Trouver les utilisateurs par rôle
    List<Utilisateur> findByRole(String role);

    // Vérifier si un email existe déjà
    boolean existsByEmail(String email);

    // Recherche par nom (contient)
    List<Utilisateur> findByNomContainingIgnoreCase(String nom);

    // Requête personnalisée : trouver les étudiants (role = 'etudiant')
    @Query("SELECT u FROM Utilisateur u WHERE u.role = 'etudiant'")
    List<Utilisateur> findAllEtudiants();

    // Requête personnalisée : trouver les professeurs
    @Query("SELECT u FROM Utilisateur u WHERE u.role = 'professeur'")
    List<Utilisateur> findAllProfesseurs();
}
