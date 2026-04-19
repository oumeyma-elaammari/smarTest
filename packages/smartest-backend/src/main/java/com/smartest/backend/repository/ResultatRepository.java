package com.smartest.backend.repository;

import com.smartest.backend.entity.Resultat;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;
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

}