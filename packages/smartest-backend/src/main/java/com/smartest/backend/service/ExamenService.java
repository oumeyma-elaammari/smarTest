package com.smartest.backend.service;

import com.smartest.backend.dto.request.ExamenRequest;
import com.smartest.backend.dto.response.ExamenResponse;
import com.smartest.backend.dto.response.QuestionResponse;
import com.smartest.backend.dto.response.ReponseResponse;
import com.smartest.backend.entity.*;
import com.smartest.backend.repository.*;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class ExamenService {

    private final ExamenRepository examenRepository;
    private final ProfesseurRepository professeurRepository;
    private final CoursRepository coursRepository;
    private final QuestionRepository questionRepository;

    /**
     * Récupérer tous les examens
     */
    @Transactional(readOnly = true)
    public List<ExamenResponse> getAllExamens() {
        return examenRepository.findAll().stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Récupérer un examen par son ID
     */
    @Transactional(readOnly = true)
    public ExamenResponse getExamenById(Long id) {
        Examen examen = examenRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Examen non trouvé avec l'id: " + id));
        return convertToResponseDTO(examen);
    }

    /**
     * Récupérer les examens d'un professeur
     */
    @Transactional(readOnly = true)
    public List<ExamenResponse> getExamensByProfesseur(Long professeurId) {
        List<Examen> examens = examenRepository.findByProfesseurId(professeurId);
        return examens.stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Récupérer les examens d'un cours
     */
    @Transactional(readOnly = true)
    public List<ExamenResponse> getExamensByCours(Long coursId) {
        List<Examen> examens = examenRepository.findByCoursId(coursId);
        return examens.stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Créer un nouvel examen
     */
    @Transactional
    public ExamenResponse createExamen(ExamenRequest request) {
        Professeur professeur = professeurRepository.findById(request.getProfesseurId())
                .orElseThrow(() -> new RuntimeException("Professeur non trouvé avec l'id: " + request.getProfesseurId()));

        Examen examen = new Examen();
        examen.setTitre(request.getTitre());
        examen.setDuree(request.getDuree());
        examen.setProfesseur(professeur);

        if (request.getCoursId() != null) {
            Cours cours = coursRepository.findById(request.getCoursId())
                    .orElseThrow(() -> new RuntimeException("Cours non trouvé avec l'id: " + request.getCoursId()));
            examen.setCours(cours);
        }

        if (request.getQuestionsIds() != null && !request.getQuestionsIds().isEmpty()) {
            List<Question> questions = questionRepository.findAllById(request.getQuestionsIds());
            examen.setQuestions(questions);
        }

        Examen savedExamen = examenRepository.save(examen);
        return convertToResponseDTO(savedExamen);
    }

    /**
     * Mettre à jour un examen existant
     */
    @Transactional
    public ExamenResponse updateExamen(Long id, ExamenRequest request) {
        Examen examen = examenRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Examen non trouvé avec l'id: " + id));

        examen.setTitre(request.getTitre());
        examen.setDuree(request.getDuree());

        if (request.getProfesseurId() != null && !examen.getProfesseur().getId().equals(request.getProfesseurId())) {
            Professeur professeur = professeurRepository.findById(request.getProfesseurId())
                    .orElseThrow(() -> new RuntimeException("Professeur non trouvé avec l'id: " + request.getProfesseurId()));
            examen.setProfesseur(professeur);
        }

        if (request.getCoursId() != null) {
            Cours cours = coursRepository.findById(request.getCoursId())
                    .orElseThrow(() -> new RuntimeException("Cours non trouvé avec l'id: " + request.getCoursId()));
            examen.setCours(cours);
        } else {
            examen.setCours(null);
        }

        if (request.getQuestionsIds() != null) {
            List<Question> questions = questionRepository.findAllById(request.getQuestionsIds());
            examen.setQuestions(questions);
        }

        Examen updatedExamen = examenRepository.save(examen);
        return convertToResponseDTO(updatedExamen);
    }

    /**
     * Supprimer un examen
     */
    @Transactional
    public void deleteExamen(Long id) {
        if (!examenRepository.existsById(id)) {
            throw new RuntimeException("Examen non trouvé avec l'id: " + id);
        }
        examenRepository.deleteById(id);
    }

    /**
     * Ajouter une question à un examen
     */
    @Transactional
    public ExamenResponse addQuestionToExamen(Long examenId, Long questionId) {
        Examen examen = examenRepository.findById(examenId)
                .orElseThrow(() -> new RuntimeException("Examen non trouvé avec l'id: " + examenId));

        Question question = questionRepository.findById(questionId)
                .orElseThrow(() -> new RuntimeException("Question non trouvée avec l'id: " + questionId));

        if (!examen.getQuestions().contains(question)) {
            examen.getQuestions().add(question);
        }

        Examen updatedExamen = examenRepository.save(examen);
        return convertToResponseDTO(updatedExamen);
    }

    /**
     * Supprimer une question d'un examen
     */
    @Transactional
    public ExamenResponse removeQuestionFromExamen(Long examenId, Long questionId) {
        Examen examen = examenRepository.findById(examenId)
                .orElseThrow(() -> new RuntimeException("Examen non trouvé avec l'id: " + examenId));

        examen.getQuestions().removeIf(q -> q.getId().equals(questionId));

        Examen updatedExamen = examenRepository.save(examen);
        return convertToResponseDTO(updatedExamen);
    }

    /**
     * Vérifier si un examen existe
     */
    @Transactional(readOnly = true)
    public boolean existsById(Long id) {
        return examenRepository.existsById(id);
    }

    /**
     * Convertir une entité Examen en ExamenResponse
     */
    private ExamenResponse convertToResponseDTO(Examen examen) {
        ExamenResponse dto = new ExamenResponse();
        dto.setId(examen.getId());
        dto.setTitre(examen.getTitre());
        dto.setDuree(examen.getDuree());

        if (examen.getProfesseur() != null) {
            dto.setProfesseurId(examen.getProfesseur().getId());
            dto.setProfesseurNom(examen.getProfesseur().getNom());
        }

        if (examen.getCours() != null) {
            dto.setCoursId(examen.getCours().getId());
            dto.setCoursTitre(examen.getCours().getTitre());
        }

        if (examen.getQuestions() != null && !examen.getQuestions().isEmpty()) {
            List<QuestionResponse> questionDTOs = examen.getQuestions().stream()
                    .map(this::convertQuestionToResponse)
                    .collect(Collectors.toList());
            dto.setQuestions(questionDTOs);
        }

        return dto;
    }

    private QuestionResponse convertQuestionToResponse(Question question) {
        QuestionResponse dto = new QuestionResponse();
        dto.setId(question.getId());
        dto.setEnonce(question.getEnonce());
        dto.setType(question.getType() != null ? question.getType().name() : null);
        dto.setDifficulte(question.getDifficulte() != null ? question.getDifficulte().name() : null);
        if (question.getReponses() != null && !question.getReponses().isEmpty()) {
            List<ReponseResponse> reponseDTOs = question.getReponses().stream()
                    .map(this::convertReponseToResponse)
                    .collect(Collectors.toList());
            dto.setReponses(reponseDTOs);
        }

        return dto;
    }

    private ReponseResponse convertReponseToResponse(Reponse reponse) {
        ReponseResponse dto = new ReponseResponse();
        dto.setId(reponse.getId());
        dto.setContenu(reponse.getContenu());
        dto.setCorrecte(reponse.getCorrecte());
        return dto;
    }
}