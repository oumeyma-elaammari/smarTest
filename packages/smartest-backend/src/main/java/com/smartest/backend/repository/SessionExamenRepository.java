package com.smartest.backend.repository;

import java.time.LocalDateTime;
import java.util.List;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.SessionExamen;

@Repository
public interface SessionExamenRepository extends JpaRepository<SessionExamen, Long> {

    // Trouver les sessions d'un examen
    List<SessionExamen> findByExamenId(Long examenId);

    // Trouver les sessions par statut
    List<SessionExamen> findByStatut(String statut);

    // Trouver les sessions en cours
    @Query("SELECT s FROM SessionExamen s WHERE s.statut = 'en_cours'")
    List<SessionExamen> findSessionsEnCours();

    // Trouver les sessions à venir
    @Query("SELECT s FROM SessionExamen s WHERE s.dateDebut > :now")
    List<SessionExamen> findSessionsAFaire(@Param("now") LocalDateTime now);

    // Trouver les sessions terminées
    @Query("SELECT s FROM SessionExamen s WHERE s.dateFin < :now")
    List<SessionExamen> findSessionsTerminees(@Param("now") LocalDateTime now);
}
