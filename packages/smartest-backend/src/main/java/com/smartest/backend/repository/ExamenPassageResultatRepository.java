package com.smartest.backend.repository;

import com.smartest.backend.entity.ExamenPassageResultat;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface ExamenPassageResultatRepository extends JpaRepository<ExamenPassageResultat, Long> {

    @Query("SELECT p FROM ExamenPassageResultat p JOIN FETCH p.etudiant e "
            + "WHERE p.examenPublie.id = :examenId ORDER BY e.email ASC, e.id ASC")
    List<ExamenPassageResultat> findByExamenPublie_IdWithEtudiantOrderByEmail(@Param("examenId") Long examenId);

    Optional<ExamenPassageResultat> findByExamenPublie_IdAndEtudiant_Id(Long examenPublieId, Long etudiantId);

    List<ExamenPassageResultat> findByExamenPublie_IdAndValideeParProfIsFalse(Long examenPublieId);

    Optional<ExamenPassageResultat> findByExamenPublie_IdAndEtudiant_IdAndNoteVisibleEtudiantIsTrue(
            Long examenPublieId, Long etudiantId);
}
