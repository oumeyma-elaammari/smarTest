package com.smartest.backend.repository;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

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
    @Query("SELECT s FROM SessionExamen s WHERE s.statut = 'EN_COURS'")
    List<SessionExamen> findSessionsEnCours();

    // Trouver les sessions à venir
    @Query("SELECT s FROM SessionExamen s WHERE s.dateDebut > :now AND s.statut = 'PLANIFIE'")
    List<SessionExamen> findSessionsAFaire(@Param("now") LocalDateTime now);

    // Trouver les sessions terminées
    @Query("SELECT s FROM SessionExamen s WHERE s.dateFin < :now OR s.statut = 'TERMINE'")
    List<SessionExamen> findSessionsTerminees(@Param("now") LocalDateTime now);

    // Vérifier si une session est en cours
    @Query("SELECT CASE WHEN COUNT(s) > 0 THEN true ELSE false END FROM SessionExamen s WHERE s.examen.id = :examenId AND s.statut = 'EN_COURS'")
    boolean isExamenEnCours(@Param("examenId") Long examenId);

    // Trouver une session avec son examen chargé
    @Query("SELECT DISTINCT s FROM SessionExamen s LEFT JOIN FETCH s.examen WHERE s.id = :id")
    Optional<SessionExamen> findByIdWithExamen(@Param("id") Long id);
}
