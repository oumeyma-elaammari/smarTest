package com.smartest.backend.repository;

import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.smartest.backend.entity.Quiz;
import com.smartest.backend.entity.enumeration.StatutQuiz;

@Repository
public interface QuizRepository extends JpaRepository<Quiz, Long> {

    /*// CORRIGÉ : utiliser "cours.id" au lieu de "coursId"
    List<Quiz> findByCoursId(Long coursId);

    // Alternative : findByCours_Id (même résultat)
    List<Quiz> findByCours_Id(Long coursId);

    // Trouver les quiz d'un professeur
    List<Quiz> findByProfesseurId(Long professeurId);

    // Trouver les quiz par titre
    List<Quiz> findByTitreContainingIgnoreCase(String titre);

    // Requête avec JOIN FETCH pour charger les questions
    @Query("SELECT DISTINCT q FROM Quiz q LEFT JOIN FETCH q.questions WHERE q.id = :id")
    Optional<Quiz> findByIdWithQuestions(@Param("id") Long id);

    // Compter les quiz par cours
    Long countByCoursId(Long coursId);

    // Supprimer les quiz d'un cours
    void deleteByCoursId(Long coursId);
*/

    List<Quiz> findByStatut(StatutQuiz statut);

    //  version simple — plus récemment publié en premier
    @Query("SELECT q FROM Quiz q WHERE q.statut = 'PUBLIE' ORDER BY q.datePublication DESC NULLS LAST, q.id DESC")
    List<Quiz> findPublies();

    List<Quiz> findByProfesseurId(Long professeurId);

    @Query("SELECT DISTINCT q FROM Quiz q JOIN q.emailsAutorisesWeb e WHERE q.statut = 'PUBLIE' AND LOWER(e) = LOWER(:email) ORDER BY q.datePublication DESC NULLS LAST, q.id DESC")
    List<Quiz> findPubliesAutorisesPourEmail(@Param("email") String email);

    @Query("SELECT DISTINCT q FROM Quiz q LEFT JOIN FETCH q.questions WHERE q.id = :id")
    Optional<Quiz> findByIdWithQuestions(@Param("id") Long id);

    /** Nombre de quiz contenant cette question (liaison {@code quiz_question}). */
    @Query("SELECT COUNT(DISTINCT q.id) FROM Quiz q JOIN q.questions qq WHERE qq.id = :questionId")
    long countQuizzesWithQuestion(@Param("questionId") Long questionId);

}
