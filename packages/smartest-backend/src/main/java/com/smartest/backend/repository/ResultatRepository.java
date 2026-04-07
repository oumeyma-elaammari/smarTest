package com.smartest.backend.repository;

import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.Resultat;

@Repository
public interface ResultatRepository extends JpaRepository<Resultat, Long> {

    // Trouver les résultats d'un utilisateur
    List<Resultat> findByUtilisateurId(Long utilisateurId);

    // Trouver les résultats d'une session d'examen
    List<Resultat> findBySessionExamenId(Long sessionExamenId);

    // Trouver les résultats d'un examen spécifique
    @Query("SELECT r FROM Resultat r WHERE r.sessionExamen.examen.id = :examenId")
    List<Resultat> findByExamenId(@Param("examenId") Long examenId);

    // Trouver le meilleur résultat d'un utilisateur pour un examen
    @Query("SELECT r FROM Resultat r WHERE r.utilisateur.id = :userId AND r.sessionExamen.examen.id = :examenId ORDER BY r.note DESC")
    Optional<Resultat> findBestResultByUserAndExamen(@Param("userId") Long userId, @Param("examenId") Long examenId);

    // Moyenne des notes pour un examen
    @Query("SELECT COALESCE(AVG(r.note), 0) FROM Resultat r WHERE r.sessionExamen.examen.id = :examenId")
    Double findAverageNoteByExamen(@Param("examenId") Long examenId);

    // Statistiques par examen
    @Query("SELECT COALESCE(MAX(r.note), 0), COALESCE(MIN(r.note), 0), COALESCE(AVG(r.note), 0), COUNT(r) FROM Resultat r WHERE r.sessionExamen.examen.id = :examenId")
    Object[] findStatsByExamen(@Param("examenId") Long examenId);

    // Vérifier si un utilisateur a déjà passé un examen
    boolean existsByUtilisateurIdAndSessionExamenExamenId(Long utilisateurId, Long examenId);
}
