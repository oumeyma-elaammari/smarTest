package com.smartest.backend.service;

import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.entity.enumeration.StatutExamen;
import com.smartest.backend.repository.ExamenPublieRepository;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import jakarta.annotation.PreDestroy;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashSet;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Locale;
import java.util.Set;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;
import java.util.stream.Collectors;

@Service
public class ExamenSupervisionService {
    private static final double DEFAULT_BAREME = 20.0;

    private final SimpMessagingTemplate messagingTemplate;
    private final ExamenPublieRepository examenPublieRepository;
    private final ObjectMapper objectMapper;
    private final EmailService emailService;
    private final Map<Long, ExamenRuntimeState> states = new ConcurrentHashMap<>();
    /** Décompte 1 s pour le minuteur de la question courante (pas de passage auto à la suivante). */
    private final Map<Long, ScheduledFuture<?>> questionTickerTasks = new ConcurrentHashMap<>();
    private final ScheduledExecutorService scheduler = Executors.newSingleThreadScheduledExecutor();

    public ExamenSupervisionService(SimpMessagingTemplate messagingTemplate,
                                    ExamenPublieRepository examenPublieRepository,
                                    ObjectMapper objectMapper,
                                    EmailService emailService) {
        this.messagingTemplate = messagingTemplate;
        this.examenPublieRepository = examenPublieRepository;
        this.objectMapper = objectMapper;
        this.emailService = emailService;
    }

    /** Bloque la salle d’attente étudiant tant que le créneau de début n’est pas atteint côté serveur. */
    private void verifierCreneauExamenAtteint(Long examenPublieId) {
        ExamenPublie exam = examenPublieRepository.findById(examenPublieId).orElse(null);
        if (exam == null || exam.getDateDebut() == null) {
            return;
        }
        LocalDateTime now = LocalDateTime.now();
        if (now.isBefore(exam.getDateDebut())) {
            throw new IllegalArgumentException(
                    "L'examen n'est pas encore accessible. Revenez à partir du "
                            + exam.getDateDebut() + " (heure du serveur).");
        }
    }

    public JoinRoomResponse rejoindreSalleAttente(Long examenId, Long etudiantId, String email) {
        mergeEmailsFromDatabase(examenId);
        verifierCreneauExamenAtteint(examenId);
        ExamenRuntimeState state = getState(examenId);
        ensurePhaseIsNot(state, "TERMINE", "ARRETE");
        if (!state.allowedEmails.isEmpty()) {
            String normalized = normalizeEmail(email);
            if (normalized.isEmpty() || !state.allowedEmails.contains(normalized)) {
                throw new IllegalArgumentException("Vous n'êtes pas autorisé à rejoindre cet examen.");
            }
        }
        StudentPresence presence = new StudentPresence(etudiantId, email, LocalDateTime.now());
        state.waitingRoom.put(etudiantId, presence);

        JoinRoomResponse response = new JoinRoomResponse(
                examenId,
                true,
                "EN_COURS".equals(state.phase),
                state.phase
        );
        publish(examenId, "salle-attente", getSalleAttente(examenId));
        /* Supervision et minuteurs prof : aligner participants / compteurs sur /topic/.../etat */
        publishAndSnapshot(examenId);
        return response;
    }

    @Transactional
    public Map<String, Object> definirEmailsAutorises(Long examenId, List<String> emails) {
        ExamenPublie examEntity = examenPublieRepository.findByIdFetchingEmailsAutorisesWeb(examenId)
                .orElseThrow(() -> new IllegalArgumentException("Examen non trouvé"));
        ExamenRuntimeState state = getState(examenId);
        Set<String> normalized = new LinkedHashSet<>();
        if (emails != null) {
            for (String raw : emails) {
                String email = normalizeEmail(raw);
                if (!email.isEmpty()) {
                    normalized.add(email);
                }
            }
        }
        state.allowedEmails.clear();
        state.allowedEmails.addAll(normalized);

        examEntity.getEmailsAutorisesWeb().clear();
        examEntity.getEmailsAutorisesWeb().addAll(normalized);
        if (normalized.isEmpty()) {
            examEntity.setPublieSurWebLe(null);
        } else {
            examEntity.setPublieSurWebLe(LocalDateTime.now());
        }
        examenPublieRepository.save(examEntity);

        Map<String, Object> response = new LinkedHashMap<>();
        response.put("examenId", examenId);
        response.put("nombreEmailsAutorises", state.allowedEmails.size());
        response.put("emailsAutorises", new ArrayList<>(state.allowedEmails));
        publish(examenId, "emails-autorises", response);
        return response;
    }

    public WaitingRoomResponse getSalleAttente(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        List<StudentPresence> attendees = new ArrayList<>(state.waitingRoom.values());
        attendees.sort(Comparator.comparing(StudentPresence::joinedAt));

        return new WaitingRoomResponse(examenId, state.phase, attendees, attendees.size());
    }

