package com.smartest.backend.repository;

import com.smartest.backend.entity.Resultat;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.Collection;
import java.util.List;
import java.util.Optional;

@Repository
public interface ResultatRepository extends JpaRepository<Resultat, Long> {

    List<Resultat> findByEtudiantId(Long etudiantId);

    List<Resultat> findBySessionExamenId(Long sessionId);

    List<Resultat> findByEtudiantIdAndSessionExamenIsNull(Long etudiantId);

    List<Resultat> findByEtudiantIdAndSessionExamenIsNotNull(Long etudiantId);

    boolean existsByEtudiantIdAndQuestionIdAndSessionExamenId(
            Long etudiantId, Long questionId, Long sessionId
    );

    boolean existsByEtudiantIdAndQuestionIdAndSessionExamenIsNull(
            Long etudiantId, Long questionId
    );

    List<Resultat> findByEtudiantIdAndSessionExamenId(Long etudiantId, Long sessionId);

    // vérifier si déjà fait quiz
    boolean existsByEtudiantIdAndQuizId(Long etudiantId, Long quizId);

    // récupérer résultats d’un quiz
    List<Resultat> findByQuizId(Long quizId);

    @Modifying(clearAutomatically = true, flushAutomatically = true)
    @Query("DELETE FROM Resultat r WHERE r.quizId = :quizId")
    void deleteByQuizId(@Param("quizId") Long quizId);

    @Modifying(clearAutomatically = true, flushAutomatically = true)
    @Query("DELETE FROM Resultat r WHERE r.sessionExamen.id = :sessionId")
    void deleteBySessionExamenId(@Param("sessionId") Long sessionId);

    @Modifying(clearAutomatically = true, flushAutomatically = true)
    @Query("DELETE FROM Resultat r WHERE r.question.id IN :ids")
    void deleteAllByQuestionIdIn(@Param("ids") Collection<Long> ids);

    /**
     * Résultats liés à un quiz ({@code quizId} renseigné) — pas les examens en session.
     * Ordre chronologique des réponses pour regroupement par tentative.
     */
    List<Resultat> findByEtudiant_IdAndQuizIdOrderByIdAsc(Long etudiantId, Long quizId);

    /**
     * Résultats de <strong>quiz</strong> uniquement : filtrés par {@code quizId} (les examens en session
     * n’enregistrent pas de {@code quizId} sur {@link com.smartest.backend.entity.Resultat}).
     */
    @Query("SELECT COUNT(DISTINCT r.etudiant.id) FROM Resultat r WHERE r.quizId = :quizId AND r.estPremiereTentative = true")
    long countEtudiantsDistinctsPremiereTentativePourQuiz(@Param("quizId") Long quizId);

    @Query("SELECT COUNT(r) FROM Resultat r WHERE r.quizId = :quizId AND r.question.id = :questionId AND r.estPremiereTentative = true")
    long countPremiereTentativePourQuestionPourQuiz(@Param("quizId") Long quizId, @Param("questionId") Long questionId);

    @Query("SELECT COUNT(r) FROM Resultat r WHERE r.quizId = :quizId AND r.question.id = :questionId AND r.estPremiereTentative = true AND r.correcte = true")
    long countPremiereTentativeCorrectesPourQuestionPourQuiz(@Param("quizId") Long quizId, @Param("questionId") Long questionId);

}