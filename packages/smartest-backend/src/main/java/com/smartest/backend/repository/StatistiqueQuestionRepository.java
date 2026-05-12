package com.smartest.backend.repository;

import com.smartest.backend.entity.StatistiqueQuestion;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;
import java.util.Collection;
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

    @Modifying(clearAutomatically = true, flushAutomatically = true)
    @Query("DELETE FROM StatistiqueQuestion s WHERE s.quiz.id = :quizId")
    void deleteAllByQuizId(@Param("quizId") Long quizId);

    @Modifying(clearAutomatically = true, flushAutomatically = true)
    @Query("DELETE FROM StatistiqueQuestion s WHERE s.examenPublie.id = :examenId")
    void deleteAllByExamenPublieId(@Param("examenId") Long examenId);

    @Modifying(clearAutomatically = true, flushAutomatically = true)
    @Query("DELETE FROM StatistiqueQuestion s WHERE s.sessionExamen.id = :sessionId")
    void deleteAllBySessionExamenId(@Param("sessionId") Long sessionId);

    @Modifying(clearAutomatically = true, flushAutomatically = true)
    @Query("DELETE FROM StatistiqueQuestion s WHERE s.question.id IN :ids")
    void deleteAllByQuestionIdIn(@Param("ids") Collection<Long> ids);
}