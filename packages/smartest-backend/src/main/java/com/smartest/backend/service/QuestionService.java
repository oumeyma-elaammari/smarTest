package com.smartest.backend.service;

import com.smartest.backend.dto.request.QuestionRequest;
import com.smartest.backend.dto.request.ReponseRequest;
import com.smartest.backend.dto.response.QuestionResponse;
import com.smartest.backend.dto.response.ReponseResponse;
import com.smartest.backend.entity.Cours;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.repository.CoursRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.ReponseRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.data.jpa.repository.support.SimpleJpaRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Collections;
import java.util.List;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class QuestionService {
    private final QuestionRepository questionRepository;
    private final CoursRepository coursRepository;
    private final ReponseRepository reponseRepository;
    private final ProfesseurRepository professeurRepository;

    /**
     * Convertir une entité Question en QuestionResponseDTO
     */
    private QuestionResponse convertToResponseDTO(Question question) {
        if (question == null) {
            return null;
        }

        return QuestionResponse.builder()
                .id(question.getId())
                .enonce(question.getEnonce())
                .type(String.valueOf(question.getType()))
                .difficulte(String.valueOf(question.getDifficulte()))
                .explication(question.getExplication())
                .professeurId(getProfesseurId(question))
                .professeurNom(getProfesseurNom(question))
                .coursId(getCoursId(question))
                .coursTitre(getCoursTitre(question))
                .reponses(convertReponsesToDTO(question.getReponses()))  // ← Ici la conversion
                .build();
    }

    /**
     * Convertir une liste de réponses en liste de DTO
     */
    private List<ReponseResponse> convertReponsesToDTO(List<Reponse> reponses) {
        if (reponses == null || reponses.isEmpty()) {
            return Collections.emptyList();
        }
        return reponses.stream()
                .map(this::convertReponseToDTO)
                .collect(Collectors.toList());
    }

    /**
     * Convertir une entité Reponse en ReponseResponseDTO
     */
    private ReponseResponse convertReponseToDTO(Reponse reponse) {
        return ReponseResponse.builder()
                .id(reponse.getId())
                .contenu(reponse.getContenu())
                .correcte(reponse.getCorrecte())
                .build();
    }

    // ==================== Méthodes utilitaires ====================

    private Long getProfesseurId(Question question) {
        return question.getProfesseur() != null ? question.getProfesseur().getId() : null;
    }

    private String getProfesseurNom(Question question) {
        return question.getProfesseur() != null ? question.getProfesseur().getNom() : null;
    }

    private Long getCoursId(Question question) {
        return question.getCours() != null ? question.getCours().getId() : null;
    }

    private String getCoursTitre(Question question) {
        return question.getCours() != null ? question.getCours().getTitre() : null;
    }

    /**
     * Mettre à jour les réponses d'une question
     */
    private void updateReponses(Question question, List<ReponseRequest> reponsesRequest) {
        // Supprimer les anciennes réponses
        reponseRepository.deleteByQuestionId(question.getId());
        question.getReponses().clear();

        // Ajouter les nouvelles réponses
        for (ReponseRequest reponseRequest : reponsesRequest) {
            Reponse reponse = new Reponse();
            reponse.setContenu(reponseRequest.getContenu());
            reponse.setCorrecte(reponseRequest.getCorrecte());
            reponse.setQuestion(question);
            question.getReponses().add(reponse);
        }
    }

    @Transactional
    public Question createQuestion(Question question, Long professeurId, Long coursId) {
        // 1. Récupérer le professeur
        Professeur professeur = professeurRepository.findById(professeurId)
                .orElseThrow(() -> new RuntimeException("Professeur non trouvé avec l'id: " + professeurId));

        // 2. Récupérer le cours (optionnel)
        if (coursId != null) {
            Cours cours = coursRepository.findById(coursId)
                    .orElseThrow(() -> new RuntimeException("Cours non trouvé avec l'id: " + coursId));
            question.setCours(cours);
        }

        // 3. Associer le professeur
        question.setProfesseur(professeur);

        // 4. Sauvegarder
        return questionRepository.save(question);
    }




    @Transactional(readOnly = true)
    public List<Question> getAll() {
        return questionRepository.findAll();
    }

    /**
     * Récupérer les questions par type
     */
    @Transactional(readOnly = true)
    public List<QuestionResponse> getByType(TypeQuestion type) {
        List<Question> questions = questionRepository.findByType(type);
        return questions.stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Récupérer les questions par difficulté
     */
    @Transactional(readOnly = true)
    public List<QuestionResponse> getByDifficulte(Difficulte difficulte) {
        List<Question> questions = questionRepository.findByDifficulte(difficulte);
        return questions.stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Récupérer les questions par cours et difficulté
     */
    @Transactional(readOnly = true)
    public List<QuestionResponse> getByCoursAndDifficulte(Long coursId, Difficulte difficulte) {
        List<Question> questions = questionRepository.findByCoursIdAndDifficulte(coursId, difficulte);
        return questions.stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Mettre à jour une question (avec gestion du cours)
     */
    @Transactional
    public QuestionResponse updateQuestion(Long id, QuestionRequest request, Long coursId) {
        // 1. Vérifier que la question existe
        Question question = questionRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Question non trouvée avec l'id: " + id));

        // 2. Mettre à jour les champs
        question.setEnonce(request.getEnonce());

        if (request.getType() != null) {
            question.setType(TypeQuestion.valueOf(request.getType()));
        }

        if (request.getDifficulte() != null) {
            question.setDifficulte(Difficulte.valueOf(request.getDifficulte()));
        }

        question.setExplication(request.getExplication());

        // 3. Mettre à jour le cours si nécessaire
        if (coursId != null) {
            Cours cours = coursRepository.findById(coursId)
                    .orElseThrow(() -> new RuntimeException("Cours non trouvé avec l'id: " + coursId));
            question.setCours(cours);
        }

        // 4. Sauvegarder
        Question updatedQuestion = questionRepository.save(question);

        // 5. Retourner la réponse
        return convertToResponseDTO(updatedQuestion);
    }

    @Transactional
    public void delete(Long id) {
        // 1. Vérifier que la question existe
        Question question = questionRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Question non trouvée avec l'id: " + id));

        // 2. Supprimer la question (les réponses sont supprimées automatiquement grâce à cascade = CascadeType.ALL)
        questionRepository.delete(question);
    }
}