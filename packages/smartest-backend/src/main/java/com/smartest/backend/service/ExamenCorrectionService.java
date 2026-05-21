package com.smartest.backend.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.config.ExamenResultatPublicationProperties;
import com.smartest.backend.dto.examen.StudentAnswerSnapshot;
import com.smartest.backend.dto.request.ValiderCorrectionsDetailRequest;
import com.smartest.backend.dto.response.ExamenCorrectionLigneResponse;
import com.smartest.backend.dto.response.ExamenCorrectionsEtudiantResponse;
import com.smartest.backend.dto.response.ExamenEtudiantPassageResponse;
import com.smartest.backend.dto.response.NoteAttenteResponse;
import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.entity.ExamenCorrectionLigne;
import com.smartest.backend.entity.ExamenPassageResultat;
import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ExamenCorrectionLigneRepository;
import com.smartest.backend.repository.ExamenPassageResultatRepository;
import com.smartest.backend.repository.ExamenPublieRepository;
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
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
@Slf4j
public class ExamenCorrectionService {

    private static final String MARQUEUR_REPONSE_MODELE = "[Réponse modèle]";
    static final String LIBELLE_PAS_DE_REPONSE = "Pas de réponse donnée";
    /** Barème cases à cocher : chaque proposition (A–D) vaut noteQuestion / 4. */
    static final int CHECKBOX_NB_CHOIX = 4;

    private final ExamenCorrectionLigneRepository ligneRepository;
    private final ExamenPassageResultatRepository passageRepository;
    private final ExamenPublieRepository examenPublieRepository;
    private final EtudiantRepository etudiantRepository;
    private final ObjectMapper objectMapper;
    private final GroqRedactionCorrectionWorker groqWorker;
    private final ExamenResultatPublicationProperties publicationProperties;
    private final EmailService emailService;

    public static String extraireReponseModele(Question q) {
        if (q == null) {
            return "";
        }
        String ex = q.getExplication();
        if (ex == null) {
            return "";
        }
        int idx = ex.indexOf(MARQUEUR_REPONSE_MODELE);
        if (idx >= 0) {
            return ex.substring(idx + MARQUEUR_REPONSE_MODELE.length()).trim();
        }
        return ex.trim();
    }

    private static double pointsUniformesParQuestion(double bareme, int nombreQuestions) {
        if (nombreQuestions <= 0) {
            return 0.0;
        }
        return bareme / nombreQuestions;
    }

    /**
     * Points max pour une question : barème défini à la création de l'examen, sinon répartition uniforme (anciens examens).
     */
    static double pointsPourQuestion(Question q, double baremeSur20, int nombreQuestions) {
        if (q != null && q.getBaremePoints() != null && q.getBaremePoints() > 0) {
            return q.getBaremePoints();
        }
        return pointsUniformesParQuestion(baremeSur20, nombreQuestions);
    }

    private static double sommeBaremeQuestions(List<Question> questions, double baremeSur20) {
        return sommeBaremeQuestionsPublic(questions, baremeSur20);
    }

    public static double sommeBaremeQuestionsPublic(List<Question> questions, double baremeSur20) {
        if (questions == null || questions.isEmpty()) {
            return baremeSur20;
        }
        double sum = questions.stream()
                .mapToDouble(q -> pointsPourQuestion(q, baremeSur20, questions.size()))
                .sum();
        return sum > 0 ? sum : baremeSur20;
    }

    private static boolean estRedaction(TypeQuestion t) {
        return t == TypeQuestion.REDACTION || t == TypeQuestion.REPONSE_COURTE;
    }

    private static double scoreQcmOuVf(Question q, StudentAnswerSnapshot ans, double pts) {
        if (ans.singleReponseId() == null || q.getReponses() == null) {
            return 0.0;
        }
        boolean ok = q.getReponses().stream()
                .anyMatch(r -> r.getId() != null
                        && r.getId().equals(ans.singleReponseId())
                        && Boolean.TRUE.equals(r.getCorrecte()));
        return ok ? pts : 0.0;
    }

    /**
     * Cases à cocher : la note de la question est répartie sur 4 choix (note/4 par proposition).
     * Bonne réponse cochée → +note/4 ; mauvaise réponse non cochée → +note/4 ; sinon 0 pour ce choix.
     */
    static double scoreCasesACocher(Question q, StudentAnswerSnapshot ans, double pts) {
        if (q.getReponses() == null || q.getReponses().isEmpty() || pts <= 0) {
            return 0.0;
        }
        double ptsParChoix = pts / CHECKBOX_NB_CHOIX;
        Set<Long> sel = new HashSet<>(ans.reponseIdsMultiOrEmpty());
        double score = 0.0;
        for (Reponse r : q.getReponses()) {
            if (r.getId() == null) {
                continue;
            }
            boolean correcte = Boolean.TRUE.equals(r.getCorrecte());
            boolean cochee = sel.contains(r.getId());
            if (correcte && cochee) {
                score += ptsParChoix;
            } else if (!correcte && !cochee) {
                score += ptsParChoix;
            }
        }
        score = Math.round(score * 100.0) / 100.0;
        return Math.min(pts, score);
    }

