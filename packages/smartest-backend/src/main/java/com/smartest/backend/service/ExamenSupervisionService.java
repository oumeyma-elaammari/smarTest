package com.smartest.backend.service;

import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.entity.enumeration.StatutExamen;
import com.smartest.backend.repository.ExamenPublieRepository;
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
    private final Map<Long, ExamenRuntimeState> states = new ConcurrentHashMap<>();
    private final Map<Long, ScheduledFuture<?>> timerTasks = new ConcurrentHashMap<>();
    private final ScheduledExecutorService scheduler = Executors.newSingleThreadScheduledExecutor();

    public ExamenSupervisionService(SimpMessagingTemplate messagingTemplate,
                                    ExamenPublieRepository examenPublieRepository) {
        this.messagingTemplate = messagingTemplate;
        this.examenPublieRepository = examenPublieRepository;
    }

    /** Bloque rejoindre / contrôle tant que l'heure de début (créneau) n'est pas atteinte côté serveur. */
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
        return response;
    }

    @Transactional
    public Map<String, Object> definirEmailsAutorises(Long examenId, List<String> emails) {
        ExamenPublie examEntity = examenPublieRepository.findById(examenId)
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

    public ExamQuestionStateResponse getQuestionCouranteEtudiant(Long examenId, Long etudiantId) {
        mergeEmailsFromDatabase(examenId);
        ExamenRuntimeState state = getState(examenId);
        if (etudiantId == null || etudiantId <= 0) {
            throw new IllegalArgumentException("Étudiant invalide.");
        }
        if (!state.waitingRoom.containsKey(etudiantId)) {
            /*
             * Avant inscription à la salle d'attente : l'étudiant autorisé peut consulter l'état
             * (PLANIFIE, fin de session…) sans contenu de question. Dès que la session est EN_COURS
             * ou en pause, il doit avoir rejoint la salle pour recevoir les questions.
             */
            if ("EN_COURS".equals(state.phase) || "EN_PAUSE".equals(state.phase)) {
                throw new IllegalArgumentException(
                        "Rejoignez la salle d'attente sur la page de l'examen pour participer.");
            }
            ExamenPublie examSansRoom = chargerExamen(examenId);
            int totalSansRoom = examSansRoom.getQuestions() == null ? 0 : examSansRoom.getQuestions().size();
            return new ExamQuestionStateResponse(
                    examenId,
                    state.phase,
                    state.paused,
                    totalSansRoom,
                    null,
                    null,
                    state.remainingMinutes
            );
        }

        ExamenPublie exam = chargerExamen(examenId);
        int totalQuestions = exam.getQuestions() == null ? 0 : exam.getQuestions().size();
        Integer idx = null;
        if (totalQuestions > 0) {
            idx = Math.max(0, Math.min(state.currentQuestionIndex, totalQuestions - 1));
        }

        boolean montrerContenuQuestions =
                "EN_COURS".equals(state.phase) || "EN_PAUSE".equals(state.phase);
        Map<String, Object> questionPayload =
                montrerContenuQuestions ? buildQuestionPayload(exam, idx) : null;

        return new ExamQuestionStateResponse(
                examenId,
                state.phase,
                state.paused,
                totalQuestions,
                idx,
                questionPayload,
                state.remainingMinutes
        );
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
        int totalQuestions = exam.getQuestions() == null ? 0 : exam.getQuestions().size();
        if (totalQuestions <= 0) {
            throw new IllegalStateException("Aucune question disponible pour cet examen.");
        }
        int currentIdx = Math.max(0, Math.min(state.currentQuestionIndex, totalQuestions - 1));
        Question currentQuestion = exam.getQuestions().get(currentIdx);
        if (!currentQuestion.getId().equals(questionId)) {
            throw new IllegalStateException("Vous ne pouvez répondre qu'à la question active.");
        }

        Reponse choix = currentQuestion.getReponses()
                .stream()
                .filter(r -> r.getId() != null && r.getId().equals(reponseId))
                .findFirst()
                .orElseThrow(() -> new IllegalArgumentException("Réponse invalide pour la question active."));

        state.answersByStudent
                .computeIfAbsent(etudiantId, __ -> new ConcurrentHashMap<>())
                .put(questionId, choix.getId());

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
        int totalQuestions = exam.getQuestions() == null ? 0 : exam.getQuestions().size();
        Map<Long, Long> answers = state.answersByStudent.getOrDefault(etudiantId, new ConcurrentHashMap<>());

        int bonnes = 0;
        for (Question q : exam.getQuestions()) {
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
        verifierCreneauExamenAtteint(examenId);
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "PLANIFIE", "EN_PAUSE");
        ExamenPublie exam = chargerExamen(examenId);
        state.phase = "EN_COURS";
        state.paused = false;
        state.startedAt = LocalDateTime.now();
        state.remainingMinutes = Math.max(0, exam.getDuree() == null ? state.remainingMinutes : exam.getDuree());
        persistStatut(examenId, StatutExamen.EN_COURS);
        restartAutoAdvanceIfNeeded(examenId, state);
        return publishAndSnapshot(examenId);
    }

    public SnapshotResponse pause(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "EN_COURS");
        state.paused = true;
        state.phase = "EN_PAUSE";
        persistStatut(examenId, StatutExamen.EN_PAUSE);
        cancelTimer(examenId);
        return publishAndSnapshot(examenId);
    }

    public SnapshotResponse reprendre(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "EN_PAUSE");
        state.paused = false;
        state.phase = "EN_COURS";
        persistStatut(examenId, StatutExamen.EN_COURS);
        restartAutoAdvanceIfNeeded(examenId, state);
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
        cancelTimer(examenId);
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
        cancelTimer(examenId);
        return publishAndSnapshot(examenId);
    }

    public SnapshotResponse questionSuivante(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "EN_COURS");
        int totalQuestions = resolveTotalQuestions(examenId);
        if (totalQuestions > 0 && state.currentQuestionIndex < totalQuestions - 1) {
            state.currentQuestionIndex++;
        }
        SnapshotResponse snap = publishAndSnapshot(examenId);
        restartAutoAdvanceIfNeeded(examenId, state);
        return snap;
    }

    public SnapshotResponse questionPrecedente(Long examenId) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "EN_COURS");
        if (state.currentQuestionIndex > 0) {
            state.currentQuestionIndex--;
        }
        SnapshotResponse snap = publishAndSnapshot(examenId);
        restartAutoAdvanceIfNeeded(examenId, state);
        return snap;
    }

    public SnapshotResponse allerAQuestion(Long examenId, Integer numeroQuestion) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhase(state, "EN_COURS");
        int totalQuestions = resolveTotalQuestions(examenId);
        if (totalQuestions <= 0) {
            state.currentQuestionIndex = 0;
            SnapshotResponse snap = publishAndSnapshot(examenId);
            restartAutoAdvanceIfNeeded(examenId, state);
            return snap;
        }
        if (numeroQuestion == null) {
            throw new IllegalArgumentException("Le numéro de question est obligatoire.");
        }
        int cible = Math.max(1, Math.min(numeroQuestion, totalQuestions));
        state.currentQuestionIndex = cible - 1;
        SnapshotResponse snap = publishAndSnapshot(examenId);
        restartAutoAdvanceIfNeeded(examenId, state);
        return snap;
    }

    public SnapshotResponse ajusterTemps(Long examenId, Integer deltaMinutes) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhaseIsNot(state, "TERMINE", "ARRETE");
        state.remainingMinutes = Math.max(0, state.remainingMinutes + (deltaMinutes == null ? 0 : deltaMinutes));
        return publishAndSnapshot(examenId);
    }

    public SnapshotResponse configurerModePassage(Long examenId, String advanceMode, Integer questionDurationSeconds) {
        ExamenRuntimeState state = getState(examenId);
        ensurePhaseIsNot(state, "TERMINE", "ARRETE");
        AdvanceMode parsedMode = parseAdvanceMode(advanceMode);
        state.advanceMode = parsedMode;
        if (parsedMode == AdvanceMode.AUTO_TIMER) {
            if (questionDurationSeconds == null || questionDurationSeconds < 5) {
                throw new IllegalArgumentException("La durée par question doit être >= 5 secondes en mode AUTO_TIMER.");
            }
            state.questionDurationSeconds = questionDurationSeconds;
        } else if (questionDurationSeconds != null && questionDurationSeconds >= 5) {
            state.questionDurationSeconds = questionDurationSeconds;
        }
        restartAutoAdvanceIfNeeded(examenId, state);
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
        int totalQuestions = exam.getQuestions() == null ? 0 : exam.getQuestions().size();
        if (totalQuestions <= 0) {
            state.currentQuestionIndex = 0;
        } else if (state.currentQuestionIndex >= totalQuestions) {
            state.currentQuestionIndex = totalQuestions - 1;
        }
        Integer idx = totalQuestions > 0 ? state.currentQuestionIndex : null;

        return new SnapshotResponse(
                examenId,
                state.phase,
                state.paused,
                state.currentQuestionIndex,
                totalQuestions,
                buildQuestionPayload(exam, idx),
                state.remainingMinutes,
                state.baremeSur20,
                state.waitingRoom.size(),
                state.allowedEmails.size(),
                state.pendingResults.size(),
                state.validatedResults.size(),
                state.advanceMode.name(),
                state.questionDurationSeconds
        );
    }

    private void mergeEmailsFromDatabase(Long examenId) {
        examenPublieRepository.findById(examenId).ifPresent(ex -> {
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
        publish(examenId, "etat", snap);
        return snap;
    }

    private void restartAutoAdvanceIfNeeded(Long examenId, ExamenRuntimeState state) {
        cancelTimer(examenId);
        if (state.advanceMode != AdvanceMode.AUTO_TIMER) {
            return;
        }
        if (!Objects.equals(state.phase, "EN_COURS") || state.paused) {
            return;
        }
        int totalQuestions = resolveTotalQuestions(examenId);
        if (totalQuestions <= 0 || state.currentQuestionIndex >= totalQuestions - 1) {
            return;
        }
        int everySeconds = Math.max(5, state.questionDurationSeconds);
        ScheduledFuture<?> task = scheduler.schedule(() -> safeAutoAdvance(examenId), everySeconds, TimeUnit.SECONDS);
        timerTasks.put(examenId, task);
    }

    private void safeAutoAdvance(Long examenId) {
        try {
            ExamenRuntimeState state = getState(examenId);
            if (!Objects.equals(state.phase, "EN_COURS") || state.paused || state.advanceMode != AdvanceMode.AUTO_TIMER) {
                return;
            }
            int totalQuestions = resolveTotalQuestions(examenId);
            if (totalQuestions <= 0) {
                return;
            }
            if (state.currentQuestionIndex < totalQuestions - 1) {
                state.currentQuestionIndex++;
                publishAndSnapshot(examenId);
                restartAutoAdvanceIfNeeded(examenId, state);
            }
        } catch (Exception ignored) {
        }
    }

    private void cancelTimer(Long examenId) {
        ScheduledFuture<?> existing = timerTasks.remove(examenId);
        if (existing != null) {
            existing.cancel(false);
        }
    }

    private static AdvanceMode parseAdvanceMode(String raw) {
        if (raw == null || raw.isBlank()) {
            throw new IllegalArgumentException("Le mode de passage est obligatoire.");
        }
        try {
            return AdvanceMode.valueOf(raw.trim().toUpperCase(Locale.ROOT));
        } catch (IllegalArgumentException ex) {
            throw new IllegalArgumentException("Mode de passage invalide. Utilisez MANUAL ou AUTO_TIMER.");
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

    private int resolveTotalQuestions(Long examenId) {
        ExamenPublie exam = chargerExamen(examenId);
        return exam.getQuestions() == null ? 0 : exam.getQuestions().size();
    }

    private Map<String, Object> buildQuestionPayload(ExamenPublie exam, Integer idx) {
        if (idx == null || exam.getQuestions() == null || exam.getQuestions().isEmpty()) {
            return null;
        }
        if (idx < 0 || idx >= exam.getQuestions().size()) {
            return null;
        }
        Question question = exam.getQuestions().get(idx);
        Map<String, Object> qc = new LinkedHashMap<>();
        qc.put("id", question.getId());
        qc.put("numero", idx + 1);
        qc.put("enonce", question.getEnonce());
        qc.put("type", question.getType() == null ? null : question.getType().name());
        qc.put("reponses", question.getReponses()
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

    private static final class ExamenRuntimeState {
        private String phase = "PLANIFIE";
        private boolean paused = false;
        private AdvanceMode advanceMode = AdvanceMode.MANUAL;
        private int questionDurationSeconds = 30;
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
            int tempsRestantMinutes
    ) {
    }

    public record SnapshotResponse(
            Long examenId,
            String etat,
            boolean enPause,
            int questionCouranteIndex,
            int totalQuestions,
            Map<String, Object> questionCourante,
            int tempsRestantMinutes,
            double baremeSur20,
            int participantsEnAttente,
            int emailsAutorises,
            int resultatsEnAttente,
            int resultatsValides,
            String advanceMode,
            int questionDurationSeconds
    ) {
    }

    private enum AdvanceMode {
        MANUAL,
        AUTO_TIMER
    }

    @PreDestroy
    void shutdownScheduler() {
        timerTasks.values().forEach(task -> task.cancel(false));
        timerTasks.clear();
        scheduler.shutdownNow();
    }
}
