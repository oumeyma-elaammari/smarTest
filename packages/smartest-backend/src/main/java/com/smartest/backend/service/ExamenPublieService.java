package com.smartest.backend.service;

import com.smartest.backend.dto.request.PublicationWebQuestionRequest;
import com.smartest.backend.dto.response.ExamenPublieMetadataResponse;
import com.smartest.backend.exception.InvalidSessionStateException;
import com.smartest.backend.exception.UnauthorizedAccessException;
import com.smartest.backend.entity.*;
import com.smartest.backend.entity.enumeration.StatutExamen;
import com.smartest.backend.repository.*;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import java.util.Objects;
import java.util.Optional;

@Service
@RequiredArgsConstructor
public class ExamenPublieService {

    private final ExamenPublieRepository examenPublieRepository;
    private final ProfesseurRepository professeurRepository;
    private final QuestionRepository questionRepository;
    private final QuizRepository quizRepository;
    private final SessionExamenRepository sessionExamenRepository;
    private final ReponseEtudiantRepository reponseEtudiantRepository;
    private final StatistiqueQuestionRepository statistiqueQuestionRepository;
    private final ExamenSupervisionService examenSupervisionService;
    private final EmailService emailService;

    private static final double BAREME_DEFAUT_WEB = 20.0;
    private static final String PROFESSEUR_INTROUVABLE = "Professeur introuvable";
    private static final String EXAMEN_INTROUVABLE = "Examen introuvable";
    private static final String EXAMEN_HORS_PROPRIETE = "Cet examen n'appartient pas à votre compte";

    @Transactional(readOnly = true)
    public List<ExamenPublieMetadataResponse> getMesExamensPublicationWeb(String emailUtilisateur) {
        if (emailUtilisateur == null || emailUtilisateur.isBlank()) {
            return List.of();
        }
        String email = emailUtilisateur.trim().toLowerCase(Locale.ROOT);
        Optional<Professeur> prof = professeurRepository.findByEmailIgnoreCase(email);
        if (prof.isPresent()) {
            return examenPublieRepository.findPublieWebParProfesseur(prof.get().getId()).stream()
                    .map(this::toMetadata)
                    .toList();
        }
        return examenPublieRepository.findAutorisesPourEmail(email).stream()
                .map(this::toMetadata)
                .toList();
    }