    /**
     * @param emailEtudiantConnecte email du compte (JWT), pour enregistrer implicitement la présence
     *                                si la session est déjà démarrée et que l'étudiant n'a pas encore
     *                                eu le temps d'appeler {@code rejoindre} (course au lancement prof).
     */
    public ExamQuestionStateResponse getQuestionCouranteEtudiant(Long examenId, Long etudiantId, String emailEtudiantConnecte) {
        mergeEmailsFromDatabase(examenId);
        ExamenRuntimeState state = getState(examenId);
        if (etudiantId == null || etudiantId <= 0) {
            throw new IllegalArgumentException("Étudiant invalide.");
        }

        boolean inRoom = state.waitingRoom.containsKey(etudiantId);
        if (!inRoom && ("EN_COURS".equals(state.phase) || "EN_PAUSE".equals(state.phase))) {
            rejoindreImplicitementPourSessionEnCours(examenId, etudiantId, emailEtudiantConnecte, state);
            inRoom = state.waitingRoom.containsKey(etudiantId);
        }

        if (!inRoom) {
            /*
             * PLANIFIE / TERMINE : pas de contenu tant que pas inscrit en salle (sauf méta via total).
             */
            ExamenPublie examSansRoom = chargerExamen(examenId);
            int totalSansRoom = questionsOrderedForExamen(examSansRoom).size();
            return new ExamQuestionStateResponse(
                    examenId,
                    state.phase,
                    state.paused,
                    totalSansRoom,
                    null,
                    null,
                    state.remainingMinutes,
                    null,
                    resolveIndicativeSeconds(examSansRoom, 0, state)
            );
        }

        ExamenPublie exam = chargerExamen(examenId);
        List<Question> ordered = questionsOrderedForExamen(exam);
        int totalQuestions = ordered.size();
        Integer idx = null;
        if (totalQuestions > 0) {
            idx = Math.max(0, Math.min(state.currentQuestionIndex, totalQuestions - 1));
        }

        boolean montrerContenuQuestions =
                "EN_COURS".equals(state.phase) || "EN_PAUSE".equals(state.phase);
        Map<String, Object> questionPayload =
                montrerContenuQuestions ? buildQuestionPayload(ordered, idx) : null;

        return new ExamQuestionStateResponse(
                examenId,
                state.phase,
                state.paused,
                totalQuestions,
                idx,
                questionPayload,
                state.remainingMinutes,
                state.questionRemainingSeconds,
                idx != null ? resolveIndicativeSeconds(exam, idx, state) : resolveIndicativeSeconds(exam, 0, state)
        );
    }

    /**
     * Même règle d’emails que {@link #rejoindreSalleAttente} ; pas de blocage créneau ici car
     * {@code getMetadataPourEtudiant} a déjà validé l’accès avant l’appel contrôleur.
     */
    private void rejoindreImplicitementPourSessionEnCours(
            Long examenId,
            Long etudiantId,
            String emailBrut,
            ExamenRuntimeState state) {
        if (emailBrut == null || emailBrut.isBlank()) {
            throw new IllegalArgumentException(
                    "Rejoignez la salle d’attente sur la page de l’examen pour participer.");
        }
        String normalized = normalizeEmail(emailBrut);
        if (!state.allowedEmails.isEmpty() && (normalized.isEmpty() || !state.allowedEmails.contains(normalized))) {
            throw new IllegalArgumentException("Vous n'êtes pas autorisé à rejoindre cet examen.");
        }
        state.waitingRoom.put(etudiantId, new StudentPresence(etudiantId, emailBrut.trim(), LocalDateTime.now()));
        publish(examenId, "salle-attente", getSalleAttente(examenId));
        publishAndSnapshot(examenId);
    }

