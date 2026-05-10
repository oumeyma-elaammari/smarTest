package com.smartest.backend.service;

import com.smartest.backend.constants.QuizPublicationLimits;
import com.smartest.backend.dto.request.*;
import com.smartest.backend.dto.response.*;
import com.smartest.backend.entity.*;
import com.smartest.backend.entity.enumeration.StatutQuiz;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.exception.InvalidQuizStateException;
import com.smartest.backend.exception.QuestionNotFoundException;
import com.smartest.backend.exception.QuizNotFoundException;
import com.smartest.backend.exception.QuizPublicationLimitExceededException;
import com.smartest.backend.exception.UnauthorizedAccessException;
import com.smartest.backend.repository.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.messaging.simp.SimpMessagingTemplate;
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

@Slf4j
@Service
@RequiredArgsConstructor
public class QuizService {
    private static final String QUIZ_NON_TROUVE = "Quiz non trouvé";
    private static final String QUIZ_INTROUVABLE = "Quiz introuvable";
    private static final String PROFESSEUR_INTROUVABLE = "Professeur introuvable";
    private static final String QUIZ_HORS_PROPRIETE = "Ce quiz n'appartient pas à votre compte";

    private static ResponseStatusException badRequest(String message) {
        return new ResponseStatusException(HttpStatus.BAD_REQUEST, message);
    }

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
    private final StatistiqueService statistiqueService;
    private final SimpMessagingTemplate messagingTemplate;
    private final QuizQrLiveStatsService quizQrLiveStatsService;

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
                .orElseThrow(() -> new QuizNotFoundException(QUIZ_NON_TROUVE));
        return convertToDTO(quiz);
    }

    // ⚠️ supprimé findByProfesseurId (non existant)

    // ================= CREATE =================

    @Transactional
    public QuizResponse createQuiz(QuizRequest request) {

        Professeur professeur = professeurRepository.findById(request.getProfesseurId())
                .orElseThrow(() -> new QuizNotFoundException("Professeur non trouvé"));

        Quiz quiz = new Quiz();
        quiz.setTitre(request.getTitre());
        quiz.setProfesseur(professeur);

        // statut par défaut
        quiz.setStatut(StatutQuiz.BROUILLON);

        return convertToDTO(quizRepository.save(quiz));
    }

    // ================= PUBLICATION =================

    public void publierQuiz(Long id) {

        Quiz quiz = quizRepository.findById(id)
                .orElseThrow(() -> new QuizNotFoundException(QUIZ_INTROUVABLE));

        if (quiz.getStatut() == StatutQuiz.PUBLIE) {
            throw new InvalidQuizStateException("Ce quiz est déjà publié.");
        }

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
     * Quiz publiés sur le web (statut {@link StatutQuiz#PUBLIE}) dont l'email de l'étudiant figure
     * dans la liste autorisée. Les quiz sans aucune question (coquilles vides) sont exclus pour éviter les cartes
     * inexploitables après une synchro locale incomplète.
     */
    @Transactional(readOnly = true)
    public List<QuizResponse> getMesQuizsPublicationWeb(String emailEtudiant) {
        if (emailEtudiant == null || emailEtudiant.isBlank()) {
            return List.of();
        }
        String email = emailEtudiant.trim().toLowerCase(Locale.ROOT);
        Long etudiantId = etudiantRepository.findByEmail(email).map(Etudiant::getId).orElse(null);
        return quizRepository.findPubliesAutorisesPourEmail(email)
                .stream()
                .filter(quiz -> quiz.getQuestions() != null && !quiz.getQuestions().isEmpty())
                .map(quiz -> etudiantId != null
                        ? convertToDTOAvecScoreEtudiant(quiz, etudiantId)
                        : convertToDTOPublicationWebSansScoresEtudiant(quiz))
                .toList();
    }

    /**
     * Statistiques agrégées QR live (mémoire) : réservé au professeur propriétaire du quiz.
     */
    public QuizQrLiveStatsResponse getQrLiveStats(Long quizId, String professeurEmail) {
        chargerQuizDuProfesseur(quizId, professeurEmail);
        return quizQrLiveStatsService.snapshot(quizId);
    }

    /**
     * Remet à zéro les stats QR live pour ce quiz et diffuse le nouvel agrégat sur le topic STOMP.
     */
    public void clearQrLiveStats(Long quizId, String professeurEmail) {
        chargerQuizDuProfesseur(quizId, professeurEmail);
        quizQrLiveStatsService.clear(quizId);
        QuizQrLiveStatsResponse refreshed = quizQrLiveStatsService.snapshot(quizId);
        messagingTemplate.convertAndSend("/topic/quiz/" + quizId + "/qr-live", refreshed);
    }

    /**
     * JWT étudiant avec email autorisé mais fiche {@link Etudiant} absente (sync rare) : liste le quiz sans scores.
     */
    private QuizResponse convertToDTOPublicationWebSansScoresEtudiant(Quiz quiz) {
        QuizResponse dto = convertToDTO(quiz);
        dto.setPremiereTentative(true);
        dto.setMeilleurScore(null);
        return dto;
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
                .orElseThrow(() -> new QuestionNotFoundException("Question introuvable pour ce quiz"));

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
     * Première mise en ligne web : enregistre les emails autorisés, passe le quiz en {@link StatutQuiz#PUBLIE},
     * fixe {@code datePublication} et envoie les mails « nouveau quiz ».
     * <p>
     * Si le quiz était déjà {@link StatutQuiz#PUBLIE}, met seulement à jour la liste d'emails (synchronisation) :
     * pas de nouvelle date de publication et pas de renvoi massif des notifications.
     */
    @Transactional
    public void publierSurLeWeb(Long quizId, String professeurEmail, List<String> emailsBruts) {
        Quiz quiz = chargerQuizDuProfesseur(quizId, professeurEmail);
        boolean dejaWebPublie = quiz.getStatut() == StatutQuiz.PUBLIE;

        Set<String> normalises = normaliserEmailsPublication(emailsBruts);
        if (normalises.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Aucun email valide dans la liste");
        }
        if (normalises.size() > QuizPublicationLimits.MAX_AUTHORIZED_STUDENT_EMAILS) {
            throw new QuizPublicationLimitExceededException(QuizPublicationLimits.MAX_AUTHORIZED_STUDENT_EMAILS);
        }

        quiz.getEmailsAutorisesWeb().clear();
        quiz.getEmailsAutorisesWeb().addAll(normalises);
        quiz.setStatut(StatutQuiz.PUBLIE);
        if (!dejaWebPublie) {
            quiz.setDatePublication(LocalDateTime.now());
        }
        quizRepository.save(quiz);

        if (dejaWebPublie) {
            return;
        }

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
                .map(q -> questionRepository.save(PublicationWebQuestionFactory.creerDepuisPublication(q, quiz.getProfesseur())))
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
            if (raw != null) {
                String e = raw.trim().toLowerCase(Locale.ROOT);
                if (!e.isEmpty() && isSimpleEmailValid(e)) {
                    out.add(e);
                }
            }
        }
        return out;
    }

    private static boolean isSimpleEmailValid(String email) {
        int at = email.indexOf('@');
        if (at <= 0 || at != email.lastIndexOf('@') || at >= email.length() - 3) {
            return false;
        }

        int dotAfterAt = email.indexOf('.', at + 1);
        if (dotAfterAt <= at + 1 || dotAfterAt >= email.length() - 1) {
            return false;
        }

        for (int i = 0; i < email.length(); i++) {
            char c = email.charAt(i);
            if (Character.isWhitespace(c)) {
                return false;
            }
        }
        return true;
    }

    // ================= LOGIC =================

    public boolean isPremiereTentative(Long quizId, Long etudiantId) {
        return !resultatRepository.existsByEtudiantIdAndQuizId(etudiantId, quizId);
    }

    @Transactional
    public ResultatQuizResponse soumettreQuiz(Long quizId, SoumissionQuizRequest request) {
        if (request == null) {
            throw badRequest("Requête de soumission incomplète");
        }
        if (request.getEtudiantId() == null) {
            throw badRequest("L'identifiant de l'étudiant est obligatoire");
        }
        if (request.getReponses() == null) {
            throw badRequest("La liste des réponses est obligatoire");
        }

        quizRepository.findById(quizId)
                .orElseThrow(() -> new QuizNotFoundException(QUIZ_NON_TROUVE));

        Etudiant etudiant = etudiantRepository.findById(request.getEtudiantId())
                .orElseThrow(() -> new QuizNotFoundException("Etudiant introuvable"));

        boolean premiere = isPremiereTentative(quizId, etudiant.getId());

        int total = request.getReponses().size();
        int correct = 0;

        for (ReponseQuizDTO dto : request.getReponses()) {
            if (dto == null || dto.getReponseId() == null) {
                throw badRequest("Chaque réponse doit indiquer un identifiant de réponse");
            }

            Reponse r = reponseRepository.findById(dto.getReponseId())
                    .orElseThrow(() -> new QuizNotFoundException("Réponse introuvable"));

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
        planifierPublicationStatistiquesTempsReelApresCommit(quizId);

        return response;
    }

    @Transactional
    public ResultatQuizWebResponse soumettreQuizWeb(
            Long quizId,
            String emailEtudiant,
            SoumissionQuizWebRequest request) {
        Quiz quiz = chargerQuizPublieAutorise(quizId, emailEtudiant);
        Etudiant etudiant = etudiantRepository.findByEmail(emailEtudiant.trim().toLowerCase(Locale.ROOT))
                .orElseThrow(() -> new UnauthorizedAccessException("Etudiant introuvable."));

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
                .map(question -> corrigerQuestion(
                        question,
                        reponsesParQuestion.get(question.getId()),
                        etudiant,
                        quizId,
                        premiere,
                        true))
                .toList();
        bonnes = (int) corrections.stream().filter(QuestionCorrectionWebResponse::isCorrecte).count();

        ResultatQuizWebResponse response = new ResultatQuizWebResponse();
        response.setBonnesReponses(bonnes);
        response.setTotalQuestions(totalQuestions);
        response.setEstPremiereTentative(premiere);
        response.setScore(totalQuestions == 0 ? 0.0 : ((double) bonnes / totalQuestions) * 100.0);
        response.setCorrections(corrections);

        planifierRecalculStatistiquesApresCommit(quizId);
        planifierPublicationStatistiquesTempsReelApresCommit(quizId);

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

    private void planifierPublicationStatistiquesTempsReelApresCommit(Long quizId) {
        Runnable action = () -> publierStatistiquesTempsReel(quizId);
        if (TransactionSynchronizationManager.isSynchronizationActive()) {
            TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
                @Override
                public void afterCommit() {
                    action.run();
                }
            });
        } else {
            action.run();
        }
    }

    private void publierStatistiquesTempsReel(Long quizId) {
        try {
            Quiz quiz = quizRepository.findById(quizId).orElse(null);
            if (quiz == null || quiz.getProfesseur() == null || quiz.getProfesseur().getEmail() == null) {
                return;
            }
            String emailProf = quiz.getProfesseur().getEmail();
            StatistiquesQuizResponse stats = statistiqueService.obtenirStatistiquesQuizPourProfesseur(quizId, emailProf);
            messagingTemplate.convertAndSend("/topic/quiz/" + quizId + "/stats", stats);
        } catch (Exception ex) {
            log.warn("Publication stats temps réel échouée pour quiz {}: {}", quizId, ex.getMessage());
        }
    }

    // ================= DTO =================

    private QuizResponse convertToDTO(Quiz quiz) {

        QuizResponse dto = new QuizResponse();

        dto.setId(quiz.getId());
        dto.setTitre(quiz.getTitre());

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
            throw new UnauthorizedAccessException("Compte étudiant introuvable.");
        }
        String email = emailEtudiant.trim().toLowerCase(Locale.ROOT);
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new QuizNotFoundException(QUIZ_INTROUVABLE));

        if (quiz.getStatut() != StatutQuiz.PUBLIE) {
            throw new InvalidQuizStateException("Ce quiz n'est pas publié sur le web.");
        }
        if (quiz.getEmailsAutorisesWeb() == null || quiz.getEmailsAutorisesWeb().stream()
                .filter(Objects::nonNull)
                .map(e -> e.trim().toLowerCase(Locale.ROOT))
                .noneMatch(email::equals)) {
            throw new UnauthorizedAccessException("Vous n'êtes pas autorisé à passer ce quiz.");
        }
        return quiz;
    }

    private Quiz chargerQuizDuProfesseur(Long quizId, String professeurEmail) {
        Professeur prof = professeurRepository.findByEmail(professeurEmail.trim().toLowerCase(Locale.ROOT))
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN, PROFESSEUR_INTROUVABLE));

        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new QuizNotFoundException(QUIZ_INTROUVABLE));

        if (quiz.getProfesseur() == null || !quiz.getProfesseur().getId().equals(prof.getId())) {
            throw new UnauthorizedAccessException(QUIZ_HORS_PROPRIETE);
        }
        return quiz;
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
            boolean premiere,
            boolean persisterResultat) {
        Reponse reponseCorrecte = question.getReponses().stream()
                .filter(r -> Boolean.TRUE.equals(r.getCorrecte()))
                .findFirst()
                .orElse(null);

        Reponse reponseChoisie = question.getReponses().stream()
                .filter(r -> r.getId().equals(reponseChoisieId))
                .findFirst()
                .orElse(null);

        boolean estCorrecte = reponseChoisie != null && Boolean.TRUE.equals(reponseChoisie.getCorrecte());

        if (reponseChoisie != null && persisterResultat && etudiant != null) {
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
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN, PROFESSEUR_INTROUVABLE));

        Quiz quiz = quizRepository.findByIdWithQuestions(quizId)
                .orElseThrow(() -> new QuizNotFoundException(QUIZ_INTROUVABLE));

        if (quiz.getProfesseur() == null || !quiz.getProfesseur().getId().equals(prof.getId())) {
            throw new UnauthorizedAccessException(QUIZ_HORS_PROPRIETE);
        }

        List<Question> questionsLiees = new ArrayList<>(quiz.getQuestions());
        for (Question q : questionsLiees) {
            reponseEtudiantRepository.deleteByQuestionId(q.getId());
        }

        // Réponses liées aux résultats de passage (quiz web / tentative) avant de supprimer les Resultat.
        reponseEtudiantRepository.deleteByResultatQuizId(quizId);

        statistiqueQuestionRepository.deleteAllByQuizId(quizId);
        resultatRepository.deleteByQuizId(quizId);

        quiz.getQuestions().clear();
        quizRepository.saveAndFlush(quiz);

        /* Liaisons et ligne quiz en SQL natif : garantit la suppression MySQL même si l’état
         * persistence / collections ManyToMany laisse des lignes résiduelles dans quiz_question
         * ou quiz_email_web_autorise. */
        quizRepository.deleteNativeQuizQuestionLinks(quizId);
        quizRepository.deleteNativeQuizEmailWebRows(quizId);
        int quizRows = quizRepository.deleteNativeById(quizId);
        if (quizRows != 1) {
            log.warn("Suppression quiz id={} : DELETE a affecté {} ligne(s) (attendu 1)", quizId, quizRows);
        }

        for (Question q : questionsLiees) {
            if (quizRepository.countQuizzesWithQuestion(q.getId()) == 0) {
                questionRepository.deleteById(q.getId());
            }
        }

        log.info("Quiz supprimé côté serveur (quizId={}, professeurEmail={})", quizId,
                professeurEmail.trim().toLowerCase(Locale.ROOT));
    }

    @Transactional
    public QuizResponse addQuestionToQuiz(Long quizId, Long questionId) {
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new QuizNotFoundException(QUIZ_NON_TROUVE));
        Question question = questionRepository.findById(questionId)
                .orElseThrow(() -> new QuestionNotFoundException("Question non trouvée"));

        quiz.getQuestions().add(question);
        quizRepository.save(quiz);

        return convertToDTO(quiz);
    }

    @Transactional
    public void removeQuestionFromQuiz(Long quizId, Long questionId) {
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new QuizNotFoundException(QUIZ_NON_TROUVE));
        Question question = questionRepository.findById(questionId)
                .orElseThrow(() -> new QuestionNotFoundException("Question non trouvée"));

        quiz.getQuestions().remove(question);
        quizRepository.save(quiz);
    }

}