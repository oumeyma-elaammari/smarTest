package com.smartest.backend.service;

import com.smartest.backend.dto.request.QuizRequest;
import com.smartest.backend.dto.response.QuestionResponse;
import com.smartest.backend.dto.response.QuizResponse;
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
public class QuizService {

    private final QuizRepository quizRepository;
    private final ProfesseurRepository professeurRepository;
    private final CoursRepository coursRepository;
    private final QuestionRepository questionRepository;

    /**
     * Récupérer tous les quiz
     */
    @Transactional(readOnly = true)
    public List<QuizResponse> getAllQuizs() {
        return quizRepository.findAll().stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Récupérer un quiz par son ID
     */
    @Transactional(readOnly = true)
    public QuizResponse getQuizById(Long id) {
        Quiz quiz = quizRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Quiz non trouvé avec l'id: " + id));
        return convertToResponseDTO(quiz);
    }

    /**
     * Récupérer les quiz d'un professeur
     */
    @Transactional(readOnly = true)
    public List<QuizResponse> getQuizsByProfesseur(Long professeurId) {
        List<Quiz> quizs = quizRepository.findByProfesseurId(professeurId);
        return quizs.stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Récupérer les quiz d'un cours
     */
    @Transactional(readOnly = true)
    public List<QuizResponse> getQuizsByCours(Long coursId) {
        List<Quiz> quizs = quizRepository.findByCoursId(coursId);
        return quizs.stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Créer un nouveau quiz
     */
    @Transactional
    public QuizResponse createQuiz(QuizRequest request) {
        // Vérifier l'existence du professeur
        Professeur professeur = professeurRepository.findById(request.getProfesseurId())
                .orElseThrow(() -> new RuntimeException("Professeur non trouvé avec l'id: " + request.getProfesseurId()));

        // Créer le quiz
        Quiz quiz = new Quiz();
        quiz.setTitre(request.getTitre());
        quiz.setDuree(request.getDuree());
        quiz.setProfesseur(professeur);

        // Associer le cours si présent
        if (request.getCoursId() != null) {
            Cours cours = coursRepository.findById(request.getCoursId())
                    .orElseThrow(() -> new RuntimeException("Cours non trouvé avec l'id: " + request.getCoursId()));
            quiz.setCours(cours);
        }

        // Ajouter les questions si présentes
        if (request.getQuestionsIds() != null && !request.getQuestionsIds().isEmpty()) {
            List<Question> questions = questionRepository.findAllById(request.getQuestionsIds());
            quiz.setQuestions(questions);
        }

        Quiz savedQuiz = quizRepository.save(quiz);
        return convertToResponseDTO(savedQuiz);
    }

    /**
     * Mettre à jour un quiz existant
     */
    @Transactional
    public QuizResponse updateQuiz(Long id, QuizRequest request) {
        Quiz quiz = quizRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Quiz non trouvé avec l'id: " + id));

        // Mettre à jour les champs
        quiz.setTitre(request.getTitre());
        quiz.setDuree(request.getDuree());

        // Mettre à jour le professeur si nécessaire
        if (request.getProfesseurId() != null && !quiz.getProfesseur().getId().equals(request.getProfesseurId())) {
            Professeur professeur = professeurRepository.findById(request.getProfesseurId())
                    .orElseThrow(() -> new RuntimeException("Professeur non trouvé avec l'id: " + request.getProfesseurId()));
            quiz.setProfesseur(professeur);
        }

        // Mettre à jour le cours si nécessaire
        if (request.getCoursId() != null) {
            Cours cours = coursRepository.findById(request.getCoursId())
                    .orElseThrow(() -> new RuntimeException("Cours non trouvé avec l'id: " + request.getCoursId()));
            quiz.setCours(cours);
        } else {
            quiz.setCours(null);
        }

        // Mettre à jour les questions si nécessaire
        if (request.getQuestionsIds() != null) {
            List<Question> questions = questionRepository.findAllById(request.getQuestionsIds());
            quiz.setQuestions(questions);
        }

        Quiz updatedQuiz = quizRepository.save(quiz);
        return convertToResponseDTO(updatedQuiz);
    }

    /**
     * Supprimer un quiz
     */
    @Transactional
    public void deleteQuiz(Long id) {
        if (!quizRepository.existsById(id)) {
            throw new RuntimeException("Quiz non trouvé avec l'id: " + id);
        }
        quizRepository.deleteById(id);
    }

    /**
     * Ajouter une question à un quiz
     */
    @Transactional
    public QuizResponse addQuestionToQuiz(Long quizId, Long questionId) {
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new RuntimeException("Quiz non trouvé avec l'id: " + quizId));

        Question question = questionRepository.findById(questionId)
                .orElseThrow(() -> new RuntimeException("Question non trouvée avec l'id: " + questionId));

        // Vérifier que la question n'est pas déjà dans le quiz
        if (!quiz.getQuestions().contains(question)) {
            quiz.getQuestions().add(question);
        }

        Quiz updatedQuiz = quizRepository.save(quiz);
        return convertToResponseDTO(updatedQuiz);
    }

    /**
     * Supprimer une question d'un quiz
     */
    @Transactional
    public QuizResponse removeQuestionFromQuiz(Long quizId, Long questionId) {
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new RuntimeException("Quiz non trouvé avec l'id: " + quizId));

        quiz.getQuestions().removeIf(q -> q.getId().equals(questionId));

        Quiz updatedQuiz = quizRepository.save(quiz);
        return convertToResponseDTO(updatedQuiz);
    }

    /**
     * Compter le nombre de questions dans un quiz
     */
    @Transactional(readOnly = true)
    public Long countQuestionsByQuizId(Long quizId) {
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new RuntimeException("Quiz non trouvé avec l'id: " + quizId));
        return (long) quiz.getQuestions().size();
    }

