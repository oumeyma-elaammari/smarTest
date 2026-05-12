package com.smartest.backend.repository;

import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Question;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

/**
 * Deux requêtes séquentielles dans la même session : Hibernate interdit de JOIN FETCH plusieurs
 * collections de type bag ({@code List}) en une fois ({@code questions} + {@code reponses}).
 */
public class ExamenPublieRepositoryImpl implements ExamenPublieRepositoryCustom {

    @PersistenceContext
    private EntityManager entityManager;

    @Override
    @Transactional(readOnly = true)
    public Optional<ExamenPublie> findWithQuestionsAndProfesseurById(Long id) {
        /* Pas de DISTINCT : avec JOIN FETCH sur une collection, DISTINCT peut perturber
         * le remplissage du bag Hibernate ; une seule ligne racine suffit (premier résultat). */
        List<ExamenPublie> exams = entityManager.createQuery(
                        "SELECT e FROM ExamenPublie e "
                                + "LEFT JOIN FETCH e.professeur "
                                + "LEFT JOIN FETCH e.questions "
                                + "WHERE e.id = :id",
                        ExamenPublie.class)
                .setParameter("id", id)
                .getResultList();

        if (exams.isEmpty()) {
            return Optional.empty();
        }

        ExamenPublie exam = exams.get(0);
        List<Question> questions = exam.getQuestions() != null ? exam.getQuestions() : List.of();
        if (!questions.isEmpty()) {
            List<Long> ids = new ArrayList<>(questions.size());
            for (Question q : questions) {
                if (q != null && q.getId() != null) {
                    ids.add(q.getId());
                }
            }
            if (!ids.isEmpty()) {
                entityManager.createQuery(
                                "SELECT q FROM Question q LEFT JOIN FETCH q.reponses WHERE q.id IN :ids",
                                Question.class)
                        .setParameter("ids", ids)
                        .getResultList();
            }
        }

        return Optional.of(exam);
    }
}