    private static double arrondirNote(double v) {
        return Math.round(v * 100.0) / 100.0;
    }

    private static void verifierNoteDansBarème(double note, double max, String libelle) {
        if (Double.isNaN(note) || Double.isInfinite(note)) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, libelle + " : valeur numérique invalide.");
        }
        if (note < 0) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, libelle + " : la note ne peut pas être négative.");
        }
        if (note > max + 1e-6) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST,
                    libelle + " : la note ne peut pas dépasser " + arrondirNote(max) + " point(s).");
        }
    }

    @Transactional
    public double enregistrerCorrectionsApresSoumission(
            ExamenPublie examen,
            Long etudiantId,
            List<Question> questionsOrdonnees,
            Map<Long, StudentAnswerSnapshot> reponsesParQuestionId,
            double baremeSur20) {
        Long examenId = examen.getId();
        ligneRepository.deleteByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId);

        Etudiant etuRef = etudiantRepository.getReferenceById(etudiantId);

        int n = Math.max(1, questionsOrdonnees.size());
        double baremeEffectif = sommeBaremeQuestions(questionsOrdonnees, baremeSur20);

        for (Question q : questionsOrdonnees) {
            if (q == null || q.getId() == null) {
                continue;
            }
            StudentAnswerSnapshot snap = reponsesParQuestionId.get(q.getId());
            TypeQuestion t = q.getType() != null ? q.getType() : TypeQuestion.QCM;
            double ptsQuestion = pointsPourQuestion(q, baremeSur20, n);

            ExamenCorrectionLigne ligne = new ExamenCorrectionLigne();
            ligne.setExamenPublie(examen);
            ligne.setEtudiant(etuRef);
            ligne.setQuestion(q);
            ligne.setReponseJson(serialiserReponse(snap));
            ligne.setCorrigeParIa(estRedaction(t));
            ligne.setDateCorrection(LocalDateTime.now());

            if (snap == null || !snap.hasReponse()) {
                ligne.setScorePartiel(0.0);
                ligne.setGroqStatut(null);
                ligneRepository.save(ligne);
                continue;
            }

            if (estRedaction(t)) {
                ligne.setScorePartiel(null);
                ligne.setGroqStatut("PENDING");
                ligneRepository.save(ligne);
                groqWorker.corrigerRedactionMaintenant(examenId, etudiantId, q.getId(), ptsQuestion);
                continue;
            }

            double score;
            if (t == TypeQuestion.CASES_A_COCHER) {
                score = scoreCasesACocher(q, snap, ptsQuestion);
            } else {
                /* QCM, VRAI_FAUX, défaut */
                score = scoreQcmOuVf(q, snap, ptsQuestion);
            }
            ligne.setScorePartiel(score);
            ligne.setGroqStatut(null);
            ligneRepository.save(ligne);
        }

        /* Note initiale : rédactions comptent 0 tant que Groq n'a pas répondu */
        ExamenPassageResultat passage = passageRepository
                .findByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId)
                .orElseGet(ExamenPassageResultat::new);
        passage.setExamenPublie(examen);
        passage.setEtudiant(etuRef);
        passage.setBaremeReference(baremeEffectif);
        passage.setValideeParProf(false);
        passage.setNoteFinale(null);
        passage.setNoteVisibleEtudiant(false);
        passage.setDatePublicationNoteEtudiant(null);
        passage.setDateSynchronisationWorkbench(null);
        LocalDateTime now = LocalDateTime.now();
        if (passage.getDateCreation() == null) {
            passage.setDateCreation(now);
        }
        passage.setDateMiseAJour(now);
        passage.setNoteProposee(recalculerSommePartielle(examenId, etudiantId));
        passageRepository.save(passage);

        return passage.getNoteProposee();
    }

    /** Crée une ligne passage brouillon dès l'inscription en salle (visible dans Sessions actives). */
    @Transactional
    public void inscrirePassageBrouillonSiAbsent(ExamenPublie examen, Long etudiantId, double baremeSur20) {
        if (examen == null || examen.getId() == null || etudiantId == null || etudiantId <= 0) {
            return;
        }
        Long examenId = examen.getId();
        if (passageRepository.findByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId).isPresent()) {
            return;
        }
        Etudiant etuRef = etudiantRepository.getReferenceById(etudiantId);
        mettreAJourPassageBrouillon(examen, etuRef, etudiantId, examenId, baremeSur20);
        log.info("Passage brouillon inscrit examen={} etudiant={}", examenId, etudiantId);
    }

    /**
     * Sauvegarde immédiate d'une réponse (visible dans Sessions actives même avant « Terminer »).
     */
    @Transactional
    public void persisterReponseUnitaire(
            ExamenPublie examen,
            Long etudiantId,
            Question question,
            StudentAnswerSnapshot snap,
            double baremeSur20,
            int totalQuestions) {
        if (examen == null || examen.getId() == null) {
            throw new IllegalArgumentException("Examen invalide pour la persistance.");
        }
        if (etudiantId == null || etudiantId <= 0) {
            throw new IllegalArgumentException("Étudiant invalide pour la persistance.");
        }
        if (question == null || question.getId() == null) {
            throw new IllegalArgumentException("Question invalide pour la persistance.");
        }
        Long examenId = examen.getId();
        Long questionId = question.getId();
        Etudiant etuRef = etudiantRepository.getReferenceById(etudiantId);
        int n = Math.max(1, totalQuestions);
        double ptsQuestion = pointsPourQuestion(question, baremeSur20, n);
        TypeQuestion t = question.getType() != null ? question.getType() : TypeQuestion.QCM;

        ExamenCorrectionLigne ligne = ligneRepository
                .findByExamenPublie_IdAndEtudiant_IdAndQuestion_Id(examenId, etudiantId, questionId)
                .orElseGet(() -> {
                    ExamenCorrectionLigne created = new ExamenCorrectionLigne();
                    created.setExamenPublie(examen);
                    created.setEtudiant(etuRef);
                    created.setQuestion(question);
                    return created;
                });

        ligne.setReponseJson(serialiserReponse(snap));
        ligne.setCorrigeParIa(estRedaction(t));
        ligne.setDateCorrection(LocalDateTime.now());

        if (snap == null || !snap.hasReponse()) {
            ligne.setScorePartiel(0.0);
            ligne.setGroqStatut(null);
        } else if (estRedaction(t)) {
            ligne.setScorePartiel(null);
            ligne.setGroqStatut("PENDING");
            ligneRepository.saveAndFlush(ligne);
            groqWorker.corrigerRedactionMaintenant(examenId, etudiantId, questionId, ptsQuestion);
            mettreAJourPassageBrouillon(examen, etuRef, etudiantId, examenId, baremeSur20);
            log.info("Réponse rédaction persistée examen={} etudiant={} question={}", examenId, etudiantId, questionId);
            return;
        } else {
            double score = t == TypeQuestion.CASES_A_COCHER
                    ? scoreCasesACocher(question, snap, ptsQuestion)
                    : scoreQcmOuVf(question, snap, ptsQuestion);
            ligne.setScorePartiel(score);
            ligne.setGroqStatut(null);
        }
        ligneRepository.saveAndFlush(ligne);
        mettreAJourPassageBrouillon(examen, etuRef, etudiantId, examenId, baremeSur20);
        log.info("Réponse persistée examen={} etudiant={} question={} score={}",
                examenId, etudiantId, questionId, ligne.getScorePartiel());
    }

    @Transactional(readOnly = true)
    public List<Long> listerEtudiantIdsAvecReponsesEnBase(Long examenPublieId) {
        return ligneRepository.findDistinctEtudiantIdsByExamenPublie_Id(examenPublieId);
    }

    @Transactional(readOnly = true)
    public boolean aDejaReponduQuestion(Long examenId, Long etudiantId, Long questionId) {
        return lireReponsePersistee(examenId, etudiantId, questionId).isPresent();
    }

    @Transactional(readOnly = true)
    public Optional<StudentAnswerSnapshot> lireReponsePersistee(Long examenId, Long etudiantId, Long questionId) {
        if (examenId == null || etudiantId == null || etudiantId <= 0 || questionId == null) {
            return Optional.empty();
        }
        return ligneRepository
                .findByExamenPublie_IdAndEtudiant_IdAndQuestion_Id(examenId, etudiantId, questionId)
                .map(ExamenCorrectionLigne::getReponseJson)
                .flatMap(this::deserialiserReponseSnapshot);
    }

    private Optional<StudentAnswerSnapshot> deserialiserReponseSnapshot(String json) {
        if (json == null || json.isBlank()) {
            return Optional.empty();
        }
        try {
            JsonNode n = objectMapper.readTree(json);
            Long single = n.hasNonNull("singleReponseId") ? n.get("singleReponseId").asLong() : null;
            Set<Long> multi = new LinkedHashSet<>();
            if (n.has("reponseIds") && n.get("reponseIds").isArray()) {
                for (JsonNode id : n.get("reponseIds")) {
                    if (id.isNumber()) {
                        multi.add(id.asLong());
                    }
                }
            }
            String texte = n.has("texte") && n.get("texte").isTextual() ? n.get("texte").asText() : null;
            StudentAnswerSnapshot snap = new StudentAnswerSnapshot(null, single, multi, texte);
            return snap.hasReponse() ? Optional.of(snap) : Optional.empty();
        } catch (Exception e) {
            return Optional.empty();
        }
    }

    /** Recalcule la note brouillon à partir des lignes déjà en base (repli si la mémoire runtime est vide). */
    @Transactional
    public void consoliderPassageDepuisLignesExistantes(ExamenPublie examen, Long etudiantId, double baremeSur20) {
        if (examen == null || examen.getId() == null || etudiantId == null || etudiantId <= 0) {
            return;
        }
        Long examenId = examen.getId();
        if (!ligneRepository.existsByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId)) {
            return;
        }
        Etudiant etuRef = etudiantRepository.getReferenceById(etudiantId);
        mettreAJourPassageBrouillon(examen, etuRef, etudiantId, examenId, baremeSur20);
    }

    private void mettreAJourPassageBrouillon(
            ExamenPublie examen, Etudiant etuRef, Long etudiantId, Long examenId, double baremeSur20) {
        ExamenPassageResultat passage = passageRepository
                .findByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId)
                .orElseGet(ExamenPassageResultat::new);
        passage.setExamenPublie(examen);
        passage.setEtudiant(etuRef);
        passage.setBaremeReference(baremeSur20);
        if (!passage.isValideeParProf()) {
            passage.setNoteFinale(null);
            passage.setNoteVisibleEtudiant(false);
        }
        LocalDateTime now = LocalDateTime.now();
        if (passage.getDateCreation() == null) {
            passage.setDateCreation(now);
        }
        passage.setDateMiseAJour(now);
        passage.setNoteProposee(recalculerSommePartielle(examenId, etudiantId));
        passageRepository.saveAndFlush(passage);
    }

    private double recalculerSommePartielle(Long examenId, Long etudiantId) {
        return ligneRepository.findByExamenPublie_IdAndEtudiant_IdOrderByQuestion_IdAsc(examenId, etudiantId).stream()
                .mapToDouble(l -> l.getScorePartiel() != null ? l.getScorePartiel() : 0.0)
                .sum();
    }

    private String serialiserReponse(StudentAnswerSnapshot snap) {
        try {
            Map<String, Object> m = new LinkedHashMap<>();
            if (snap == null) {
                return objectMapper.writeValueAsString(m);
            }
            if (snap.singleReponseId() != null) {
                m.put("singleReponseId", snap.singleReponseId());
            }
            if (snap.reponseIdsMultiOrEmpty() != null && !snap.reponseIdsMultiOrEmpty().isEmpty()) {
                m.put("reponseIds", new ArrayList<>(snap.reponseIdsMultiOrEmpty()));
            }
            if (snap.texteLibre() != null && !snap.texteLibre().isBlank()) {
                m.put("texte", snap.texteLibre());
            }
            return objectMapper.writeValueAsString(m);
        } catch (Exception e) {
            return "{}";
        }
    }

    @Transactional(readOnly = true)
    public ExamenCorrectionsEtudiantResponse getCorrectionsPourProf(Long examenId, Long etudiantId, double baremeReference) {
        List<ExamenCorrectionLigne> lignes =
                ligneRepository.findByExamenPublie_IdAndEtudiant_IdOrderByQuestion_IdAsc(examenId, etudiantId);
        if (lignes.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Aucune correction enregistrée pour cet étudiant.");
        }
        ExamenPassageResultat passage = passageRepository
                .findByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Résultat d'examen introuvable."));

        List<Question> questionsExamen = examenPublieRepository.findByIdWithQuestions(examenId)
                .map(ExamenPublie::getQuestions)
                .filter(qs -> qs != null && !qs.isEmpty())
                .orElseGet(() -> lignes.stream()
                        .map(ExamenCorrectionLigne::getQuestion)
                        .filter(q -> q != null && q.getId() != null)
                        .toList());

        int n = Math.max(1, questionsExamen.size());
        double baremeRef = passage.getBaremeReference() > 0
                ? passage.getBaremeReference()
                : sommeBaremeQuestions(questionsExamen, baremeReference);

        Map<Long, ExamenCorrectionLigne> ligneParQuestionId = lignes.stream()
                .filter(l -> l.getQuestion() != null && l.getQuestion().getId() != null)
                .collect(Collectors.toMap(l -> l.getQuestion().getId(), l -> l, (a, b) -> a, LinkedHashMap::new));

        List<ExamenCorrectionLigneResponse> dto = new ArrayList<>();
        for (Question q : questionsExamen) {
            if (q == null || q.getId() == null) {
                continue;
            }
            double ptsMax = pointsPourQuestion(q, baremeRef, n);
            ExamenCorrectionLigne ligne = ligneParQuestionId.get(q.getId());
            if (ligne == null) {
                dto.add(new ExamenCorrectionLigneResponse(
                        q.getId(),
                        q.getEnonce(),
                        q.getType() != null ? q.getType().name() : null,
                        LIBELLE_PAS_DE_REPONSE,
                        libelleReponseCorrecte(q),
                        "{}",
                        0.0,
                        null,
                        ptsMax,
                        estRedaction(q.getType() != null ? q.getType() : TypeQuestion.QCM),
                        null));
            } else {
                dto.add(new ExamenCorrectionLigneResponse(
                        q.getId(),
                        q.getEnonce(),
                        q.getType() != null ? q.getType().name() : null,
                        libelleReponseEtudiant(q, ligne.getReponseJson()),
                        libelleReponseCorrecte(q),
                        ligne.getReponseJson(),
                        ligne.getScorePartiel(),
                        ligne.getNoteFinaleLigne(),
                        ptsMax,
                        ligne.isCorrigeParIa(),
                        ligne.getGroqStatut()));
            }
        }

        ExamenCorrectionsEtudiantResponse out = new ExamenCorrectionsEtudiantResponse();
        out.setEtudiantId(etudiantId);
        out.setNoteProposee(passage.getNoteProposee());
        out.setBaremeReference(baremeRef);
        out.setValideeParProf(passage.isValideeParProf());
        out.setLignes(dto);
        return out;
    }

    private String libelleReponseCorrecte(Question q) {
        if (q == null) {
            return "";
        }
        TypeQuestion t = q.getType() != null ? q.getType() : TypeQuestion.QCM;
        if (estRedaction(t)) {
            return extraireReponseModele(q);
        }
        if (q.getReponses() == null || q.getReponses().isEmpty()) {
            return "";
        }
        return q.getReponses().stream()
                .filter(r -> Boolean.TRUE.equals(r.getCorrecte()))
                .map(Reponse::getContenu)
                .filter(c -> c != null && !c.isBlank())
                .collect(Collectors.joining(", "));
    }

    private String libelleReponseEtudiant(Question q, String json) {
        if (json == null || json.isBlank() || "{}".equals(json.trim())) {
            return LIBELLE_PAS_DE_REPONSE;
        }
        TypeQuestion t = q.getType() != null ? q.getType() : TypeQuestion.QCM;
        try {
            JsonNode root = objectMapper.readTree(json);
            if (estRedaction(t)) {
                String tx = root.path("texte").asText("");
                if (tx.isBlank()) {
                    return LIBELLE_PAS_DE_REPONSE;
                }
                return tx.length() > 800 ? tx.substring(0, 800) + "…" : tx;
            }
            if (t == TypeQuestion.CASES_A_COCHER && root.has("reponseIds") && root.get("reponseIds").isArray()) {
                List<String> labels = new ArrayList<>();
                for (JsonNode idNode : root.get("reponseIds")) {
                    if (!idNode.canConvertToLong()) {
                        continue;
                    }
                    long rid = idNode.longValue();
                    String lib = q.getReponses() == null ? ("#" + rid) : q.getReponses().stream()
                            .filter(r -> r.getId() != null && r.getId().longValue() == rid)
                            .map(Reponse::getContenu)
                            .findFirst()
                            .orElse("#" + rid);
                    labels.add(lib);
                }
                return labels.isEmpty() ? LIBELLE_PAS_DE_REPONSE : String.join(", ", labels);
            }
            if (root.has("singleReponseId") && root.get("singleReponseId").canConvertToLong()) {
                long rid = root.get("singleReponseId").longValue();
                return q.getReponses() == null ? ("#" + rid) : q.getReponses().stream()
                        .filter(r -> r.getId() != null && r.getId().longValue() == rid)
                        .map(Reponse::getContenu)
                        .findFirst()
                        .orElse("#" + rid);
            }
            return LIBELLE_PAS_DE_REPONSE;
        } catch (Exception ignored) {
        }
        return LIBELLE_PAS_DE_REPONSE;
    }

    @Transactional
    public void validerCorrectionsDetail(
            Long examenId,
            Long etudiantId,
            ValiderCorrectionsDetailRequest body) {
        if (body == null || body.getNotesFinales() == null || body.getNotesFinales().isEmpty()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "notesFinales requis");
        }
        List<ExamenCorrectionLigne> lignes =
                ligneRepository.findByExamenPublie_IdAndEtudiant_IdOrderByQuestion_IdAsc(examenId, etudiantId);
        if (lignes.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Aucune ligne de correction.");
        }
        Map<Long, Double> parsed = new LinkedHashMap<>();
        for (Map.Entry<String, Double> e : body.getNotesFinales().entrySet()) {
            try {
                long qid = Long.parseLong(e.getKey().trim());
                if (e.getValue() != null) {
                    parsed.put(qid, e.getValue());
                }
            } catch (NumberFormatException ex) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Clé question invalide: " + e.getKey());
            }
        }
        ExamenPassageResultat passage = passageRepository
                .findByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Résultat introuvable."));
        double baremeMax = passage.getBaremeReference() > 0 ? passage.getBaremeReference() : 20.0;
        int nbQuestions = Math.max(1, lignes.size());

        for (ExamenCorrectionLigne ligne : lignes) {
            Question q = ligne.getQuestion();
            Long qid = q != null ? q.getId() : null;
            if (qid == null) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Question invalide dans la correction.");
            }
            Double v = parsed.get(qid);
            if (v == null) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Note manquante pour la question " + qid);
            }
            double ptsMax = pointsPourQuestion(q, baremeMax, nbQuestions);
            verifierNoteDansBarème(v, ptsMax, "Question " + qid);
            ligne.setNoteFinaleLigne(arrondirNote(v));
            ligneRepository.save(ligne);
        }
        double total = body.getNoteTotale() != null
                ? body.getNoteTotale()
                : lignes.stream().mapToDouble(l -> l.getNoteFinaleLigne() != null ? l.getNoteFinaleLigne() : 0.0).sum();
        verifierNoteDansBarème(total, baremeMax, "Note totale de l'examen");
        total = arrondirNote(total);

        passage.setNoteFinale(total);
        passage.setRemarque(body.getRemarque());
        passage.setValideeParProf(true);
        passage.setDateValidation(LocalDateTime.now());
        passage.setDateMiseAJour(LocalDateTime.now());
        apresValidationPublication(passage);
        passageRepository.save(passage);
        publierNoteApresValidation(examenId, etudiantId, passage);
        if (passage.isNoteVisibleEtudiant()) {
            planifierEmailNoteValideeEtudiant(examenId, etudiantId);
        }
    }

    private void publierNoteApresValidation(Long examenId, Long etudiantId, ExamenPassageResultat passage) {
        if (!publicationProperties.isPublierNoteImmediatementApresValidation()) {
            return;
        }
        if (!passage.isNoteVisibleEtudiant()) {
            passage.setNoteVisibleEtudiant(true);
            passage.setDatePublicationNoteEtudiant(LocalDateTime.now());
            passage.setDateSynchronisationWorkbench(LocalDateTime.now());
            passage.setDateMiseAJour(LocalDateTime.now());
            passageRepository.save(passage);
        }
    }

    @Transactional
    public void validerNoteLegacySeule(
            Long examenId,
            Long etudiantId,
            double noteFinale,
            String remarque) {
        List<ExamenCorrectionLigne> lignes =
                ligneRepository.findByExamenPublie_IdAndEtudiant_IdOrderByQuestion_IdAsc(examenId, etudiantId);
        ExamenPassageResultat passage = passageRepository
                .findByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId)
                .orElse(null);
        if (lignes.isEmpty() || passage == null) {
            return;
        }
        double sumPartielle = lignes.stream()
                .mapToDouble(l -> l.getScorePartiel() != null ? l.getScorePartiel() : 0.0)
                .sum();
        if (sumPartielle <= 0.0001) {
            double part = noteFinale / lignes.size();
            for (ExamenCorrectionLigne ligne : lignes) {
                ligne.setNoteFinaleLigne(part);
                ligneRepository.save(ligne);
            }
        } else {
            for (ExamenCorrectionLigne ligne : lignes) {
                double sp = ligne.getScorePartiel() != null ? ligne.getScorePartiel() : 0.0;
                double ligneFinale = noteFinale * (sp / sumPartielle);
                ligne.setNoteFinaleLigne(ligneFinale);
                ligneRepository.save(ligne);
            }
        }
        passage.setNoteFinale(noteFinale);
        passage.setRemarque(remarque);
        passage.setValideeParProf(true);
        passage.setDateValidation(LocalDateTime.now());
        passage.setDateMiseAJour(LocalDateTime.now());
        apresValidationPublication(passage);
        passageRepository.save(passage);
        publierNoteApresValidation(examenId, etudiantId, passage);
        if (passage.isNoteVisibleEtudiant()) {
            planifierEmailNoteValideeEtudiant(examenId, etudiantId);
        }
    }

    private void apresValidationPublication(ExamenPassageResultat passage) {
        if (publicationProperties.isPublierNoteImmediatementApresValidation()) {
            passage.setNoteVisibleEtudiant(true);
            passage.setDatePublicationNoteEtudiant(LocalDateTime.now());
            passage.setDateSynchronisationWorkbench(LocalDateTime.now());
        }
    }

    @Transactional
    public void marquerSynchronisationWorkbench(Long examenId, Long etudiantId) {
        ExamenPassageResultat passage = passageRepository
                .findByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Résultat introuvable."));
        if (!passage.isValideeParProf()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Validez d'abord la correction côté professeur.");
        }
        boolean etaitDejaVisible = passage.isNoteVisibleEtudiant();
        passage.setDateSynchronisationWorkbench(LocalDateTime.now());
        passage.setNoteVisibleEtudiant(true);
        passage.setDatePublicationNoteEtudiant(
                passage.getDatePublicationNoteEtudiant() != null
                        ? passage.getDatePublicationNoteEtudiant()
                        : LocalDateTime.now());
        passage.setDateMiseAJour(LocalDateTime.now());
        passageRepository.save(passage);
        if (!etaitDejaVisible) {
            planifierEmailNoteValideeEtudiant(examenId, etudiantId);
        }
    }

    /** Envoi après commit : la validation ne doit pas échouer si la messagerie est indisponible. */
    private void planifierEmailNoteValideeEtudiant(Long examenId, Long etudiantId) {
        Runnable envoi = () -> envoyerEmailNoteValideeEtudiant(examenId, etudiantId);
        if (TransactionSynchronizationManager.isSynchronizationActive()) {
            TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
                @Override
                public void afterCommit() {
                    envoi.run();
                }
            });
        } else {
            envoi.run();
        }
    }

    private void envoyerEmailNoteValideeEtudiant(Long examenId, Long etudiantId) {
        try {
            ExamenPassageResultat passage = passageRepository
                    .findByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId)
                    .orElse(null);
            if (passage == null || !passage.isNoteVisibleEtudiant()) {
                return;
            }
            Etudiant etu = passage.getEtudiant();
            if (etu == null || etu.getEmail() == null || etu.getEmail().isBlank()) {
                etu = etudiantRepository.findById(etudiantId).orElse(null);
            }
            if (etu == null || etu.getEmail() == null || etu.getEmail().isBlank()) {
                log.warn("Email note examen non envoyé : étudiant {} sans adresse", etudiantId);
                return;
            }
            ExamenPublie exam = examenPublieRepository.findWithQuestionsAndProfesseurById(examenId).orElse(null);
            if (exam == null) {
                return;
            }
            String profNom = exam.getProfesseur() != null && exam.getProfesseur().getNom() != null
                    ? exam.getProfesseur().getNom()
                    : null;
            String titre = exam.getTitre();
            double note = passage.getNoteFinale() != null ? passage.getNoteFinale() : 0.0;
            double bareme = passage.getBaremeReference() > 0 ? passage.getBaremeReference() : 20.0;
            emailService.sendExamenNoteValideeEmail(
                    etu.getEmail().trim(),
                    profNom,
                    titre,
                    note,
                    bareme);
            log.info("Email note examen envoyé à {} pour examen {}", etu.getEmail(), examenId);
        } catch (Exception ex) {
            log.warn("Email note examen non envoyé examen={} etudiant={} : {}",
                    examenId, etudiantId, ex.getMessage());
        }
    }

    @Transactional(readOnly = true)
    public Optional<Double> lireNoteProposeeBrouillon(Long examenId, Long etudiantId) {
        return passageRepository.findByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId)
                .filter(p -> !p.isValideeParProf())
                .map(ExamenPassageResultat::getNoteProposee);
    }

    @Transactional(readOnly = true)
    public List<ExamenEtudiantPassageResponse> listerEtudiantsAyantPasse(Long examenPublieId) {
        Map<Long, ExamenEtudiantPassageResponse> map = new LinkedHashMap<>();

        for (ExamenPassageResultat p : passageRepository.findByExamenPublie_IdWithEtudiantOrderByEmail(examenPublieId)) {
            Etudiant e = p.getEtudiant();
            if (e == null || e.getId() == null) {
                continue;
            }
            map.put(e.getId(), toPassageResponse(e, p.getNoteProposee(), p.getNoteFinale(), p.isValideeParProf()));
        }

        for (Long etudiantId : ligneRepository.findDistinctEtudiantIdsByExamenPublie_Id(examenPublieId)) {
            if (etudiantId == null || map.containsKey(etudiantId)) {
                continue;
            }
            etudiantRepository.findById(etudiantId).ifPresent(e ->
                    map.put(etudiantId, toPassageResponse(e, null, null, false)));
        }

        return new ArrayList<>(map.values());
    }

    private static ExamenEtudiantPassageResponse toPassageResponse(
            Etudiant e,
            Double noteProposee,
            Double noteFinale,
            boolean valideeParProf) {
        return new ExamenEtudiantPassageResponse(
                e.getId(),
                e.getEmail() != null ? e.getEmail() : "",
                e.getNom() != null ? e.getNom() : "",
                noteProposee,
                noteFinale,
                valideeParProf);
    }

    @Transactional(readOnly = true)
    public List<NoteAttenteResponse> listerBrouillonsDepuisBase(Long examenPublieId) {
        return passageRepository.findByExamenPublie_IdAndValideeParProfIsFalse(examenPublieId).stream()
                .map(p -> new NoteAttenteResponse(p.getEtudiant().getId(), p.getNoteProposee()))
                .toList();
    }

    @Transactional(readOnly = true)
    public boolean existeCorrectionsPourEtudiant(Long examenPublieId, Long etudiantId) {
        return ligneRepository.existsByExamenPublie_IdAndEtudiant_Id(examenPublieId, etudiantId);
    }

    @Transactional(readOnly = true)
    public boolean aPassageEnregistre(Long examenPublieId, Long etudiantId) {
        return passageRepository.findByExamenPublie_IdAndEtudiant_Id(examenPublieId, etudiantId).isPresent();
    }

    @Transactional(readOnly = true)
    public Optional<Etudiant> trouverEtudiantParId(Long etudiantId) {
        if (etudiantId == null || etudiantId <= 0) {
            return Optional.empty();
        }
        return etudiantRepository.findById(etudiantId);
    }

    /** Réponses déjà en base (reprise après redémarrage serveur ou consolidation forcée). */
    @Transactional(readOnly = true)
    public Map<Long, StudentAnswerSnapshot> exporterReponsesPersistees(Long examenId, Long etudiantId) {
        if (examenId == null || etudiantId == null || etudiantId <= 0) {
            return Map.of();
        }
        Map<Long, StudentAnswerSnapshot> out = new LinkedHashMap<>();
        for (ExamenCorrectionLigne ligne :
                ligneRepository.findByExamenPublie_IdAndEtudiant_IdOrderByQuestion_IdAsc(examenId, etudiantId)) {
            if (ligne.getQuestion() == null || ligne.getQuestion().getId() == null) {
                continue;
            }
            Long qid = ligne.getQuestion().getId();
            deserialiserReponseSnapshot(ligne.getReponseJson())
                    .filter(StudentAnswerSnapshot::hasReponse)
                    .ifPresent(snap -> out.put(qid, new StudentAnswerSnapshot(
                            qid, snap.singleReponseId(), snap.reponseIdsMultiOrEmpty(), snap.texteLibre())));
        }
        return out;
    }

    @Transactional(readOnly = true)
    public boolean noteVisiblePourEtudiant(Long examenId, Long etudiantId) {
        return passageRepository
                .findByExamenPublie_IdAndEtudiant_IdAndNoteVisibleEtudiantIsTrue(examenId, etudiantId)
                .isPresent();
    }

    @Transactional(readOnly = true)
    public ExamenPassageResultat getPassageExamenEtudiant(Long examenId, Long etudiantId) {
        return passageRepository.findByExamenPublie_IdAndEtudiant_Id(examenId, etudiantId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Résultat introuvable."));
    }

    @Transactional(readOnly = true)
    public Optional<ExamenPassageResultat> trouverResultatVisible(Long examenId, Long etudiantId) {
        return passageRepository.findByExamenPublie_IdAndEtudiant_IdAndNoteVisibleEtudiantIsTrue(examenId, etudiantId);
    }

    @Transactional(readOnly = true)
    public ExamenPassageResultat getResultatVisible(Long examenId, Long etudiantId) {
        return trouverResultatVisible(examenId, etudiantId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Note non publiée."));
    }
}
