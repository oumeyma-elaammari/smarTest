package com.smartest.backend.service;

import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Cours;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.CoursRepository;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class QuestionService {

    @Autowired
    private QuestionRepository questionRepository;

    @Autowired
    private ProfesseurRepository professeurRepository;

    @Autowired
    private CoursRepository coursRepository;

    //  Créer une question
    public Question createQuestion(Question question, Long professeurId, Long coursId) {

        Professeur prof = professeurRepository.findById(professeurId)
                .orElseThrow(() -> new RuntimeException("Professeur non trouvé"));

        question.setProfesseur(prof);

        if (coursId != null) {
            Cours cours = coursRepository.findById(coursId)
                    .orElseThrow(() -> new RuntimeException("Cours non trouvé"));
            question.setCours(cours);
        }

        return questionRepository.save(question);
    }

    //  Récupérer toutes les questions
    public List<Question> getAll() {
        return questionRepository.findAll();
    }

    //  Par type
    public List<Question> getByType(TypeQuestion type) {
        return questionRepository.findByType(type);
    }

    //  Par difficulté
    public List<Question> getByDifficulte(Difficulte difficulte) {
        return questionRepository.findByDifficulte(difficulte);
    }

    // Par cours + difficulté
    public List<Question> getByCoursAndDifficulte(Long coursId, Difficulte niveau) {
        return questionRepository.findByCoursAndDifficulte(coursId, niveau);
    }


    //modifier
    public Question updateQuestion(Long id, Question updatedQuestion, Long coursId) {

        Question existingQuestion = questionRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Question non trouvée"));

        //  Mise à jour des champs simples
        existingQuestion.setEnonce(updatedQuestion.getEnonce());
        existingQuestion.setType(updatedQuestion.getType());
        existingQuestion.setDifficulte(updatedQuestion.getDifficulte());

        //  Mise à jour du cours (optionnel)
        if (coursId != null) {
            Cours cours = coursRepository.findById(coursId)
                    .orElseThrow(() -> new RuntimeException("Cours non trouvé"));
            existingQuestion.setCours(cours);
        }

        return questionRepository.save(existingQuestion);
    }

    //  Supprimer
    public void delete(Long id) {
        questionRepository.deleteById(id);
    }
}