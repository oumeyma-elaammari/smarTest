package com.smartest.backend.repository;

import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.Quiz;

@Repository
public interface QuizRepository extends JpaRepository<Quiz, Long> {

    // CORRIGÉ : utiliser "cours.id" au lieu de "coursId"
    List<Quiz> findByCoursId(Long coursId);

    // Alternative : findByCours_Id (même résultat)
    List<Quiz> findByCours_Id(Long coursId);

    // Trouver les quiz d'un professeur
    List<Quiz> findByProfesseurId(Long professeurId);

    // Trouver les quiz par titre
    List<Quiz> findByTitreContainingIgnoreCase(String titre);

    // Requête avec JOIN FETCH pour charger les questions
    @Query("SELECT DISTINCT q FROM Quiz q LEFT JOIN FETCH q.questions WHERE q.id = :id")
    Optional<Quiz> findByIdWithQuestions(@Param("id") Long id);

    // Compter les quiz par cours
    Long countByCoursId(Long coursId);

    // Supprimer les quiz d'un cours
    void deleteByCoursId(Long coursId);
}
