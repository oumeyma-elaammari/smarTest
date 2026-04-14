package com.smartest.backend.repository;

import com.smartest.backend.entity.Reponse;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;
import java.util.List;

@Repository
public interface ReponseRepository extends JpaRepository<Reponse, Long> {

    // Trouver les réponses d'un étudiant
    List<Reponse> findByEtudiantId(Long etudiantId);

    // Trouver les réponses d'une session d'examen
    List<Reponse> findBySessionExamenId(Long sessionExamenId);

    // Trouver les réponses correctes d'un étudiant pour une session
    @Query("SELECT r FROM Reponse r WHERE r.etudiant.id = :etudiantId AND r.sessionExamen.id = :sessionId AND r.correcte = true")
    List<Reponse> findReponsesCorrectesByEtudiantAndSession(@Param("etudiantId") Long etudiantId, @Param("sessionId") Long sessionId);

    // Compter les réponses correctes par étudiant pour une session
    @Query("SELECT COUNT(r) FROM Reponse r WHERE r.etudiant.id = :etudiantId AND r.sessionExamen.id = :sessionId AND r.correcte = true")
    Long countReponsesCorrectes(@Param("etudiantId") Long etudiantId, @Param("sessionId") Long sessionId);

    // Trouver toutes les réponses d'une question pour une session
    List<Reponse> findByQuestionIdAndSessionExamenId(Long questionId, Long sessionExamenId);
}