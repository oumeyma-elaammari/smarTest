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

    // ✅ Correct — examenPublie existe dans l'entité
    List<SessionExamen> findByExamenPublieId(Long examenPublieId);

    List<SessionExamen> findByStatut(String statut);

    @Query("SELECT s FROM SessionExamen s WHERE s.dateDebut > :now AND s.statut = 'PLANIFIE'")
    List<SessionExamen> findSessionsAFaire(@Param("now") LocalDateTime now);

    @Query("SELECT s FROM SessionExamen s WHERE s.dateFin < :now OR s.statut = 'TERMINE'")
    List<SessionExamen> findSessionsTerminees(@Param("now") LocalDateTime now);

    // ✅ Corrigé : s.examenPublie au lieu de s.examen
    @Query("SELECT DISTINCT s FROM SessionExamen s LEFT JOIN FETCH s.examenPublie WHERE s.id = :id")
    Optional<SessionExamen> findByIdWithExamen(@Param("id") Long id);
}