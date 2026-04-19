package com.smartest.backend.service;

import com.smartest.backend.dto.request.*;
import com.smartest.backend.dto.response.*;
import com.smartest.backend.entity.*;
import com.smartest.backend.entity.enumeration.StatutQuiz;
import com.smartest.backend.repository.*;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.List;

@Service
@RequiredArgsConstructor
public class QuizService {

    private final QuizRepository quizRepository;
    private final ProfesseurRepository professeurRepository;
    private final QuestionRepository questionRepository;

    private final ResultatRepository resultatRepository;
    private final ReponseRepository reponseRepository;
    private final EtudiantRepository etudiantRepository;

    // ================= GET =================

    public List<QuizResponse> getAllQuizs() {
        return quizRepository.findAll()
                .stream()
                .map(this::convertToDTO)
                .toList();
    }

    public QuizResponse getQuizById(Long id) {
        Quiz quiz = quizRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Quiz non trouvé"));
        return convertToDTO(quiz);
    }

    // ⚠️ supprimé findByProfesseurId (non existant)

    // ================= CREATE =================

    @Transactional
    public QuizResponse createQuiz(QuizRequest request) {

        Professeur professeur = professeurRepository.findById(request.getProfesseurId())
                .orElseThrow(() -> new RuntimeException("Professeur non trouvé"));

        Quiz quiz = new Quiz();
        quiz.setTitre(request.getTitre());
        quiz.setDuree(request.getDuree());
        quiz.setProfesseur(professeur);

        // statut par défaut
        quiz.setStatut(StatutQuiz.BROUILLON);

        return convertToDTO(quizRepository.save(quiz));
    }

    // ================= PUBLICATION =================

    public void publierQuiz(Long id) {

        Quiz quiz = quizRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Quiz introuvable"));

        quiz.setStatut(StatutQuiz.PUBLIE);
        quiz.setDatePublication(LocalDateTime.now());

        quizRepository.save(quiz);
    }

    public List<QuizResponse> getQuizPublies() {
        return quizRepository.findPublies()
                .stream()
                .map(this::convertToDTO)
                .toList();
    }

    // ================= LOGIC =================

    public boolean isPremiereTentative(Long quizId, Long etudiantId) {
        return !resultatRepository.existsByEtudiantIdAndQuizId(etudiantId, quizId);
    }

    @Transactional
    public ResultatQuizResponse soumettreQuiz(Long quizId, SoumissionQuizRequest request) {

        Etudiant etudiant = etudiantRepository.findById(request.getEtudiantId())
                .orElseThrow(() -> new RuntimeException("Etudiant introuvable"));

        boolean premiere = isPremiereTentative(quizId, etudiant.getId());

        int total = request.getReponses().size();
        int correct = 0;

        for (ReponseQuizDTO dto : request.getReponses()) {

            Reponse r = reponseRepository.findById(dto.getReponseId())
                    .orElseThrow(() -> new RuntimeException("Réponse introuvable"));

            if (Boolean.TRUE.equals(r.getCorrecte())) correct++;

            Resultat res = new Resultat();
            res.setEtudiant(etudiant);
            res.setQuestion(r.getQuestion());
            res.setReponse(r);
            res.setCorrecte(r.getCorrecte());
            res.setQuizId(quizId);
            res.setDatePassage(LocalDateTime.now());
            res.setEstPremiereTentative(premiere);

            resultatRepository.save(res);
        }

        double score = total == 0 ? 0.0 : ((double) correct / total) * 100;

        ResultatQuizResponse response = new ResultatQuizResponse();
        response.setScore(score);
        response.setBonnesReponses(correct);
        response.setTotalQuestions(total);
        response.setEstPremiereTentative(premiere);

        return response;
    }

    // ================= DTO =================

    private QuizResponse convertToDTO(Quiz quiz) {

        QuizResponse dto = new QuizResponse();

        dto.setId(quiz.getId());
        dto.setTitre(quiz.getTitre());
        dto.setDuree(quiz.getDuree());

        if (quiz.getProfesseur() != null) {
            dto.setProfesseurId(quiz.getProfesseur().getId());
            dto.setProfesseurNom(quiz.getProfesseur().getNom());
        }

        // ⚠️ IMPORTANT : PAS DE statut/datePublication car pas dans DTO

        return dto;
    }

    @Transactional
    public void deleteQuiz(Long quizId) {
        quizRepository.deleteById(quizId);
    }

    @Transactional
    public QuizResponse addQuestionToQuiz(Long quizId, Long questionId) {
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new RuntimeException("Quiz non trouvé"));
        Question question = questionRepository.findById(questionId)
                .orElseThrow(() -> new RuntimeException("Question non trouvée"));

        quiz.getQuestions().add(question);
        quizRepository.save(quiz);

        return convertToDTO(quiz);
    }

    @Transactional
    public void removeQuestionFromQuiz(Long quizId, Long questionId) {
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new RuntimeException("Quiz non trouvé"));
        Question question = questionRepository.findById(questionId)
                .orElseThrow(() -> new RuntimeException("Question non trouvée"));

        quiz.getQuestions().remove(question);
        quizRepository.save(quiz);
    }

}