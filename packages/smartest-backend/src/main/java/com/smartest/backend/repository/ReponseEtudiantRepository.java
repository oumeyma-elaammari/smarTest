package com.smartest.backend.repository;

import com.smartest.backend.entity.ReponseEtudiant;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;
import java.util.List;
import java.util.Optional;

@Repository
public interface ReponseEtudiantRepository extends JpaRepository<ReponseEtudiant, Long> {

    // ==================== COMPTAGES ====================

    /**
     * Compter le nombre total de réponses d'un étudiant
     */
    long countByEtudiantId(Long etudiantId);

    /**
     * Compter le nombre de réponses correctes d'un étudiant
     */
    long countByEtudiantIdAndEstCorrecteTrue(Long etudiantId);

    /**
     * Compter le nombre de réponses incorrectes d'un étudiant
     */
    long countByEtudiantIdAndEstCorrecteFalse(Long etudiantId);

    /**
     * Compter le nombre de réponses correctes d'un étudiant pour une session d'examen
     */
    long countByEtudiantIdAndSessionExamenIdAndEstCorrecteTrue(Long etudiantId, Long sessionExamenId);

    /**
     * Compter le nombre de réponses pour une question
     */
    long countByQuestionId(Long questionId);

    /**
     * Compter le nombre de réponses correctes pour une question
     */
    long countByQuestionIdAndEstCorrecteTrue(Long questionId);

    /**
     * Compter le nombre de réponses incorrectes pour une question
     */
    long countByQuestionIdAndEstCorrecteFalse(Long questionId);

    /**
     * Compter le nombre de réponses pour une session d'examen
     */
    long countBySessionExamenId(Long sessionExamenId);

    /**
     * Compter le nombre de réponses pour un quiz (via sessionExamenId)
     */
    long countBySessionExamenIdAndEstCorrecteTrue(Long sessionExamenId);

    // ==================== RECHERCHES PAR ÉTUDIANT ====================

    /**
     * Trouver toutes les réponses d'un étudiant
     */
    List<ReponseEtudiant> findByEtudiantId(Long etudiantId);

    /**
     * Trouver toutes les réponses correctes d'un étudiant
     */
    List<ReponseEtudiant> findByEtudiantIdAndEstCorrecteTrue(Long etudiantId);

    /**
     * Trouver toutes les réponses incorrectes d'un étudiant
     */
    List<ReponseEtudiant> findByEtudiantIdAndEstCorrecteFalse(Long etudiantId);

    /**
     * Trouver les réponses d'un étudiant pour une session d'examen
     */
    List<ReponseEtudiant> findByEtudiantIdAndSessionExamenId(Long etudiantId, Long sessionExamenId);

    /**
     * Trouver les réponses correctes d'un étudiant pour une session d'examen
     */
    List<ReponseEtudiant> findByEtudiantIdAndSessionExamenIdAndEstCorrecteTrue(Long etudiantId, Long sessionExamenId);

    // ==================== RECHERCHES PAR SESSION ====================

    /**
     * Trouver toutes les réponses d'une session d'examen
     */
    List<ReponseEtudiant> findBySessionExamenId(Long sessionExamenId);

    /**
     * Trouver les réponses correctes d'une session d'examen
     */
    List<ReponseEtudiant> findBySessionExamenIdAndEstCorrecteTrue(Long sessionExamenId);

    /**
     * Trouver les réponses incorrectes d'une session d'examen
     */
    List<ReponseEtudiant> findBySessionExamenIdAndEstCorrecteFalse(Long sessionExamenId);

    // ==================== RECHERCHES PAR QUESTION ====================

    /**
     * Trouver toutes les réponses pour une question
     */
    List<ReponseEtudiant> findByQuestionId(Long questionId);

    /**
     * Trouver les réponses correctes pour une question
     */
    List<ReponseEtudiant> findByQuestionIdAndEstCorrecteTrue(Long questionId);

    /**
     * Trouver les réponses incorrectes pour une question
     */
    List<ReponseEtudiant> findByQuestionIdAndEstCorrecteFalse(Long questionId);

    /**
     * Trouver les réponses pour une question dans une session spécifique
     */
    List<ReponseEtudiant> findByQuestionIdAndSessionExamenId(Long questionId, Long sessionExamenId);

