package com.smartest.backend.service;

import com.smartest.backend.constants.QuizPublicationLimits;
import com.smartest.backend.dto.request.*;
import com.smartest.backend.dto.response.*;
import com.smartest.backend.entity.*;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.StatutQuiz;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.repository.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.transaction.support.TransactionSynchronization;
import org.springframework.transaction.support.TransactionSynchronizationManager;
import org.springframework.web.server.ResponseStatusException;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Objects;
import java.util.Set;
import java.util.regex.Pattern;

@Slf4j
@Service
@RequiredArgsConstructor
public class QuizService {

    private static final Pattern EMAIL_SIMPLE =
            Pattern.compile("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", Pattern.CASE_INSENSITIVE);

    private final QuizRepository quizRepository;
    private final ProfesseurRepository professeurRepository;
    private final QuestionRepository questionRepository;

    private final ResultatRepository resultatRepository;
    private final ReponseRepository reponseRepository;
    private final EtudiantRepository etudiantRepository;
    private final StatistiqueQuestionRepository statistiqueQuestionRepository;
    private final ReponseEtudiantRepository reponseEtudiantRepository;

    private final EmailService emailService;

    private final StatistiqueRecalculService statistiqueRecalculService;

    // ================= GET =================

    /**
     * Lecture + mapping DTO : {@code open-in-view=false} exige une transaction pour accéder
     * aux collections lazy (ex. {@code questions} pour le comptage).
     */
    @Transactional(readOnly = true)
    public List<QuizResponse> getAllQuizs() {
        return quizRepository.findAll()
                .stream()
                .map(this::convertToDTO)
                .toList();
    }

    @Transactional(readOnly = true)
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

    @Transactional(readOnly = true)
    public List<QuizResponse> getQuizPublies() {
        return quizRepository.findPublies()
                .stream()
                .map(this::convertToDTO)
                .toList();
    }

    /**
     * Quiz publiés sur le web dont l'email de l'étudiant figure dans la liste autorisée.
     */
    @Transactional(readOnly = true)
    public List<QuizResponse> getMesQuizsPublicationWeb(String emailEtudiant) {
        if (emailEtudiant == null || emailEtudiant.isBlank()) {
            return List.of();
        }
        String email = emailEtudiant.trim().toLowerCase(Locale.ROOT);
        Etudiant etudiant = etudiantRepository.findByEmail(email).orElse(null);
        if (etudiant == null) {
            return List.of();
        }
        Long etudiantId = etudiant.getId();
        return quizRepository.findPubliesAutorisesPourEmail(email)
                .stream()
                .map(quiz -> convertToDTOAvecScoreEtudiant(quiz, etudiantId))
                .toList();
    }

    private QuizResponse convertToDTOAvecScoreEtudiant(Quiz quiz, Long etudiantId) {
        QuizResponse dto = convertToDTO(quiz);
        Long quizId = quiz.getId();
        boolean premiere = !resultatRepository.existsByEtudiantIdAndQuizId(etudiantId, quizId);
        dto.setPremiereTentative(premiere);
        int n = dto.getNombreQuestions() != null ? dto.getNombreQuestions() : 0;
        if (!premiere && n > 0) {
            dto.setMeilleurScore(calculerMeilleurScoreQuiz(etudiantId, quizId, n));
        }
        return dto;
    }

    /**
     * Meilleur score parmi les tentatives, à partir des {@link com.smartest.backend.entity.Resultat}
     * avec {@code quizId} (parcours quiz uniquement, pas les examens).
     */
    private Double calculerMeilleurScoreQuiz(Long etudiantId, Long quizId, int nombreQuestions) {
        List<Resultat> rows = resultatRepository.findByEtudiant_IdAndQuizIdOrderByIdAsc(etudiantId, quizId);
        if (rows.isEmpty()) {
            return null;
        }
        double best = 0.0;
        boolean anyChunk = false;
        for (int i = 0; i + nombreQuestions <= rows.size(); i += nombreQuestions) {
            List<Resultat> chunk = rows.subList(i, i + nombreQuestions);
            long bonnes = chunk.stream().filter(r -> Boolean.TRUE.equals(r.getCorrecte())).count();
            double pct = (double) bonnes / nombreQuestions * 100.0;
            if (!anyChunk || pct > best) {
                best = pct;
                anyChunk = true;
            }
        }
        return anyChunk ? best : null;
    }

    @Transactional(readOnly = true)
    public QuizPassageWebResponse getQuizPourPassageWeb(Long quizId, String emailEtudiant) {
        Quiz quiz = chargerQuizPublieAutorise(quizId, emailEtudiant);

        QuizPassageWebResponse dto = new QuizPassageWebResponse();
        dto.setId(quiz.getId());
        dto.setTitre(quiz.getTitre());
        dto.setDuree(quiz.getDuree());
        dto.setNombreQuestions(quiz.getQuestions().size());
        dto.setQuestions(quiz.getQuestions().stream().map(this::toQuestionPassageWebDto).toList());
        return dto;
    }

