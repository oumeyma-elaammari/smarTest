package com.smartest.backend.service;

import com.smartest.backend.dto.request.ChangePasswordRequest;
import com.smartest.backend.dto.request.UpdateProfesseurRequest;
import com.smartest.backend.dto.response.QuizProfesseurListeResponse;
import com.smartest.backend.dto.response.ProfesseurResponse;
import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Quiz;
import com.smartest.backend.entity.SessionExamen;
import com.smartest.backend.exception.AccountNotFoundException;
import com.smartest.backend.exception.EmailAlreadyUsedException;
import com.smartest.backend.exception.InvalidPasswordException;
import com.smartest.backend.exception.PasswordMismatchException;
import com.smartest.backend.repository.ExamenPublieRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.QuizRepository;
import com.smartest.backend.repository.ReponseEtudiantRepository;
import com.smartest.backend.repository.ReponseRepository;
import com.smartest.backend.repository.ResultatRepository;
import com.smartest.backend.repository.SessionExamenRepository;
import com.smartest.backend.repository.StatistiqueQuestionRepository;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Locale;

@Service
public class ProfesseurService {

    private final ProfesseurRepository professeurRepository;
    private final PasswordEncoder passwordEncoder;
    private final QuizRepository quizRepository;
    private final ExamenPublieRepository examenPublieRepository;
    private final SessionExamenRepository sessionExamenRepository;
    private final StatistiqueQuestionRepository statistiqueQuestionRepository;
    private final ResultatRepository resultatRepository;
    private final ReponseEtudiantRepository reponseEtudiantRepository;
    private final ReponseRepository reponseRepository;
    private final QuestionRepository questionRepository;

    public ProfesseurService(
            ProfesseurRepository professeurRepository,
            PasswordEncoder passwordEncoder,
            QuizRepository quizRepository,
            ExamenPublieRepository examenPublieRepository,
            SessionExamenRepository sessionExamenRepository,
            StatistiqueQuestionRepository statistiqueQuestionRepository,
            ResultatRepository resultatRepository,
            ReponseEtudiantRepository reponseEtudiantRepository,
            ReponseRepository reponseRepository,
            QuestionRepository questionRepository) {
        this.professeurRepository = professeurRepository;
        this.passwordEncoder = passwordEncoder;
        this.quizRepository = quizRepository;
        this.examenPublieRepository = examenPublieRepository;
        this.sessionExamenRepository = sessionExamenRepository;
        this.statistiqueQuestionRepository = statistiqueQuestionRepository;
        this.resultatRepository = resultatRepository;
        this.reponseEtudiantRepository = reponseEtudiantRepository;
        this.reponseRepository = reponseRepository;
        this.questionRepository = questionRepository;
    }

    /**
     * Liste des quiz enregistrés côté serveur pour ce professeur (table {@code Quiz}),
     * triés par id descendant (récent / plus grand id en premier).
     */
    @Transactional(readOnly = true)
    public List<QuizProfesseurListeResponse> listMesQuiz(String email) {
        String emailNorm = email.trim().toLowerCase(Locale.ROOT);
        Professeur p = professeurRepository.findByEmail(emailNorm)
                .orElseThrow(() -> new AccountNotFoundException(email));
        return quizRepository.findByProfesseurId(p.getId()).stream()
                .sorted(Comparator.comparing(Quiz::getId).reversed())
                .map(quiz -> new QuizProfesseurListeResponse(
                        quiz.getId(),
                        quiz.getTitre() != null ? quiz.getTitre() : "",
                        quiz.getQuestions() != null ? quiz.getQuestions().size() : 0))
                .toList();
    }

