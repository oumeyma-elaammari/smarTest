package com.smartest.backend.repository;

import java.util.List;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.Reponse;

@Repository
public interface ReponseRepository extends JpaRepository<Reponse, Long> {

    // Trouver les réponses d'un utilisateur
    List<Reponse> findByUtilisateurId(Long utilisateurId);

    // Trouver les réponses d'une session d'examen
    List<Reponse> findBySessionExamenId(Long sessionExamenId);

    // Trouver les réponses correctes d'un utilisateur pour une session
    @Query("SELECT r FROM Reponse r WHERE r.utilisateur.id = :userId AND r.sessionExamen.id = :sessionId AND r.correcte = true")
    List<Reponse> findReponsesCorrectesByUserAndSession(@Param("userId") Long userId, @Param("sessionId") Long sessionId);

    // Compter les réponses correctes par utilisateur pour une session
    @Query("SELECT COUNT(r) FROM Reponse r WHERE r.utilisateur.id = :userId AND r.sessionExamen.id = :sessionId AND r.correcte = true")
    Long countReponsesCorrectes(@Param("userId") Long userId, @Param("sessionId") Long sessionId);
}
