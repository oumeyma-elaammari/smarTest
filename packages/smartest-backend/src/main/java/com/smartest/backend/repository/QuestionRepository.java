package com.smartest.backend.repository;

import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;

public interface QuestionRepository extends JpaRepository<Question, Long> {

    List<Question> findByType(TypeQuestion type);

    List<Question> findByDifficulte(Difficulte difficulte);

    List<Question> findByProfesseurId(Long professeurId);

    //List<Question> findByCoursId(Long coursId);

    //  version dynamique
    //@Query("SELECT q FROM Question q WHERE q.cours.id = :coursId AND q.difficulte = :niveau")
    //List<Question> findByCoursAndDifficulte(
            //@Param("coursId") Long coursId,
            //@Param("niveau") Difficulte niveau
    //);

    //  stats propre
    @Query("SELECT q.type as type, COUNT(q) as count FROM Question q GROUP BY q.type")
    List<QuestionStats> countQuestionsByType();

    //List<Question> findByCoursIdAndDifficulte(Long coursId, Difficulte difficulte);

    /** Nombre d'examens publiés contenant cette question (liaison {@code examen_publie_question}). */
    @Query("SELECT COUNT(DISTINCT e.id) FROM ExamenPublie e JOIN e.questions qq WHERE qq.id = :questionId")
    long countExamensWithQuestion(@Param("questionId") Long questionId);
}

