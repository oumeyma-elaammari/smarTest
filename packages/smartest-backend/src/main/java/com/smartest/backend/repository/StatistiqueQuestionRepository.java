package com.smartest.backend.repository;

import com.smartest.backend.entity.StatistiqueQuestion;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;
import java.util.List;
import java.util.Optional;

@Repository
public interface StatistiqueQuestionRepository extends JpaRepository<StatistiqueQuestion, Long> {

    @Query("SELECT s FROM StatistiqueQuestion s WHERE s.question.id = :questionId AND s.quiz.id = :quizId")
    Optional<StatistiqueQuestion> findByQuestionIdAndQuizId(
            @Param("questionId") Long questionId,
            @Param("quizId") Long quizId);

    List<StatistiqueQuestion> findByQuizId(Long quizId);

    @Query("SELECT s FROM StatistiqueQuestion s WHERE s.quiz.id = :quizId AND s.aGenereAlerte = true")
    List<StatistiqueQuestion> findAlertesByQuizId(@Param("quizId") Long quizId);

    @Query("SELECT s FROM StatistiqueQuestion s WHERE s.pourcentageEchec > 70")
    List<StatistiqueQuestion> findQuestionsAlerteEchec();

    @Modifying
    @Query("DELETE FROM StatistiqueQuestion s WHERE s.quiz.id = :quizId")
    void deleteAllByQuizId(@Param("quizId") Long quizId);
}