    // ==================== RECHERCHES PAR RÉSULTAT ====================

    /**
     * Trouver toutes les réponses associées à un résultat
     */
    List<ReponseEtudiant> findByResultatId(Long resultatId);

    /**
     * Trouver les réponses correctes pour un résultat
     */
    List<ReponseEtudiant> findByResultatIdAndEstCorrecteTrue(Long resultatId);

    // ==================== REQUÊTES PERSONNALISÉES ====================

    /**
     * Trouver la meilleure réponse d'un étudiant pour une question (pour analyse)
     */
    @Query("SELECT r FROM ReponseEtudiant r WHERE r.etudiant.id = :etudiantId AND r.question.id = :questionId ORDER BY r.dateSoumission DESC")
    Optional<ReponseEtudiant> findLastReponseByEtudiantAndQuestion(@Param("etudiantId") Long etudiantId, @Param("questionId") Long questionId);

    /**
     * Calculer le pourcentage de réussite pour une question
     */
    @Query("SELECT CASE WHEN COUNT(r) > 0 THEN (CAST(COUNT(CASE WHEN r.estCorrecte = true THEN 1 END) AS double) / COUNT(r)) * 100 ELSE 0 END FROM ReponseEtudiant r WHERE r.question.id = :questionId")
    Double calculateTauxReussiteByQuestion(@Param("questionId") Long questionId);

    /**
     * Calculer le pourcentage de réussite pour une question dans une session
     */
    @Query("SELECT CASE WHEN COUNT(r) > 0 THEN (CAST(COUNT(CASE WHEN r.estCorrecte = true THEN 1 END) AS double) / COUNT(r)) * 100 ELSE 0 END FROM ReponseEtudiant r WHERE r.question.id = :questionId AND r.sessionExamen.id = :sessionExamenId")
    Double calculateTauxReussiteByQuestionAndSession(@Param("questionId") Long questionId, @Param("sessionExamenId") Long sessionExamenId);

    /**
     * Trouver les questions qui ont un taux d'échec > 80% (pour alertes)
     */
    @Query("SELECT r.question.id FROM ReponseEtudiant r GROUP BY r.question.id HAVING (CAST(COUNT(CASE WHEN r.estCorrecte = false THEN 1 END) AS double) / COUNT(r)) * 100 > 80")
    List<Long> findQuestionsAlerteEchec();

    /**
     * Trouver les questions qui ont un taux d'échec > 80% pour une session spécifique
     */
    @Query("SELECT r.question.id FROM ReponseEtudiant r WHERE r.sessionExamen.id = :sessionExamenId GROUP BY r.question.id HAVING (CAST(COUNT(CASE WHEN r.estCorrecte = false THEN 1 END) AS double) / COUNT(r)) * 100 > 80")
    List<Long> findQuestionsAlerteEchecBySession(@Param("sessionExamenId") Long sessionExamenId);

    /**
     * Obtenir la distribution des réponses pour une question QCM
     */
    @Query("SELECT r.reponseTexte, COUNT(r) FROM ReponseEtudiant r WHERE r.question.id = :questionId GROUP BY r.reponseTexte")
    List<Object[]> getRepartitionReponsesByQuestion(@Param("questionId") Long questionId);

    // ==================== SUPPRESSIONS ====================

    /**
     * Supprimer toutes les réponses d'un étudiant
     */
    void deleteByEtudiantId(Long etudiantId);

    /**
     * Supprimer toutes les réponses d'une session d'examen
     */
    void deleteBySessionExamenId(Long sessionExamenId);

    /**
     * Supprimer toutes les réponses d'une question
     */
    void deleteByQuestionId(Long questionId);

    // ==================== EXISTENCE ====================

    /**
     * Vérifier si un étudiant a déjà répondu à une question dans une session
     */
    boolean existsByEtudiantIdAndQuestionIdAndSessionExamenId(Long etudiantId, Long questionId, Long sessionExamenId);

    /**
     * Vérifier si un étudiant a déjà répondu correctement à une question
     */
    boolean existsByEtudiantIdAndQuestionIdAndEstCorrecteTrue(Long etudiantId, Long questionId);


    Optional<ReponseEtudiant> findByEtudiantIdAndQuestionId(Long etudiantId, Long questionId);
}