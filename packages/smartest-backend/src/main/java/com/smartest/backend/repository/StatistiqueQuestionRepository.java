package com.smartest.backend.repository;

import com.smartest.backend.entity.StatistiqueQuestion;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;
import java.util.List;
import java.util.Optional;

@Repository
public interface StatistiqueQuestionRepository extends JpaRepository<StatistiqueQuestion, Long> {

    Optional<StatistiqueQuestion> findByQuestionIdAndQuizId(Long questionId, Long quizId);

    List<StatistiqueQuestion> findByQuizId(Long quizId);

    @Query("SELECT s FROM StatistiqueQuestion s WHERE s.quiz.id = :quizId AND s.aGenereAlerte = true")
    List<StatistiqueQuestion> findAlertesByQuizId(@Param("quizId") Long quizId);

    @Query("SELECT s FROM StatistiqueQuestion s WHERE s.pourcentageEchec > 80")
    List<StatistiqueQuestion> findQuestionsAlerteEchec();
}