    /**
     * Vérifier si un quiz existe
     */
    @Transactional(readOnly = true)
    public boolean existsById(Long id) {
        return quizRepository.existsById(id);
    }

    /**
     * Convertir une entité Quiz en QuizResponseDTO
     */
    private QuizResponse convertToResponseDTO(Quiz quiz) {
        QuizResponse dto = new QuizResponse();
        dto.setId(quiz.getId());
        dto.setTitre(quiz.getTitre());
        dto.setDuree(quiz.getDuree());

        // Informations du professeur
        if (quiz.getProfesseur() != null) {
            dto.setProfesseurId(quiz.getProfesseur().getId());
            dto.setProfesseurNom(quiz.getProfesseur().getNom());
        }

        // Informations du cours
        if (quiz.getCours() != null) {
            dto.setCoursId(quiz.getCours().getId());
            dto.setCoursTitre(quiz.getCours().getTitre());
        }

        // Convertir les questions
        if (quiz.getQuestions() != null && !quiz.getQuestions().isEmpty()) {
            List<QuestionResponse> questionDTOs = quiz.getQuestions().stream()
                    .map(this::convertQuestionToResponseDTO)
                    .collect(Collectors.toList());
            dto.setQuestions(questionDTOs);
        }

        return dto;
    }

    /**
     * Convertir une entité Question en QuestionResponseDTO
     */
    private QuestionResponse convertQuestionToResponseDTO(Question question) {
        QuestionResponse dto = new QuestionResponse();
        dto.setId(question.getId());
        dto.setEnonce(question.getEnonce());

        // Vérifier si type n'est pas null avant d'appeler name()
        if (question.getType() != null) {
            dto.setType(question.getType());
        }

        // Vérifier si difficulte n'est pas null avant d'appeler name()
        if (question.getDifficulte() != null) {
            dto.setDifficulte(question.getDifficulte());
        }

        // Convertir les réponses
        if (question.getReponses() != null && !question.getReponses().isEmpty()) {
            List<ReponseResponse> reponseDTOs = question.getReponses().stream()
                    .map(this::convertReponseToResponseDTO)
                    .collect(Collectors.toList());
            dto.setReponses(reponseDTOs);
        }

        return dto;
    }

    /**
     * Convertir une entité Reponse en ReponseResponseDTO
     */
    private ReponseResponse convertReponseToResponseDTO(Reponse reponse) {
        ReponseResponse dto = new ReponseResponse();
        dto.setId(reponse.getId());
        dto.setContenu(reponse.getContenu());
        dto.setCorrecte(reponse.getCorrecte());
        return dto;
    }
}