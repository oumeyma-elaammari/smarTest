package com.smartest.backend.repository;

import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.enumeration.StatutExamen;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

public interface ExamenPublieRepository extends JpaRepository<ExamenPublie, Long>, ExamenPublieRepositoryCustom {

    @Query("SELECT DISTINCT e FROM ExamenPublie e JOIN e.emailsAutorisesWeb em WHERE LOWER(em) = LOWER(:email) AND e.publieSurWebLe IS NOT NULL ORDER BY e.dateDebut DESC NULLS LAST, e.id DESC")
    List<ExamenPublie> findAutorisesPourEmail(@Param("email") String email);

    /**
     * Examens dont le prof a validé une publication web (liste d’emails non vide au moment de la publication).
     */
    @Query("SELECT DISTINCT e FROM ExamenPublie e JOIN FETCH e.professeur WHERE e.professeur.id = :professeurId AND e.publieSurWebLe IS NOT NULL ORDER BY e.dateDebut DESC NULLS LAST, e.id DESC")
    List<ExamenPublie> findPublieWebParProfesseur(@Param("professeurId") Long professeurId);

    List<ExamenPublie> findByProfesseurId(Long professeurId);

    List<ExamenPublie> findByStatut(StatutExamen statut);

    List<ExamenPublie> findByStatutAndDateDebutBeforeAndDateFinAfter(
            StatutExamen statut,
            LocalDateTime now1,
            LocalDateTime now2
    );

    /** Compte fiable sans charger la collection (open-in-view désactivé) ; table Flyway {@code examen_publie_question}. */
    @Query(value = "SELECT COUNT(*) FROM examen_publie_question WHERE examen_publie_id = :examId", nativeQuery = true)
    long countLinkedQuestions(@Param("examId") Long examId);

    /**
     * Charge {@link com.smartest.backend.entity.ExamenPublie#getEmailsAutorisesWeb()} avec la requête
     * (JOIN FETCH : plus fiable qu’{@code EntityGraph} sur {@code @ElementCollection} selon les versions Hibernate).
     */
    @Query("SELECT DISTINCT e FROM ExamenPublie e LEFT JOIN FETCH e.emailsAutorisesWeb WHERE e.id = :id")
    Optional<ExamenPublie> findByIdFetchingEmailsAutorisesWeb(@Param("id") Long id);
}