    @Transactional(readOnly = true)
    public ExamenPublieMetadataResponse getMetadataPourEtudiant(Long examenId, String emailEtudiant) {
        ExamenPublie ex = examenPublieRepository.findByIdFetchingEmailsAutorisesWeb(examenId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Examen introuvable"));
        verifierEmailAutorisePourExamen(ex, emailEtudiant);
        return toMetadata(ex);
    }

    /** Métadonnées pour le professeur propriétaire (supervision web, sans contrainte publication web). */
    @Transactional(readOnly = true)
    public ExamenPublieMetadataResponse getMetadataPourProfesseur(Long examenId, String emailProf) {
        if (emailProf == null || emailProf.isBlank()) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Email requis");
        }
        String email = emailProf.trim().toLowerCase(Locale.ROOT);
        Professeur prof = professeurRepository.findByEmailIgnoreCase(email)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN, "Accès réservé aux professeurs"));
        ExamenPublie ex = examenPublieRepository.findById(examenId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Examen introuvable"));
        if (ex.getProfesseur() == null || !ex.getProfesseur().getId().equals(prof.getId())) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Cet examen n'appartient pas à votre compte");
        }
        return toMetadata(ex);
    }

    /**
     * Met à jour les emails web ; utilisé à la publication depuis le desktop.
     */
    @Transactional
    public void definirEmailsPublicationWeb(Long examenId, Long professeurId, List<String> emailsBruts) {
        ExamenPublie ex = examenPublieRepository.findByIdFetchingEmailsAutorisesWeb(examenId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Examen introuvable"));
        if (ex.getProfesseur() == null || !ex.getProfesseur().getId().equals(professeurId)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Cet examen n'appartient pas à votre compte");
        }
        ex.getEmailsAutorisesWeb().clear();
        if (emailsBruts != null) {
            for (String raw : emailsBruts) {
                if (raw != null && !raw.isBlank()) {
                    ex.getEmailsAutorisesWeb().add(raw.trim().toLowerCase(Locale.ROOT));
                }
            }
        }
        if (ex.getEmailsAutorisesWeb().isEmpty()) {
            ex.setPublieSurWebLe(null);
        } else {
            ex.setPublieSurWebLe(LocalDateTime.now());
        }
        examenPublieRepository.save(ex);

        if (!ex.getEmailsAutorisesWeb().isEmpty()) {
            String profNom = ex.getProfesseur() != null ? ex.getProfesseur().getNom() : null;
            String titre = ex.getTitre();
            for (String email : ex.getEmailsAutorisesWeb()) {
                try {
                    emailService.sendExamenPublieEmail(email, profNom, titre);
                } catch (Exception ignored) {
                }
            }
        }
    }

    /**
     * Envoie depuis le bureau le contenu de l'examen : crée les questions côté serveur et rattache à
     * l'{@link ExamenPublie} (même modèle que la publication web du quiz).
     */
    @Transactional
    public void synchroniserQuestionsPublicationWeb(
            Long examenId,
            String emailProfesseur,
            List<PublicationWebQuestionRequest> questionsBrutes) {
        if (questionsBrutes == null || questionsBrutes.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST,
                    "Au moins une question est requise pour publier le contenu de l'examen web.");
        }
        String email = emailProfesseur.trim().toLowerCase(Locale.ROOT);
        Professeur prof = professeurRepository.findByEmailIgnoreCase(email)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN, "Professeur introuvable"));
        ExamenPublie ex = examenPublieRepository.findById(examenId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Examen introuvable"));
        if (ex.getProfesseur() == null || !ex.getProfesseur().getId().equals(prof.getId())) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Cet examen n'appartient pas à votre compte");
        }

        List<Question> nouvelles = new ArrayList<>();
        for (PublicationWebQuestionRequest q : questionsBrutes) {
            nouvelles.add(questionRepository.save(PublicationWebQuestionFactory.creerDepuisPublication(q, prof)));
        }
        if (ex.getQuestions() == null) {
            ex.setQuestions(new ArrayList<>());
        }
        ex.getQuestions().clear();
        ex.getQuestions().addAll(nouvelles);
        examenPublieRepository.saveAndFlush(ex);
    }

    private void verifierEmailAutorisePourExamen(ExamenPublie ex, String emailEtudiant) {
        if (emailEtudiant == null || emailEtudiant.isBlank()) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Email requis");
        }
        if (ex.getPublieSurWebLe() == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Examen introuvable");
        }
        String email = emailEtudiant.trim().toLowerCase(Locale.ROOT);
        if (ex.getEmailsAutorisesWeb() == null || ex.getEmailsAutorisesWeb().isEmpty()) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Vous n'êtes pas autorisé pour cet examen");
        }
        boolean ok = ex.getEmailsAutorisesWeb().stream()
                .filter(Objects::nonNull)
                .map(e -> e.trim().toLowerCase(Locale.ROOT))
                .anyMatch(email::equals);
        if (!ok) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Vous n'êtes pas autorisé pour cet examen");
        }
    }

    private ExamenPublieMetadataResponse toMetadata(ExamenPublie ex) {
        long n = examenPublieRepository.countLinkedQuestions(ex.getId());
        int totalQuestions = n > Integer.MAX_VALUE ? Integer.MAX_VALUE : (int) n;
        String nomProf = ex.getProfesseur() != null && ex.getProfesseur().getNom() != null
                ? ex.getProfesseur().getNom()
                : null;
        return new ExamenPublieMetadataResponse(
                ex.getId(),
                ex.getTitre(),
                ex.getDescription(),
                ex.getDateDebut(),
                ex.getDateFin(),
                ex.getDuree(),
                ex.getStatut() != null ? ex.getStatut().name() : null,
                totalQuestions,
                BAREME_DEFAUT_WEB,
                false,
                nomProf
        );
    }

    public ExamenPublie publier(Long professeurId, String titre, Integer duree, String description,
                                LocalDateTime debut, LocalDateTime fin) {

        Professeur prof = professeurRepository.findById(professeurId)
                .orElseThrow(() -> new IllegalArgumentException("Professeur introuvable"));

        ExamenPublie exam = new ExamenPublie();
        exam.setTitre(titre);
        exam.setDuree(duree);
        exam.setDescription(description);
        exam.setProfesseur(prof);
        exam.setStatut(StatutExamen.PLANIFIE);
        exam.setDateDebut(debut);
        exam.setDateFin(fin);
        exam.setDateCreation(LocalDateTime.now());

        return examenPublieRepository.save(exam);
    }

    public List<ExamenPublie> findAll() {
        return examenPublieRepository.findAll();
    }

    public List<ExamenPublie> getDisponibles() {
        LocalDateTime now = LocalDateTime.now();
        return examenPublieRepository.findByStatutAndDateDebutBeforeAndDateFinAfter(
                StatutExamen.EN_COURS, now, now);
    }

    public ExamenPublie demarrer(Long id) {
        ExamenPublie exam = examenPublieRepository.findById(id)
                .orElseThrow(() -> new IllegalArgumentException("Examen non trouvé"));

        if (exam.getStatut() != StatutExamen.PLANIFIE) {
            throw new InvalidSessionStateException(
                    "Impossible de démarrer cet examen dans son état actuel : " + exam.getStatut());
        }

        exam.setStatut(StatutExamen.EN_COURS);
        return examenPublieRepository.save(exam);
    }

    public ExamenPublie terminer(Long id) {
        ExamenPublie exam = examenPublieRepository.findById(id)
                .orElseThrow(() -> new IllegalArgumentException("Examen non trouvé"));

        if (exam.getStatut() != StatutExamen.EN_COURS) {
            throw new InvalidSessionStateException("Impossible de terminer un examen qui n'est pas en cours.");
        }

        exam.setStatut(StatutExamen.TERMINE);
        return examenPublieRepository.save(exam);
    }

    /**
     * Supprime l'examen publié côté serveur : réservé au professeur propriétaire (aligné sur {@link QuizService#deleteQuiz}).
     */
    @Transactional
    public void deleteExamenPublie(Long examenId, String professeurEmail) {
        Professeur prof = professeurRepository.findByEmail(professeurEmail.trim().toLowerCase(Locale.ROOT))
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN, PROFESSEUR_INTROUVABLE));

        ExamenPublie exam = examenPublieRepository.findWithQuestionsAndProfesseurById(examenId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, EXAMEN_INTROUVABLE));

        if (exam.getProfesseur() == null || !exam.getProfesseur().getId().equals(prof.getId())) {
            throw new UnauthorizedAccessException(EXAMEN_HORS_PROPRIETE);
        }

        examenSupervisionService.evictRuntimeState(examenId);

        List<Question> questionsLiees = exam.getQuestions() != null
                ? new ArrayList<>(exam.getQuestions())
                : new ArrayList<>();

        for (Question q : questionsLiees) {
            reponseEtudiantRepository.deleteByQuestionId(q.getId());
        }

        List<SessionExamen> sessions = sessionExamenRepository.findByExamenPublieId(examenId);
        List<Long> sessionIds = sessions.stream()
                .map(SessionExamen::getId)
                .filter(Objects::nonNull)
                .toList();

        if (!sessionIds.isEmpty()) {
            statistiqueQuestionRepository.deleteAllBySessionExamenIdIn(sessionIds);
        }
        statistiqueQuestionRepository.deleteAllByExamenPublieId(examenId);

        for (SessionExamen s : sessions) {
            reponseEtudiantRepository.deleteBySessionExamenId(s.getId());
        }
        sessionExamenRepository.deleteAll(sessions);

        if (exam.getQuestions() != null) {
            exam.getQuestions().clear();
        }
        examenPublieRepository.saveAndFlush(exam);

        examenPublieRepository.delete(exam);
        examenPublieRepository.flush();

        for (Question q : questionsLiees) {
            if (quizRepository.countQuizzesWithQuestion(q.getId()) == 0
                    && questionRepository.countExamensWithQuestion(q.getId()) == 0) {
                questionRepository.deleteById(q.getId());
            }
        }
    }
}