    /**
     * Vérifie la réponse choisie sur une question (ne persiste rien : la soumission reste {@code soumettre-web}).
     */
    @Transactional(readOnly = true)
    public VerificationQuestionWebResponse verifierQuestionPassageWeb(
            Long quizId,
            String emailEtudiant,
            VerificationQuestionWebRequest request) {
        if (request == null || request.getQuestionId() == null || request.getReponseId() == null) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Requête incomplète");
        }
        Quiz quiz = chargerQuizPublieAutorise(quizId, emailEtudiant);
        Question question = quiz.getQuestions().stream()
                .filter(q -> q.getId().equals(request.getQuestionId()))
                .findFirst()
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Question introuvable pour ce quiz"));

        Reponse choisie = question.getReponses().stream()
                .filter(r -> r.getId().equals(request.getReponseId()))
                .findFirst()
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.BAD_REQUEST, "Réponse invalide pour cette question"));

        Reponse bonne = question.getReponses().stream()
                .filter(r -> Boolean.TRUE.equals(r.getCorrecte()))
                .findFirst()
                .orElse(null);

        boolean estCorrecte = Boolean.TRUE.equals(choisie.getCorrecte());
        return VerificationQuestionWebResponse.builder()
                .correcte(estCorrecte)
                .reponseCorrecteId(bonne != null ? bonne.getId() : null)
                .reponseCorrecteContenu(bonne != null ? bonne.getContenu() : null)
                .build();
    }

    /**
     * Enregistre la liste d'emails autorisés sur le serveur et passe le quiz en {@link StatutQuiz#PUBLIE}.
     */
    @Transactional
    public void publierSurLeWeb(Long quizId, String professeurEmail, List<String> emailsBruts) {
        Quiz quiz = chargerQuizDuProfesseur(quizId, professeurEmail);

        Set<String> normalises = normaliserEmailsPublication(emailsBruts);
        if (normalises.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Aucun email valide dans la liste");
        }
        if (normalises.size() > QuizPublicationLimits.MAX_AUTHORIZED_STUDENT_EMAILS) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST,
                    "Maximum " + QuizPublicationLimits.MAX_AUTHORIZED_STUDENT_EMAILS + " emails autorisés");
        }

        quiz.getEmailsAutorisesWeb().clear();
        quiz.getEmailsAutorisesWeb().addAll(normalises);
        quiz.setStatut(StatutQuiz.PUBLIE);
        quiz.setDatePublication(LocalDateTime.now());
        quizRepository.save(quiz);

        String nomProf = quiz.getProfesseur() != null && quiz.getProfesseur().getNom() != null
                ? quiz.getProfesseur().getNom().trim()
                : "Votre professeur";
        String titreQuiz = quiz.getTitre() != null ? quiz.getTitre() : "";
        List<String> destinataires = new ArrayList<>(normalises);

        Runnable envoyerNotifications = () -> {
            for (String email : destinataires) {
                try {
                    emailService.sendQuizWebPublishedEmail(email, nomProf, titreQuiz);
                } catch (Exception ex) {
                    log.warn("Notification « nouveau quiz web » non envoyée à {} : {}", email, ex.getMessage());
                }
            }
        };

        if (TransactionSynchronizationManager.isSynchronizationActive()) {
            TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
                @Override
                public void afterCommit() {
                    envoyerNotifications.run();
                }
            });
        } else {
            envoyerNotifications.run();
        }
    }

    @Transactional
    public void synchroniserQuestionsPublicationWeb(
            Long quizId,
            String professeurEmail,
            List<PublicationWebQuestionRequest> questionsBrutes) {
        if (questionsBrutes == null || questionsBrutes.isEmpty()) {
            return;
        }

        Quiz quiz = chargerQuizDuProfesseur(quizId, professeurEmail);
        List<Question> nouvellesQuestions = questionsBrutes.stream()
                .map(q -> questionRepository.save(construireQuestionDepuisPublication(q, quiz.getProfesseur())))
                .toList();

        quiz.getQuestions().clear();
        quiz.getQuestions().addAll(nouvellesQuestions);
        quizRepository.save(quiz);
    }

    private static Set<String> normaliserEmailsPublication(List<String> emailsBruts) {
        if (emailsBruts == null) {
            return Set.of();
        }
        Set<String> out = new LinkedHashSet<>();
        for (String raw : emailsBruts) {
            if (raw == null) continue;
            String e = raw.trim().toLowerCase(Locale.ROOT);
            if (e.isEmpty()) continue;
            if (!EMAIL_SIMPLE.matcher(e).matches()) continue;
            out.add(e);
            if (out.size() > QuizPublicationLimits.MAX_AUTHORIZED_STUDENT_EMAILS) {
                break;
            }
        }
        return out;
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

        planifierRecalculStatistiquesApresCommit(quizId);

        return response;
    }

    @Transactional
    public ResultatQuizWebResponse soumettreQuizWeb(Long quizId, String emailEtudiant, SoumissionQuizWebRequest request) {
        Quiz quiz = chargerQuizPublieAutorise(quizId, emailEtudiant);
        Etudiant etudiant = etudiantRepository.findByEmail(emailEtudiant.trim().toLowerCase(Locale.ROOT))
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN, "Etudiant introuvable"));

        boolean premiere = isPremiereTentative(quizId, etudiant.getId());
        Map<Long, Long> reponsesParQuestion = new HashMap<>();
        if (request != null && request.getReponses() != null) {
            for (ReponseQuizDTO dto : request.getReponses()) {
                if (dto == null || dto.getQuestionId() == null || dto.getReponseId() == null) continue;
                reponsesParQuestion.put(dto.getQuestionId(), dto.getReponseId());
            }
        }

        int totalQuestions = quiz.getQuestions().size();
        int bonnes = 0;
        List<QuestionCorrectionWebResponse> corrections = quiz.getQuestions().stream()
                .map(question -> corrigerQuestion(question, reponsesParQuestion.get(question.getId()), etudiant, quizId, premiere))
                .toList();
        bonnes = (int) corrections.stream().filter(QuestionCorrectionWebResponse::isCorrecte).count();

        ResultatQuizWebResponse response = new ResultatQuizWebResponse();
        response.setBonnesReponses(bonnes);
        response.setTotalQuestions(totalQuestions);
        response.setEstPremiereTentative(premiere);
        response.setScore(totalQuestions == 0 ? 0.0 : ((double) bonnes / totalQuestions) * 100.0);
        response.setCorrections(corrections);

        planifierRecalculStatistiquesApresCommit(quizId);

        return response;
    }

    private void planifierRecalculStatistiquesApresCommit(Long quizId) {
        if (TransactionSynchronizationManager.isSynchronizationActive()) {
            TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
                @Override
                public void afterCommit() {
                    statistiqueRecalculService.planifierApresDelai(quizId);
                }
            });
        } else {
            statistiqueRecalculService.planifierApresDelai(quizId);
        }
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

        dto.setStatut(quiz.getStatut());
        dto.setDatePublication(quiz.getDatePublication());
        dto.setNombreQuestions(quiz.getQuestions() != null ? quiz.getQuestions().size() : 0);

        return dto;
    }

    private Quiz chargerQuizPublieAutorise(Long quizId, String emailEtudiant) {
        if (emailEtudiant == null || emailEtudiant.isBlank()) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Compte étudiant introuvable");
        }
        String email = emailEtudiant.trim().toLowerCase(Locale.ROOT);
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Quiz introuvable"));

        if (quiz.getStatut() != StatutQuiz.PUBLIE) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Ce quiz n'est pas publié sur le web");
        }
        if (quiz.getEmailsAutorisesWeb() == null || quiz.getEmailsAutorisesWeb().stream()
                .filter(Objects::nonNull)
                .map(e -> e.trim().toLowerCase(Locale.ROOT))
                .noneMatch(email::equals)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Vous n'êtes pas autorisé à passer ce quiz");
        }
        return quiz;
    }

    private Quiz chargerQuizDuProfesseur(Long quizId, String professeurEmail) {
        Professeur prof = professeurRepository.findByEmail(professeurEmail.trim().toLowerCase(Locale.ROOT))
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN, "Professeur introuvable"));

        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Quiz introuvable"));

        if (quiz.getProfesseur() == null || !quiz.getProfesseur().getId().equals(prof.getId())) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Ce quiz n'appartient pas à votre compte");
        }
        return quiz;
    }

    private static Question construireQuestionDepuisPublication(PublicationWebQuestionRequest src, Professeur professeur) {
        Question q = new Question();
        q.setEnonce(src != null && src.getEnonce() != null ? src.getEnonce().trim() : "");
        q.setType(TypeQuestion.QCM);
        q.setDifficulte(parseDifficulte(src != null ? src.getDifficulte() : null));
        q.setExplication(src != null && src.getExplication() != null ? src.getExplication().trim() : "");
        q.setProfesseur(professeur);

        String correcte = src != null && src.getReponseCorrecte() != null
                ? src.getReponseCorrecte().trim().toUpperCase(Locale.ROOT)
                : "";

        q.getReponses().add(buildReponse(q, src != null ? src.getOptionA() : null, "A".equals(correcte)));
        q.getReponses().add(buildReponse(q, src != null ? src.getOptionB() : null, "B".equals(correcte)));
        q.getReponses().add(buildReponse(q, src != null ? src.getOptionC() : null, "C".equals(correcte)));
        q.getReponses().add(buildReponse(q, src != null ? src.getOptionD() : null, "D".equals(correcte)));
        return q;
    }

    private static Reponse buildReponse(Question q, String contenu, boolean correcte) {
        Reponse r = new Reponse();
        r.setQuestion(q);
        r.setContenu(contenu == null ? "" : contenu.trim());
        r.setCorrecte(correcte);
        return r;
    }

    private static Difficulte parseDifficulte(String raw) {
        if (raw == null || raw.isBlank()) return Difficulte.MOYEN;
        String n = raw.trim().toUpperCase(Locale.ROOT);
        return switch (n) {
            case "FACILE" -> Difficulte.FACILE;
            case "DIFFICILE" -> Difficulte.DIFFICILE;
            default -> Difficulte.MOYEN;
        };
    }

    private QuestionPassageWebResponse toQuestionPassageWebDto(Question question) {
        QuestionPassageWebResponse dto = new QuestionPassageWebResponse();
        dto.setId(question.getId());
        dto.setEnonce(question.getEnonce());
        dto.setReponses(question.getReponses().stream().map(r -> {
            ReponsePassageWebResponse rr = new ReponsePassageWebResponse();
            rr.setId(r.getId());
            rr.setContenu(r.getContenu());
            return rr;
        }).toList());
        return dto;
    }

    private QuestionCorrectionWebResponse corrigerQuestion(
            Question question,
            Long reponseChoisieId,
            Etudiant etudiant,
            Long quizId,
            boolean premiere) {
        Reponse reponseCorrecte = question.getReponses().stream()
                .filter(r -> Boolean.TRUE.equals(r.getCorrecte()))
                .findFirst()
                .orElse(null);

        Reponse reponseChoisie = question.getReponses().stream()
                .filter(r -> r.getId().equals(reponseChoisieId))
                .findFirst()
                .orElse(null);

        boolean estCorrecte = reponseChoisie != null && Boolean.TRUE.equals(reponseChoisie.getCorrecte());

        if (reponseChoisie != null) {
            Resultat resultat = new Resultat();
            resultat.setEtudiant(etudiant);
            resultat.setQuestion(question);
            resultat.setReponse(reponseChoisie);
            resultat.setCorrecte(estCorrecte);
            resultat.setQuizId(quizId);
            resultat.setDatePassage(LocalDateTime.now());
            resultat.setEstPremiereTentative(premiere);
            resultatRepository.save(resultat);
        }

        QuestionCorrectionWebResponse dto = new QuestionCorrectionWebResponse();
        dto.setQuestionId(question.getId());
        dto.setEnonce(question.getEnonce());
        dto.setReponseChoisieId(reponseChoisie != null ? reponseChoisie.getId() : null);
        dto.setReponseChoisieContenu(reponseChoisie != null ? reponseChoisie.getContenu() : null);
        dto.setReponseCorrecteId(reponseCorrecte != null ? reponseCorrecte.getId() : null);
        dto.setReponseCorrecteContenu(reponseCorrecte != null ? reponseCorrecte.getContenu() : null);
        dto.setCorrecte(estCorrecte);
        dto.setExplication(question.getExplication());
        return dto;
    }

    /**
     * Supprime le quiz côté serveur uniquement s'il appartient au professeur connecté.
     */
    @Transactional
    public void deleteQuiz(Long quizId, String professeurEmail) {
        Professeur prof = professeurRepository.findByEmail(professeurEmail.trim().toLowerCase(Locale.ROOT))
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN, "Professeur introuvable"));

        Quiz quiz = quizRepository.findByIdWithQuestions(quizId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Quiz introuvable"));

        if (quiz.getProfesseur() == null || !quiz.getProfesseur().getId().equals(prof.getId())) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Ce quiz n'appartient pas à votre compte");
        }

        List<Question> questionsLiees = new ArrayList<>(quiz.getQuestions());
        for (Question q : questionsLiees) {
            reponseEtudiantRepository.deleteByQuestionId(q.getId());
        }

        statistiqueQuestionRepository.deleteAllByQuizId(quizId);
        resultatRepository.deleteByQuizId(quizId);

        quiz.getQuestions().clear();
        quizRepository.saveAndFlush(quiz);

        quizRepository.delete(quiz);
        quizRepository.flush();

        for (Question q : questionsLiees) {
            if (quizRepository.countQuizzesWithQuestion(q.getId()) == 0) {
                questionRepository.deleteById(q.getId());
            }
        }
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