    public Map<String, Object> enregistrerReponseEtudiant(Long examenId, Long etudiantId, Long questionId, Long reponseId) {
        ExamenRuntimeState state = getState(examenId);
        if ("TERMINE".equals(state.phase)) {
            throw new IllegalStateException("La session est terminée ; vous ne pouvez plus modifier vos réponses.");
        }
        ensurePhase(state, "EN_COURS");
        if (state.paused) {
            throw new IllegalStateException("L'examen est en pause.");
        }
        if (etudiantId == null || etudiantId <= 0) {
            throw new IllegalArgumentException("Étudiant invalide.");
        }
        if (!state.waitingRoom.containsKey(etudiantId)) {
            throw new IllegalArgumentException("Étudiant non inscrit dans la session d'examen.");
        }
        if (questionId == null || reponseId == null) {
            throw new IllegalArgumentException("Question et réponse sont obligatoires.");
        }

        ExamenPublie exam = chargerExamen(examenId);
        List<Question> ordered = questionsOrderedForExamen(exam);
        int totalQuestions = ordered.size();
        if (totalQuestions <= 0) {
            throw new IllegalStateException("Aucune question disponible pour cet examen.");
        }
        int currentIdx = Math.max(0, Math.min(state.currentQuestionIndex, totalQuestions - 1));
        Question currentQuestion = ordered.get(currentIdx);
        if (!currentQuestion.getId().equals(questionId)) {
            throw new IllegalStateException("Vous ne pouvez répondre qu'à la question active.");
        }

        if (state.questionRemainingSeconds <= 0) {
            throw new IllegalStateException(
                    "Le temps imparti pour cette question est écoulé. Attendez que le professeur ajoute du temps au minuteur.");
        }

        Reponse choix = currentQuestion.getReponses()
                .stream()
                .filter(r -> r.getId() != null && r.getId().equals(reponseId))
                .findFirst()
                .orElseThrow(() -> new IllegalArgumentException("Réponse invalide pour la question active."));

        state.answersByStudent
                .computeIfAbsent(etudiantId, __ -> new ConcurrentHashMap<>())
                .put(questionId, choix.getId());

        /* Actualise la supervision prof en temps réel (compteur réponses / étudiants actifs). */
        int repCount = compterReponsesPourQuestionCourante(state, ordered, currentIdx);
        Map<String, Object> compteurs = new LinkedHashMap<>();
        compteurs.put("reponsesPourQuestionCourante", repCount);
        compteurs.put("participantsEnAttente", state.waitingRoom.size());
        compteurs.put("questionCouranteIndex", currentIdx);
        publish(examenId, "compteurs-supervision", compteurs);
        publishAndSnapshot(examenId);

        Map<String, Object> response = new LinkedHashMap<>();
        response.put("enregistree", true);
        response.put("examenId", examenId);
        response.put("questionId", questionId);
        response.put("aucuneCorrectionImmediate", true);
        return response;
    }

    public Map<String, Object> soumettreExamenEtudiant(Long examenId, Long etudiantId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhaseIsNot(state, "ARRETE");
        if (!"TERMINE".equals(state.phase)) {
            throw new IllegalStateException(
                    "La soumission finale n'est possible qu'après la fin de session par le professeur.");
        }
        if (etudiantId == null || etudiantId <= 0) {
            throw new IllegalArgumentException("Étudiant invalide.");
        }
        if (!state.waitingRoom.containsKey(etudiantId)) {
            throw new IllegalArgumentException("Étudiant non inscrit dans la session d'examen.");
        }
        if (state.finalSubmittedStudents.contains(etudiantId)) {
            Map<String, Object> already = new LinkedHashMap<>();
            already.put("soumis", true);
            already.put("dejaSoumis", true);
            already.put("aucuneCorrectionImmediate", true);
            return already;
        }

        ExamenPublie exam = chargerExamen(examenId);
        List<Question> ordered = questionsOrderedForExamen(exam);
        int totalQuestions = ordered.size();
        Map<Long, Long> answers = state.answersByStudent.getOrDefault(etudiantId, new ConcurrentHashMap<>());

        int bonnes = 0;
        for (Question q : ordered) {
            Long selectedResponseId = answers.get(q.getId());
            if (selectedResponseId == null) continue;
            boolean isCorrect = q.getReponses()
                    .stream()
                    .anyMatch(r -> r.getId() != null &&
                            r.getId().equals(selectedResponseId) &&
                            Boolean.TRUE.equals(r.getCorrecte()));
            if (isCorrect) {
                bonnes++;
            }
        }

        enregistrerResultatEtudiant(examenId, etudiantId, bonnes, totalQuestions);
        state.finalSubmittedStudents.add(etudiantId);

        Map<String, Object> response = new LinkedHashMap<>();
        response.put("soumis", true);
        response.put("dejaSoumis", false);
        response.put("aucuneCorrectionImmediate", true);
        response.put("message", "Examen soumis. Le résultat sera validé ultérieurement par le professeur.");
        return response;
    }

