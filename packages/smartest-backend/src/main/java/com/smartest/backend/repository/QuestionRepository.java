package com.smartest.backend.repository;

import java.util.List;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.Question;

@Repository
public interface QuestionRepository extends JpaRepository<Question, Long> {

    // Trouver les questions par type (QCM, OUVERTE, VF)
    List<Question> findByType(String type);

    // Trouver les questions par difficulté
    List<Question> findByDifficulte(String difficulte);

    // Trouver les questions d'un professeur
    List<Question> findByProfesseurId(Long professeurId);

    // Trouver les questions d'un cours
    List<Question> findByCoursId(Long coursId);

    // Requête complexe : questions difficiles d'un cours
    @Query("SELECT q FROM Question q WHERE q.cours.id = :coursId AND q.difficulte = 'difficile'")
    List<Question> findQuestionsDifficilesByCours(@Param("coursId") Long coursId);

    // Compter les questions par type
    @Query("SELECT q.type, COUNT(q) FROM Question q GROUP BY q.type")
    List<Object[]> countQuestionsByType();
}
