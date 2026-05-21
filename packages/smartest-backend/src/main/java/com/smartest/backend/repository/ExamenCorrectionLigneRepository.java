package com.smartest.backend.repository;

import com.smartest.backend.entity.ExamenCorrectionLigne;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface ExamenCorrectionLigneRepository extends JpaRepository<ExamenCorrectionLigne, Long> {

    @Query("SELECT DISTINCT l.etudiant.id FROM ExamenCorrectionLigne l "
            + "WHERE l.examenPublie.id = :examenPublieId AND l.etudiant.id IS NOT NULL")
    List<Long> findDistinctEtudiantIdsByExamenPublie_Id(@Param("examenPublieId") Long examenPublieId);

    List<ExamenCorrectionLigne> findByExamenPublie_IdAndEtudiant_IdOrderByQuestion_IdAsc(Long examenPublieId, Long etudiantId);

    Optional<ExamenCorrectionLigne> findByExamenPublie_IdAndEtudiant_IdAndQuestion_Id(
            Long examenPublieId, Long etudiantId, Long questionId);

    boolean existsByExamenPublie_IdAndEtudiant_Id(Long examenPublieId, Long etudiantId);

    void deleteByExamenPublie_IdAndEtudiant_Id(Long examenPublieId, Long etudiantId);

    @Query("SELECT l FROM ExamenCorrectionLigne l JOIN FETCH l.question q "
            + "WHERE l.examenPublie.id = :examenId ORDER BY q.id ASC, l.etudiant.id ASC")
    List<ExamenCorrectionLigne> findAllByExamenPublie_IdWithQuestion(@Param("examenId") Long examenId);

    @Query("SELECT l FROM ExamenCorrectionLigne l JOIN FETCH l.question JOIN FETCH l.etudiant "
            + "WHERE l.examenPublie.id = :examenId AND l.corrigeParIa = true")
    List<ExamenCorrectionLigne> findByExamenPublie_IdAndCorrigeParIaIsTrue(@Param("examenId") Long examenId);
}