    // Récupère le profil du professeur connecté
    public ProfesseurResponse getProfile(String email) {
        Professeur p = professeurRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));
        return toResponse(p);
    }

    // Met à jour le profil du professeur (nom + email)
    public ProfesseurResponse updateProfile(String email, UpdateProfesseurRequest request) {
        Professeur p = professeurRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));

        if (request.getNom() != null && !request.getNom().isBlank()) {
            p.setNom(request.getNom().trim());
        }

        if (request.getEmail() != null && !request.getEmail().isBlank()) {
            String newEmail = request.getEmail().trim().toLowerCase();
            String currentEmail = p.getEmail() == null ? "" : p.getEmail().trim().toLowerCase();
            if (!newEmail.equals(currentEmail) && professeurRepository.existsByEmail(newEmail)) {
                throw new EmailAlreadyUsedException(newEmail);
            }
            p.setEmail(newEmail);
        }

        professeurRepository.save(p);
        return toResponse(p);
    }

    // Changement de mot de passe
    public void changePassword(String email, ChangePasswordRequest request) {
        if (!request.getNewPassword().equals(request.getConfirmPassword())) {
            throw new PasswordMismatchException();
        }
        Professeur p = professeurRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email));
        if (!passwordEncoder.matches(request.getOldPassword(), p.getPassword())) {
            throw new InvalidPasswordException();
        }
        p.setPassword(passwordEncoder.encode(request.getNewPassword()));
        professeurRepository.save(p);
    }

    /**
     * Supprime définitivement le compte professeur et <strong>toutes</strong> les données associées
     * dans ce modèle : quiz (y compris emails web et liaisons quiz–question), examens publiés,
     * sessions d’examen, questions (propositions QCM), statistiques, résultats et réponses
     * côté étudiants. Les comptes étudiants ne sont pas supprimés (réponses / résultats liés si,
     * en revanche, le sont).
     */
    @Transactional
    public void deleteAccount(String email) {
        Long profId = professeurRepository.findByEmail(email)
                .orElseThrow(() -> new AccountNotFoundException(email))
                .getId();

        for (Quiz q : quizRepository.findByProfesseurId(profId)) {
            Long quizId = q.getId();
            statistiqueQuestionRepository.deleteAllByQuizId(quizId);
            reponseEtudiantRepository.deleteByResultatQuizId(quizId);
            resultatRepository.deleteByQuizId(quizId);
        }

        for (ExamenPublie ep : new ArrayList<>(examenPublieRepository.findByProfesseurId(profId))) {
            Long examenId = ep.getId();
            List<SessionExamen> sessions = sessionExamenRepository.findByExamenPublieId(examenId);
            for (SessionExamen se : sessions) {
                Long sessionId = se.getId();
                statistiqueQuestionRepository.deleteAllBySessionExamenId(sessionId);
                reponseEtudiantRepository.deleteBySessionExamenId(sessionId);
                resultatRepository.deleteBySessionExamenId(sessionId);
                reponseRepository.deleteBySessionExamenId(sessionId);
            }
            sessionExamenRepository.deleteAll(sessions);
            statistiqueQuestionRepository.deleteAllByExamenPublieId(examenId);
        }

        List<Long> questionIds = questionRepository.findByProfesseurId(profId).stream()
                .map(Question::getId)
                .toList();
        if (!questionIds.isEmpty()) {
            statistiqueQuestionRepository.deleteAllByQuestionIdIn(questionIds);
            for (Long qid : questionIds) {
                reponseEtudiantRepository.deleteByQuestionId(qid);
            }
            resultatRepository.deleteAllByQuestionIdIn(questionIds);
            for (Long qid : questionIds) {
                reponseRepository.deleteByQuestionId(qid);
            }
        }

        // Ordre : quiz puis examens (tables de jointure), puis questions, enfin le professeur.
        List<Quiz> quizzes = quizRepository.findByProfesseurId(profId);
        if (!quizzes.isEmpty()) {
            quizRepository.deleteAll(quizzes);
        }
        List<ExamenPublie> examens = examenPublieRepository.findByProfesseurId(profId);
        if (!examens.isEmpty()) {
            examenPublieRepository.deleteAll(examens);
        }
        List<Question> questions = questionRepository.findByProfesseurId(profId);
        if (!questions.isEmpty()) {
            questionRepository.deleteAll(questions);
        }

        professeurRepository.deleteById(profId);
    }

    private ProfesseurResponse toResponse(Professeur p) {
        return new ProfesseurResponse(p.getId(), p.getNom(), p.getEmail(), p.isEmailVerifie());
    }
}
