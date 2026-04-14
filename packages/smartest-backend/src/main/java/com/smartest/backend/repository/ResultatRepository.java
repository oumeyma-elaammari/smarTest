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

    // Trouver les résultats d'un étudiant
    List<Resultat> findByEtudiantId(Long etudiantId);

    // Trouver les résultats d'une session d'examen
    List<Resultat> findBySessionExamenId(Long sessionExamenId);

    // Trouver le meilleur résultat d'un étudiant pour un examen
    @Query("SELECT r FROM Resultat r WHERE r.etudiant.id = :etudiantId AND r.sessionExamen.examen.id = :examenId ORDER BY r.note DESC")
    Optional<Resultat> findBestResultByEtudiantAndExamen(@Param("etudiantId") Long etudiantId, @Param("examenId") Long examenId);

    // Trouver le dernier résultat d'un étudiant pour un examen
   // @Query("SELECT r FROM Resultat r WHERE r.etudiant.id = :etudiantId AND r.sessionExamen.examen.id = :examenId ORDER BY r.dateObtention DESC")
   // Optional<Resultat> findLastResultByEtudiantAndExamen(@Param("etudiantId") Long etudiantId, @Param("examenId") Long examenId);

    // Moyenne des notes pour un examen
    @Query("SELECT COALESCE(AVG(r.note), 0) FROM Resultat r WHERE r.sessionExamen.examen.id = :examenId")
    Double findAverageNoteByExamen(@Param("examenId") Long examenId);

    // Statistiques par examen
    @Query("SELECT COALESCE(MAX(r.note), 0), COALESCE(MIN(r.note), 0), COALESCE(AVG(r.note), 0), COUNT(r) FROM Resultat r WHERE r.sessionExamen.examen.id = :examenId")
    Object[] findStatsByExamen(@Param("examenId") Long examenId);

    // Vérifier si un étudiant a déjà passé un examen
    @Query("SELECT CASE WHEN COUNT(r) > 0 THEN true ELSE false END FROM Resultat r WHERE r.etudiant.id = :etudiantId AND r.sessionExamen.examen.id = :examenId")
    boolean existsByEtudiantIdAndSessionExamenExamenId(@Param("etudiantId") Long etudiantId, @Param("examenId") Long examenId);

    // Supprimer tous les résultats d'un examen
    void deleteBySessionExamenExamenId(Long examenId);
}