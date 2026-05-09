package com.smartest.backend.repository;

import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.enumeration.StatutExamen;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

public interface ExamenPublieRepository extends JpaRepository<ExamenPublie, Long> {

    @EntityGraph(attributePaths = {"professeur", "questions", "questions.reponses"})
    @Query("SELECT e FROM ExamenPublie e WHERE e.id = :id")
    Optional<ExamenPublie> findWithQuestionsAndProfesseurById(@Param("id") Long id);

    @Query("SELECT DISTINCT e FROM ExamenPublie e JOIN e.emailsAutorisesWeb em WHERE LOWER(em) = LOWER(:email) AND e.publieSurWebLe IS NOT NULL ORDER BY e.dateDebut DESC NULLS LAST, e.id DESC")
    List<ExamenPublie> findAutorisesPourEmail(@Param("email") String email);

    List<ExamenPublie> findByProfesseurId(Long professeurId);

    List<ExamenPublie> findByStatut(StatutExamen statut);

    List<ExamenPublie> findByStatutAndDateDebutBeforeAndDateFinAfter(
            StatutExamen statut,
            LocalDateTime now1,
            LocalDateTime now2
    );
}