package com.smartest.backend.repository;

import com.smartest.backend.entity.Reponse;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;


public interface ReponseRepository extends JpaRepository<Reponse, Long> {

    //  Récupérer toutes les réponses d’une question
    List<Reponse> findByQuestionId(Long questionId);

    //  Récupérer uniquement les bonnes réponses d’une question
    List<Reponse> findByQuestionIdAndCorrecteTrue(Long questionId);

    //  Récupérer toutes les réponses correctes
    List<Reponse> findByCorrecteTrue();

    //  Vérifier si une question a au moins une bonne réponse
    boolean existsByQuestionIdAndCorrecteTrue(Long questionId);


    void deleteByQuestionId(Long questionId);

    @Modifying(clearAutomatically = true, flushAutomatically = true)
    @Query("DELETE FROM Reponse r WHERE r.sessionExamen.id = :sessionId")
    void deleteBySessionExamenId(@Param("sessionId") Long sessionId);

}