    public SnapshotResponse lancer(Long examenId) {
        /* Pas de vérif créneau : le prof ouvre la session quand il veut ; les étudiants restent bloqués
         * à rejoindre tant que verifierCreneauExamenAtteint n’est pas OK (rejoindreSalleAttente). */
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "PLANIFIE", "EN_PAUSE");
        ExamenPublie exam = chargerExamen(examenId);
        state.phase = "EN_COURS";
        state.paused = false;
        state.startedAt = LocalDateTime.now();
        state.remainingMinutes = Math.max(0, exam.getDuree() == null ? state.remainingMinutes : exam.getDuree());
        List<Question> orderedLaunch = questionsOrderedForExamen(exam);
        int qi = orderedLaunch.isEmpty()
                ? 0
                : Math.max(0, Math.min(state.currentQuestionIndex, orderedLaunch.size() - 1));
        state.questionRemainingSeconds = Math.max(0, resolveIndicativeSeconds(exam, qi, state));
        persistStatut(examenId, StatutExamen.EN_COURS);
        notifierEtudiantsExamenLance(examenId, exam);
        restartQuestionTicker(examenId);
        return publishAndSnapshot(examenId);
    }

    private void notifierEtudiantsExamenLance(Long examenId, ExamenPublie exam) {
        try {
            ExamenPublie fetched = examenPublieRepository.findByIdFetchingEmailsAutorisesWeb(examenId).orElse(null);
            if (fetched == null || fetched.getEmailsAutorisesWeb() == null || fetched.getEmailsAutorisesWeb().isEmpty()) {
                return;
            }
            String profNom = exam.getProfesseur() != null ? exam.getProfesseur().getNom() : null;
            String titre = exam.getTitre();
            for (String email : fetched.getEmailsAutorisesWeb()) {
                try {
                    emailService.sendExamenLanceEmail(email, profNom, titre);
                } catch (Exception ignored) {
                }
            }
        } catch (Exception ignored) {
        }
    }

    public SnapshotResponse pause(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "EN_COURS");
        state.paused = true;
        state.phase = "EN_PAUSE";
        persistStatut(examenId, StatutExamen.EN_PAUSE);
        cancelQuestionTicker(examenId);
        return publishAndSnapshot(examenId);
    }

    public SnapshotResponse reprendre(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "EN_PAUSE");
        state.paused = false;
        state.phase = "EN_COURS";
        persistStatut(examenId, StatutExamen.EN_COURS);
        restartQuestionTicker(examenId);
        return publishAndSnapshot(examenId);
    }

    public SnapshotResponse arreter(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhaseIsNot(state, "TERMINE", "ARRETE");
        state.phase = "ARRETE";
        state.paused = false;
        state.finishedAt = LocalDateTime.now();
        state.waitingRoom.clear();
        persistStatut(examenId, StatutExamen.ANNULE);
        cancelQuestionTicker(examenId);
        return publishAndSnapshot(examenId);
    }

    public SnapshotResponse terminer(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhaseIsNot(state, "TERMINE", "ARRETE");
        state.phase = "TERMINE";
        state.paused = false;
        state.finishedAt = LocalDateTime.now();
        /* Conserver la salle d'attente : les étudiants doivent pouvoir soumettre après la fin imposée par le prof. */
        persistStatut(examenId, StatutExamen.TERMINE);
        cancelQuestionTicker(examenId);
        return publishAndSnapshot(examenId);
    }

    public SnapshotResponse questionSuivante(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "EN_COURS");
        int totalQuestions = resolveTotalQuestions(examenId);
        if (totalQuestions > 0 && state.currentQuestionIndex < totalQuestions - 1) {
            state.currentQuestionIndex++;
        }
        ExamenPublie examAfter = chargerExamen(examenId);
        state.questionRemainingSeconds = Math.max(
                0,
                resolveIndicativeSeconds(examAfter, state.currentQuestionIndex, state));
        SnapshotResponse snap = publishAndSnapshot(examenId);
        restartQuestionTicker(examenId);
        return snap;
    }

    public SnapshotResponse questionPrecedente(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "EN_COURS");
        if (state.currentQuestionIndex > 0) {
            state.currentQuestionIndex--;
        }
        ExamenPublie examPrev = chargerExamen(examenId);
        state.questionRemainingSeconds = Math.max(
                0,
                resolveIndicativeSeconds(examPrev, state.currentQuestionIndex, state));
        SnapshotResponse snap = publishAndSnapshot(examenId);
        restartQuestionTicker(examenId);
        return snap;
    }

    public SnapshotResponse allerAQuestion(Long examenId, Integer numeroQuestion) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "EN_COURS");
        int totalQuestions = resolveTotalQuestions(examenId);
        ExamenPublie exam = chargerExamen(examenId);
        if (totalQuestions <= 0) {
            state.currentQuestionIndex = 0;
            state.questionRemainingSeconds = Math.max(0, resolveIndicativeSeconds(exam, 0, state));
            SnapshotResponse snap = publishAndSnapshot(examenId);
            restartQuestionTicker(examenId);
            return snap;
        }
        if (numeroQuestion == null) {
            throw new IllegalArgumentException("Le numéro de question est obligatoire.");
        }
        int cible = Math.max(1, Math.min(numeroQuestion, totalQuestions));
        state.currentQuestionIndex = cible - 1;
        state.questionRemainingSeconds = Math.max(
                0,
                resolveIndicativeSeconds(exam, state.currentQuestionIndex, state));
        SnapshotResponse snap = publishAndSnapshot(examenId);
        restartQuestionTicker(examenId);
        return snap;
    }

    public SnapshotResponse ajusterTemps(Long examenId, Integer deltaMinutes) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhaseIsNot(state, "TERMINE", "ARRETE");
        state.remainingMinutes = Math.max(0, state.remainingMinutes + (deltaMinutes == null ? 0 : deltaMinutes));
        return publishAndSnapshot(examenId);
    }

    /**
     * Ajuste le minuteur de la question courante (+/- secondes). Ne change pas la durée par défaut des questions suivantes.
     */
    public SnapshotResponse ajusterMinuteurQuestion(Long examenId, Integer deltaSeconds) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhaseIsNot(state, "TERMINE", "ARRETE");
        int d = deltaSeconds == null ? 0 : deltaSeconds;
        state.questionRemainingSeconds = Math.max(0, state.questionRemainingSeconds + d);
        SnapshotResponse snap = publishAndSnapshot(examenId);
        restartQuestionTicker(examenId);
        return snap;
    }

    public SnapshotResponse configurerModePassage(Long examenId, String advanceMode, Integer questionDurationSeconds) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhaseIsNot(state, "TERMINE", "ARRETE");
        AdvanceMode parsedMode = parseAdvanceMode(advanceMode);
        state.advanceMode = parsedMode;
        if (questionDurationSeconds != null && questionDurationSeconds >= 5) {
            state.questionDurationSeconds = questionDurationSeconds;
        }
        /* Plus de passage automatique : le minuteur par question est toujours piloté manuellement pour changer de question. */
        return publishAndSnapshot(examenId);
    }

    public SnapshotResponse definirBareme(Long examenId, Double baremeSur20) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhaseIsNot(state, "TERMINE", "ARRETE");
        if (baremeSur20 == null || baremeSur20 <= 0.0) {
            throw new IllegalArgumentException("Le barème doit être strictement positif.");
        }
        state.baremeSur20 = baremeSur20;
        return publishAndSnapshot(examenId);
    }

    public Map<String, Object> enregistrerResultatEtudiant(Long examenId, Long etudiantId, Integer bonnesReponses, Integer totalQuestions) {
        ExamenRuntimeState state = getState(examenId);
        int total = totalQuestions == null || totalQuestions <= 0 ? 0 : totalQuestions;
        int bonnes = bonnesReponses == null || bonnesReponses < 0 ? 0 : Math.min(bonnesReponses, total);
        double brutSur20 = total == 0 ? 0.0 : (bonnes * state.baremeSur20) / total;

        NoteDraft draft = new NoteDraft(etudiantId, brutSur20, brutSur20, null, false);
        state.pendingResults.put(etudiantId, draft);

        Map<String, Object> response = new LinkedHashMap<>();
        response.put("examenId", examenId);
        response.put("etudiantId", etudiantId);
        response.put("transmisAuProfesseur", true);
        response.put("visibleEtudiant", false);
        publish(examenId, "resultats-en-attente", getResultatsEnAttente(examenId));
        return response;
    }

    public List<NoteDraft> getResultatsEnAttente(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        return new ArrayList<>(state.pendingResults.values());
    }

    public NoteDraft validerResultat(Long examenId, Long etudiantId, Double noteFinale, String remarque) {
        ExamenRuntimeState state = getState(examenId);
        NoteDraft pending = state.pendingResults.get(etudiantId);
        if (pending == null) {
            throw new IllegalArgumentException("Aucun résultat en attente pour cet étudiant.");
        }
        double validated = noteFinale != null ? noteFinale : pending.noteProposee();
        NoteDraft approved = new NoteDraft(etudiantId, pending.noteProposee(), validated, remarque, true);
        state.pendingResults.remove(etudiantId);
        state.validatedResults.put(etudiantId, approved);
        publish(examenId, "resultats-valides", getResultatsValides(examenId));
        return approved;
    }

    public List<NoteDraft> getResultatsValides(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        return new ArrayList<>(state.validatedResults.values());
    }

    public NoteDraft getResultatVisibleEtudiant(Long examenId, Long etudiantId) {
        ExamenRuntimeState state = getState(examenId);
        NoteDraft draft = state.validatedResults.get(etudiantId);
        if (draft == null) {
            throw new IllegalArgumentException("La note n'est pas encore validée.");
        }
        return draft;
    }

    public SnapshotResponse snapshot(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ExamenPublie exam = chargerExamen(examenId);
        List<Question> orderedSnap = questionsOrderedForExamen(exam);
        int totalQuestions = orderedSnap.size();
        if (totalQuestions <= 0) {
            state.currentQuestionIndex = 0;
        } else if (state.currentQuestionIndex >= totalQuestions) {
            state.currentQuestionIndex = totalQuestions - 1;
        }
        Integer idx = totalQuestions > 0 ? state.currentQuestionIndex : null;
        int indicativeSnap = totalQuestions > 0 && idx != null
                ? resolveIndicativeSeconds(exam, idx, state)
                : clampFallbackSeconds(state.questionDurationSeconds);

        /*
         * Ne pas retirer les choix (reponses) : le même snapshot est diffusé sur /topic/examen/{id}/etat pour les étudiants.
         * Les retirer faisait écraser l’état côté web à chaque tick WS — les énoncés restaient mais les options QCM disparaissaient.
         * Seuls id + contenu sont exposés (pas la correction).
         */
        Map<String, Object> questionPourSupervision = buildQuestionPayload(orderedSnap, idx);
        int reponsesPourQuestionCourante = compterReponsesPourQuestionCourante(state, orderedSnap, idx);

        return new SnapshotResponse(
                examenId,
                exam.getTitre(),
                state.phase,
                state.paused,
                state.currentQuestionIndex,
                totalQuestions,
                questionPourSupervision,
                buildPlanQuestionsLight(exam),
                state.remainingMinutes,
                state.questionRemainingSeconds,
                state.baremeSur20,
                state.waitingRoom.size(),
                state.allowedEmails.size(),
                state.pendingResults.size(),
                state.validatedResults.size(),
                state.advanceMode.name(),
                indicativeSnap,
                reponsesPourQuestionCourante
        );
    }

    /** Nombre d’étudiants présents en session ayant enregistré une réponse pour la question à l’index donné. */
    private static int compterReponsesPourQuestionCourante(
            ExamenRuntimeState state,
            List<Question> orderedQuestions,
            Integer idx) {
        if (idx == null || idx < 0 || idx >= orderedQuestions.size()) {
            return 0;
        }
        Question q = orderedQuestions.get(idx);
        Long qid = q.getId();
        if (qid == null) {
            return 0;
        }
        int n = 0;
        for (Long etudiantId : state.waitingRoom.keySet()) {
            Map<Long, Long> parQuestion = state.answersByStudent.get(etudiantId);
            if (parQuestion != null && parQuestion.containsKey(qid)) {
                n++;
            }
        }
        return n;
    }

    private void mergeEmailsFromDatabase(Long examenId) {
        examenPublieRepository.findByIdFetchingEmailsAutorisesWeb(examenId).ifPresent(ex -> {
            if (ex.getEmailsAutorisesWeb() == null || ex.getEmailsAutorisesWeb().isEmpty()) {
                return;
            }
            ExamenRuntimeState state = getState(examenId);
            state.allowedEmails.clear();
            for (String raw : ex.getEmailsAutorisesWeb()) {
                String n = normalizeEmail(raw);
                if (!n.isEmpty()) {
                    state.allowedEmails.add(n);
                }
            }
        });
    }

    private void persistStatut(Long examenId, StatutExamen statut) {
        examenPublieRepository.findById(examenId).ifPresent(ex -> {
            ex.setStatut(statut);
            examenPublieRepository.save(ex);
        });
    }

    private static String normalizeEmail(String raw) {
        if (raw == null) return "";
        return raw.trim().toLowerCase(Locale.ROOT);
    }

    private SnapshotResponse publishAndSnapshot(Long examenId) {
        SnapshotResponse snap = snapshot(examenId);
        /*
         * Même sérialisation que l’API REST : convertAndSend avec un record peut être traité par un
         * MessageConverter différent ; une Map garantit que tous les champs (dont reponsesPourQuestionCourante)
         * partent bien vers /topic/examen/{id}/etat pour la supervision temps réel.
         */
        Map<String, Object> body = objectMapper.convertValue(snap, new TypeReference<Map<String, Object>>() {});
        publish(examenId, "etat", body);
        return snap;
    }

    private void restartQuestionTicker(Long examenId) {
        cancelQuestionTicker(examenId);
        ExamenRuntimeState state = getState(examenId);
        if (!Objects.equals(state.phase, "EN_COURS") || state.paused) {
            return;
        }
        int totalQuestions = resolveTotalQuestions(examenId);
        if (totalQuestions <= 0 || state.questionRemainingSeconds <= 0) {
            return;
        }
        ScheduledFuture<?> task = scheduler.scheduleAtFixedRate(() -> tickQuestionTimer(examenId), 1, 1, TimeUnit.SECONDS);
        questionTickerTasks.put(examenId, task);
    }

    private void tickQuestionTimer(Long examenId) {
        try {
            ExamenRuntimeState state = getState(examenId);
            if (!Objects.equals(state.phase, "EN_COURS") || state.paused) {
                return;
            }
            if (state.questionRemainingSeconds <= 0) {
                cancelQuestionTicker(examenId);
                return;
            }
            state.questionRemainingSeconds--;
            publishAndSnapshot(examenId);
            if (state.questionRemainingSeconds <= 0) {
                cancelQuestionTicker(examenId);
            }
        } catch (Exception ignored) {
        }
    }

    private void cancelQuestionTicker(Long examenId) {
        ScheduledFuture<?> existing = questionTickerTasks.remove(examenId);
        if (existing != null) {
            existing.cancel(false);
        }
    }

    /** Arrête minuteurs / état mémoire pour cet examen (ex. suppression par le professeur). */
    public void evictRuntimeState(Long examenId) {
        cancelQuestionTicker(examenId);
        states.remove(examenId);
    }

    private static AdvanceMode parseAdvanceMode(String raw) {
        if (raw == null || raw.isBlank()) {
            return AdvanceMode.MANUAL;
        }
        String u = raw.trim().toUpperCase(Locale.ROOT);
        /* Ancien mode AUTO_TIMER : le passage à la question suivante est toujours manuel ; seul le minuteur par question compte. */
        if ("AUTO_TIMER".equals(u)) {
            return AdvanceMode.MANUAL;
        }
        try {
            return AdvanceMode.valueOf(u);
        } catch (IllegalArgumentException ex) {
            throw new IllegalArgumentException("Mode de passage invalide. Utilisez MANUAL.");
        }
    }

    private void publish(Long examenId, String channel, Object payload) {
        messagingTemplate.convertAndSend("/topic/examen/" + examenId + "/" + channel, payload);
    }

    private static void ensurePhase(ExamenRuntimeState state, String... expected) {
        for (String candidate : expected) {
            if (candidate.equals(state.phase)) {
                return;
            }
        }
        throw new IllegalStateException("Transition non autorisée depuis l'état " + state.phase);
    }

    private static void ensurePhaseIsNot(ExamenRuntimeState state, String... forbidden) {
        for (String candidate : forbidden) {
            if (candidate.equals(state.phase)) {
                throw new IllegalStateException("Action impossible dans l'état " + state.phase);
            }
        }
    }

    private ExamenRuntimeState getState(Long examenId) {
        return states.computeIfAbsent(examenId, id -> new ExamenRuntimeState());
    }

    private ExamenPublie chargerExamen(Long examenId) {
        return examenPublieRepository.findWithQuestionsAndProfesseurById(examenId)
                .orElseThrow(() -> new IllegalArgumentException("Examen non trouvé"));
    }

    /**
     * Ordre stable entre requêtes : {@link ExamenPublie#getQuestions()} est une association {@code ManyToMany}
     * sans colonne d’ordre ; Hibernate peut renvoyer une permutation différente à chaque chargement. Toutes les
     * opérations indexées (énoncé affiché, validation élève, compteur de réponses) doivent utiliser cette liste.
     */
    private static List<Question> questionsOrderedForExamen(ExamenPublie exam) {
        List<Question> raw = exam.getQuestions();
        if (raw == null || raw.isEmpty()) {
            return List.of();
        }
        List<Question> copy = new ArrayList<>();
        for (Question q : raw) {
            if (q != null) {
                copy.add(q);
            }
        }
        copy.sort(Comparator.comparing(Question::getId, Comparator.nullsLast(Long::compareTo)));
        return copy;
    }

    private static int clampFallbackSeconds(int configuredDefault) {
        if (configuredDefault >= 5 && configuredDefault <= 7200) {
            return configuredDefault;
        }
        return 60;
    }

    /**
     * Durée indicative pour l’index donné : valeur persistée sur la question si présente, sinon repli sur
     * {@link ExamenRuntimeState#questionDurationSeconds} (ex. API legacy / défaut).
     */
    private int resolveIndicativeSeconds(ExamenPublie exam, int questionIndex, ExamenRuntimeState state) {
        List<Question> list = questionsOrderedForExamen(exam);
        if (list.isEmpty() || questionIndex < 0 || questionIndex >= list.size()) {
            return clampFallbackSeconds(state.questionDurationSeconds);
        }
        Question q = list.get(questionIndex);
        Integer d = q.getDureeSecondesIndicative();
        if (d != null && d >= 5) {
            return Math.min(d, 7200);
        }
        return clampFallbackSeconds(state.questionDurationSeconds);
    }

    private int resolveTotalQuestions(Long examenId) {
        ExamenPublie exam = chargerExamen(examenId);
        return questionsOrderedForExamen(exam).size();
    }

    private Map<String, Object> buildQuestionPayload(List<Question> orderedQuestions, Integer idx) {
        if (idx == null || orderedQuestions.isEmpty()) {
            return null;
        }
        if (idx < 0 || idx >= orderedQuestions.size()) {
            return null;
        }
        Question question = orderedQuestions.get(idx);
        Map<String, Object> qc = new LinkedHashMap<>();
        qc.put("id", question.getId());
        qc.put("numero", idx + 1);
        qc.put("enonce", question.getEnonce());
        qc.put("type", question.getType() == null ? null : question.getType().name());
        List<Reponse> reponsesQuestion = question.getReponses();
        if (reponsesQuestion == null) {
            reponsesQuestion = List.of();
        }
        qc.put("reponses", reponsesQuestion
                .stream()
                .map(r -> {
                    Map<String, Object> rp = new LinkedHashMap<>();
                    rp.put("id", r.getId());
                    rp.put("contenu", r.getContenu());
                    return rp;
                })
                .collect(Collectors.toList()));
        return qc;
    }

    /** Plan complet pour la supervision (énoncés + type, sans les choix — ceux-ci sont sur {@link #buildQuestionPayload}). */
    private List<Map<String, Object>> buildPlanQuestionsLight(ExamenPublie exam) {
        List<Question> ordered = questionsOrderedForExamen(exam);
        if (ordered.isEmpty()) {
            return List.of();
        }
        List<Map<String, Object>> plan = new ArrayList<>();
        int i = 0;
        for (Question q : ordered) {
            if (q == null) {
                continue;
            }
            Map<String, Object> row = new LinkedHashMap<>();
            row.put("id", q.getId());
            row.put("numero", i + 1);
            row.put("enonce", q.getEnonce());
            row.put("type", q.getType() == null ? null : q.getType().name());
            plan.add(row);
            i++;
        }
        return plan;
    }

    private static final class ExamenRuntimeState {
        private String phase = "PLANIFIE";
        private boolean paused = false;
        private AdvanceMode advanceMode = AdvanceMode.MANUAL;
        /** Durée par défaut du minuteur à chaque nouvelle question (secondes). */
        private int questionDurationSeconds = 60;
        /** Décompte serveur pour la question affichée ; à 0 le temps est écoulé sans changer de question automatiquement. */
        private int questionRemainingSeconds = 0;
        private int currentQuestionIndex = 0;
        private int remainingMinutes = 0;
        private double baremeSur20 = DEFAULT_BAREME;
        private LocalDateTime startedAt;
        private LocalDateTime finishedAt;
        private final Set<String> allowedEmails = ConcurrentHashMap.newKeySet();
        private final Map<Long, StudentPresence> waitingRoom = new ConcurrentHashMap<>();
        private final Map<Long, NoteDraft> pendingResults = new ConcurrentHashMap<>();
        private final Map<Long, NoteDraft> validatedResults = new ConcurrentHashMap<>();
        private final Map<Long, Map<Long, Long>> answersByStudent = new ConcurrentHashMap<>();
        private final Set<Long> finalSubmittedStudents = ConcurrentHashMap.newKeySet();
    }

    public record StudentPresence(Long etudiantId, String email, LocalDateTime joinedAt) {
    }

    public record NoteDraft(Long etudiantId, Double noteProposee, Double noteFinale, String remarque, boolean validee) {
    }

    public record JoinRoomResponse(Long examenId, boolean joined, boolean peutCommencer, String etat) {
    }

    public record WaitingRoomResponse(Long examenId, String etat, List<StudentPresence> connectes, int nombreConnectes) {
    }

    public record ExamQuestionStateResponse(
            Long examenId,
            String etat,
            boolean enPause,
            int totalQuestions,
            Integer questionCouranteIndex,
            Map<String, Object> questionCourante,
            int tempsRestantMinutes,
            Integer tempsQuestionRestantSeconds,
            /** Durée indicatif prévue pour répondre à la question (secondes), affichée aux étudiants. */
            int questionDurationSeconds
    ) {
    }

    public record SnapshotResponse(
            Long examenId,
            String titre,
            String etat,
            boolean enPause,
            int questionCouranteIndex,
            int totalQuestions,
            Map<String, Object> questionCourante,
            List<Map<String, Object>> planQuestions,
            int tempsRestantMinutes,
            int tempsQuestionRestantSeconds,
            double baremeSur20,
            int participantsEnAttente,
            int emailsAutorises,
            int resultatsEnAttente,
            int resultatsValides,
            String advanceMode,
            int questionDurationSeconds,
            /** Parmi les étudiants présents en session, combien ont déjà répondu à la question courante. */
            int reponsesPourQuestionCourante
    ) {
    }

    private enum AdvanceMode {
        MANUAL,
        AUTO_TIMER
    }

    @PreDestroy
    void shutdownScheduler() {
        questionTickerTasks.values().forEach(task -> task.cancel(false));
        questionTickerTasks.clear();
        scheduler.shutdownNow();
    }
}
