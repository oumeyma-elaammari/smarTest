package com.smartest.backend.repository;

import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.Professeur;

@Repository
public interface ProfesseurRepository extends JpaRepository<Professeur, Long> {

    // Trouver un professeur par email
    Optional<Professeur> findByEmail(String email);

    // Trouver les professeurs par nom
    List<Professeur> findByNomContainingIgnoreCase(String nom);

    // Compter le nombre de cours par professeur
    @Query("SELECT p.id, p.nom, COUNT(c) FROM Professeur p LEFT JOIN p.cours c GROUP BY p.id")
    List<Object[]> countCoursByProfesseur();
}
