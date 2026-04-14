package com.smartest.backend.repository;

import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.Examen;

@Repository
public interface ExamenRepository extends JpaRepository<Examen, Long> {

    // CORRIGÉ : utiliser "cours.id" au lieu de "coursId"
    List<Examen> findByCoursId(Long coursId);

    // Trouver les examens d'un professeur
    List<Examen> findByProfesseurId(Long professeurId);

    // Trouver les examens par titre
    List<Examen> findByTitreContainingIgnoreCase(String titre);

    // Requête avec JOIN FETCH pour charger les questions et sessions
    @Query("SELECT DISTINCT e FROM Examen e LEFT JOIN FETCH e.questions LEFT JOIN FETCH e.sessions WHERE e.id = :id")
    Optional<Examen> findByIdWithDetails(@Param("id") Long id);

    // Compter les examens par cours
    Long countByCoursId(Long coursId);
}
