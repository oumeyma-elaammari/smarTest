package com.smartest.backend.repository;

import java.util.List;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.Cours;

@Repository
public interface CoursRepository extends JpaRepository<Cours, Long> {

    // Trouver les cours d'un professeur
    List<Cours> findByProfesseurId(Long professeurId);

    // Trouver les cours par titre (contient)
    List<Cours> findByTitreContainingIgnoreCase(String titre);

    // Requête personnalisée avec JOIN (CORRIGÉE)
    @Query("SELECT c FROM Cours c JOIN c.professeur p WHERE p.id = :profId")
    List<Cours> findCoursByProfesseurId(@Param("profId") Long profId);

    @Query("SELECT COUNT(c) FROM Cours c")
    Long countAllCours();

    // Trouver les cours avec leurs professeurs (fetch eager)
    @Query("SELECT DISTINCT c FROM Cours c LEFT JOIN FETCH c.professeur")
    List<Cours> findAllWithProfesseur();
}
