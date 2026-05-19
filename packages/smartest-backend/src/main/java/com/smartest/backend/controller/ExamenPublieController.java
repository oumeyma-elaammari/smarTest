package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ExamenPassageReponseRequest;
import com.smartest.backend.dto.request.PublicationExamenQuestionsRequest;
import com.smartest.backend.dto.request.ValiderCorrectionsDetailRequest;
import com.smartest.backend.dto.request.ValiderResultatExamenRequest;
import com.smartest.backend.dto.response.ExamenCorrectionsEtudiantResponse;
import com.smartest.backend.dto.response.ExamenEtudiantPassageResponse;
import com.smartest.backend.dto.response.ExamenPublieMetadataResponse;
import com.smartest.backend.dto.response.MessageResponse;
import com.smartest.backend.dto.response.ResultatVisibleResponse;
import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ExamenPublieRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.service.ExamenCorrectionService;
import com.smartest.backend.service.ExamenPublieService;
import com.smartest.backend.service.ExamenSupervisionService;
import com.smartest.backend.service.GroqApiKeyRegistry;
import com.smartest.backend.service.GroqRedactionRepriseService;
import com.smartest.backend.service.ExamenSupervisionService.NoteDraft;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

@RestController
@RequestMapping("/api/examens-publies")
@RequiredArgsConstructor
public class ExamenPublieController {

    private final ExamenPublieService examenPublieService;
    private final ExamenSupervisionService supervisionService;
    private final ExamenCorrectionService examenCorrectionService;
    private final GroqApiKeyRegistry groqApiKeyRegistry;
    private final GroqRedactionRepriseService groqRedactionRepriseService;
    private final ExamenPublieRepository examenPublieRepository;
    private final ProfesseurRepository professeurRepository;
    private final EtudiantRepository etudiantRepository;

    // --- Création (desktop) ---

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public ExamenPublie publier(@RequestParam Long professeurId,
                                @RequestParam String titre,
                                @RequestParam Integer duree,
                                @RequestParam String description,
                                @RequestParam String dateDebut,
                                @RequestParam String dateFin) {
        return examenPublieService.publier(
                professeurId,
                titre,
                duree,
                description,
                LocalDateTime.parse(dateDebut),
                LocalDateTime.parse(dateFin)
        );
    }

    @GetMapping
    public List<ExamenPublie> tous() {
        return examenPublieService.findAll();
    }

    @GetMapping("/disponibles")
    public List<ExamenPublie> disponibles() {
        return examenPublieService.getDisponibles();
    }

    /** Liste métadonnées pour les étudiants (sans questions). */
    @GetMapping("/mes-publications-web")
    public ResponseEntity<List<ExamenPublieMetadataResponse>> mesPublicationsWeb(
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(examenPublieService.getMesExamensPublicationWeb(userDetails.getUsername()));
    }

    /** Suppression par le professeur propriétaire (ex. depuis le bureau) : retire l'examen du web. */
    @DeleteMapping("/{id}")
    public ResponseEntity<MessageResponse> supprimerExamenPublie(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        examenPublieService.deleteExamenPublie(id, userDetails.getUsername());
        return ResponseEntity.ok(new MessageResponse("Examen supprimé avec succès", true, 200));
    }

    @GetMapping("/{id}/metadata")
    public ResponseEntity<ExamenPublieMetadataResponse> metadata(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        String email = normalize(userDetails.getUsername());
        if (professeurRepository.findByEmailIgnoreCase(email).isPresent()) {
            return ResponseEntity.ok(examenPublieService.getMetadataPourProfesseur(id, email));
        }
        return ResponseEntity.ok(examenPublieService.getMetadataPourEtudiant(id, userDetails.getUsername()));
    }

    // --- Publication emails (desktop / prof) ---

    @PostMapping("/{id}/publication-web/emails")
    public ResponseEntity<Map<String, Object>> definirEmailsPublicationWeb(
            @PathVariable Long id,
            @RequestBody List<String> emails,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        
        // Récupérer le professeurId pour appeler la méthode correcte
        String emailProf = normalize(userDetails.getUsername());
        Professeur prof = professeurRepository.findByEmailIgnoreCase(emailProf)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN, "Professeur introuvable"));
        
        examenPublieService.definirEmailsPublicationWeb(id, prof.getId(), emails);
        
        // Retourner une réponse cohérente avec l'ancienne implémentation
        Map<String, Object> response = new LinkedHashMap<>();
        response.put("examenId", id);
        response.put("nombreEmailsAutorises", emails != null ? emails.size() : 0);
        response.put("emailsAutorises", emails != null ? emails : List.of());
        return ResponseEntity.ok(response);
    }

    /** Synchronise le contenu QCM (comme le quiz web) : indispensable pour la supervision et le passage étudiant. */
    @PostMapping("/{id}/publication-web/questions")
    public ResponseEntity<MessageResponse> synchroniserQuestionsPublicationWeb(
            @PathVariable Long id,
            @Valid @RequestBody PublicationExamenQuestionsRequest body,
            @org.springframework.web.bind.annotation.RequestHeader(value = "X-Groq-Api-Key", required = false) String groqApiKey,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        if (groqApiKey != null && !groqApiKey.isBlank()) {
            groqApiKeyRegistry.registerForExamen(id, groqApiKey);
        }
        examenPublieService.synchroniserQuestionsPublicationWeb(
                id, userDetails.getUsername(), body.getQuestions());
        return ResponseEntity.ok(new MessageResponse("Questions de l'examen enregistrées sur le serveur.", true, 200));
    }

    // --- Salle d'attente & passage étudiant ---

    @PostMapping("/{id}/salle-attente/rejoindre")
    public ResponseEntity<ExamenSupervisionService.JoinRoomResponse> rejoindre(
            @PathVariable Long id,
            @RequestParam Long etudiantId,
            @RequestParam String email,
            @AuthenticationPrincipal UserDetails userDetails) {
        Etudiant etu = requireEtudiant(userDetails);
        long etuId = etu.getId();
        String emailEffectif = normalize(userDetails.getUsername());
        examenPublieService.getMetadataPourEtudiant(id, emailEffectif);
        return ResponseEntity.ok(supervisionService.rejoindreSalleAttente(id, etuId, emailEffectif));
    }

    /**
     * Liste des présents en salle d'attente : professeur propriétaire ou étudiant autorisé (même vue pour la cohérence).
     */
    @GetMapping("/{id}/salle-attente")
    public ResponseEntity<ExamenSupervisionService.WaitingRoomResponse> salleAttente(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        if (userDetails == null) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED);
        }
        String email = normalize(userDetails.getUsername());
        if (professeurRepository.findByEmailIgnoreCase(email).isPresent()) {
            assertProfOwnsExamenByEmail(id, email);
        } else {
            examenPublieService.getMetadataPourEtudiant(id, email);
        }
        supervisionService.synchroniserParticipantsVersBase(id);
        return ResponseEntity.ok(supervisionService.getSalleAttente(id));
    }

    @GetMapping("/{id}/passage/question-courante")
    public ResponseEntity<ExamenSupervisionService.ExamQuestionStateResponse> questionCourante(
            @PathVariable Long id,
            @RequestParam(required = false) Long etudiantId,
            @AuthenticationPrincipal UserDetails userDetails) {
        Etudiant etu = requireEtudiant(userDetails);
        long etuId = etu.getId();
        String email = normalize(userDetails.getUsername());
        examenPublieService.getMetadataPourEtudiant(id, email);
        return ResponseEntity.ok(supervisionService.getQuestionCouranteEtudiant(id, etuId, email));
    }

    @PostMapping(value = "/{id}/passage/reponse", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Map<String, Object>> enregistrerReponse(
            @PathVariable Long id,
            @RequestParam(required = false) Long etudiantId,
            @RequestBody ExamenPassageReponseRequest body,
            @AuthenticationPrincipal UserDetails userDetails) {
        Etudiant etu = requireEtudiant(userDetails);
        long etuId = etu.getId();
        String email = normalize(userDetails.getUsername());
        examenPublieService.getMetadataPourEtudiant(id, email);
        return ResponseEntity.ok(supervisionService.enregistrerReponseEtudiant(id, etuId, email, body));
    }

    @PostMapping("/{id}/passage/soumettre-final")
    public ResponseEntity<Map<String, Object>> soumettreFinal(
            @PathVariable Long id,
            @RequestParam(required = false) Long etudiantId,
            @AuthenticationPrincipal UserDetails userDetails) {
        Etudiant etu = requireEtudiant(userDetails);
        long etuId = etu.getId();
        examenPublieService.getMetadataPourEtudiant(id, normalize(userDetails.getUsername()));
        return ResponseEntity.ok(supervisionService.soumettreExamenEtudiant(id, etuId));
    }

    @GetMapping("/{id}/passage/resultat-visible")
    public ResponseEntity<ResultatVisibleResponse> resultatVisible(
            @PathVariable Long id,
            @RequestParam(required = false) Long etudiantId,
            @AuthenticationPrincipal UserDetails userDetails) {
        Etudiant etu = requireEtudiant(userDetails);
        long etuId = etu.getId();
        examenPublieService.getMetadataPourEtudiant(id, normalize(userDetails.getUsername()));
        return ResponseEntity.ok(supervisionService.getResultatVisibleEtudiantResponse(id, etuId));
    }

    // --- Supervision professeur ---

    /**
     * Actions simples : lancer, pause, reprendre, terminer, arreter.
     * Compatibilité web ({@code /controle/lancer}) et desktop ({@code /controle/{action}}).
     */
    @PatchMapping("/{id}/controle/{action}")
    public ResponseEntity<ExamenSupervisionService.SnapshotResponse> controleSimple(
            @PathVariable Long id,
            @PathVariable String action,
            @org.springframework.web.bind.annotation.RequestHeader(value = "X-Groq-Api-Key", required = false) String groqApiKey,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        if (groqApiKey != null && !groqApiKey.isBlank()) {
            groqApiKeyRegistry.registerForExamen(id, groqApiKey);
            groqRedactionRepriseService.relancerCorrectionsRedactionExamen(id);
        }
        String a = action == null ? "" : action.trim().toLowerCase(Locale.ROOT);
        return ResponseEntity.ok(switch (a) {
            case "lancer" -> supervisionService.lancer(id);
            case "pause" -> supervisionService.pause(id);
            case "reprendre" -> supervisionService.reprendre(id);
            case "terminer" -> supervisionService.terminer(id);
            case "arreter" -> supervisionService.arreter(id);
            default -> throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Action inconnue: " + action);
        });
    }

    @PatchMapping("/{id}/controle/question/suivante")
    public ResponseEntity<ExamenSupervisionService.SnapshotResponse> questionSuivante(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.questionSuivante(id));
    }

    @PatchMapping("/{id}/controle/question/precedente")
    public ResponseEntity<ExamenSupervisionService.SnapshotResponse> questionPrecedente(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.questionPrecedente(id));
    }

    @PatchMapping("/{id}/controle/question/aller")
    public ResponseEntity<ExamenSupervisionService.SnapshotResponse> allerAQuestion(
            @PathVariable Long id,
            @RequestParam Integer numeroQuestion,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.allerAQuestion(id, numeroQuestion));
    }

    @PatchMapping("/{id}/controle/temps")
    public ResponseEntity<ExamenSupervisionService.SnapshotResponse> ajusterTemps(
            @PathVariable Long id,
            @RequestParam Integer deltaMinutes,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.ajusterTemps(id, deltaMinutes));
    }

    @PatchMapping("/{id}/controle/minuteur-question")
    public ResponseEntity<ExamenSupervisionService.SnapshotResponse> ajusterMinuteurQuestion(
            @PathVariable Long id,
            @RequestParam Integer deltaSeconds,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.ajusterMinuteurQuestion(id, deltaSeconds));
    }

    @PatchMapping("/{id}/controle/mode-passage")
    public ResponseEntity<ExamenSupervisionService.SnapshotResponse> modePassage(
            @PathVariable Long id,
            @RequestParam String mode,
            @RequestParam(required = false) Integer questionDurationSeconds,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.configurerModePassage(id, mode, questionDurationSeconds));
    }

    @PatchMapping("/{id}/bareme")
    public ResponseEntity<ExamenSupervisionService.SnapshotResponse> definirBareme(
            @PathVariable Long id,
            @RequestParam Double baremeSur20,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.definirBareme(id, baremeSur20));
    }

    @GetMapping("/{id}/supervision/snapshot")
    public ResponseEntity<ExamenSupervisionService.SnapshotResponse> snapshot(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        supervisionService.synchroniserParticipantsVersBase(id);
        return ResponseEntity.ok(supervisionService.snapshot(id));
    }

    @GetMapping({"/{id}/supervision/etudiants-passages", "/{id}/supervision/participants-soumis"})
    public ResponseEntity<List<ExamenEtudiantPassageResponse>> etudiantsPassages(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        supervisionService.synchroniserParticipantsVersBase(id);
        List<ExamenEtudiantPassageResponse> rows = new ArrayList<>(examenCorrectionService.listerEtudiantsAyantPasse(id));
        for (ExamenEtudiantPassageResponse m : supervisionService.listerEtudiantsSoumisDepuisMemoire(id)) {
            if (m.getEtudiantId() == null) {
                continue;
            }
            boolean deja = rows.stream().anyMatch(r -> m.getEtudiantId().equals(r.getEtudiantId()));
            if (!deja) {
                rows.add(m);
            }
        }
        return ResponseEntity.ok(rows);
    }

    @GetMapping("/{id}/supervision/resultats-en-attente")
    public ResponseEntity<List<NoteDraft>> resultatsEnAttente(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.getResultatsEnAttente(id));
    }

    @PostMapping("/{id}/supervision/valider-resultat")
    public ResponseEntity<NoteDraft> validerResultat(
            @PathVariable Long id,
            @RequestBody ValiderResultatExamenRequest body,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        if (body == null || body.getEtudiantId() == null) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "etudiantId requis");
        }
        return ResponseEntity.ok(supervisionService.validerResultat(
                id, body.getEtudiantId(), body.getNoteFinale(), body.getRemarque()));
    }

    @GetMapping("/{id}/supervision/etudiants/{etudiantId}/corrections")
    public ResponseEntity<ExamenCorrectionsEtudiantResponse> correctionsEtudiant(
            @PathVariable Long id,
            @PathVariable Long etudiantId,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        double bareme = supervisionService.snapshot(id).baremeSur20();
        return ResponseEntity.ok(examenCorrectionService.getCorrectionsPourProf(id, etudiantId, bareme));
    }

    @PostMapping("/{id}/supervision/etudiants/{etudiantId}/valider-corrections-detail")
    public ResponseEntity<MessageResponse> validerCorrectionsDetail(
            @PathVariable Long id,
            @PathVariable Long etudiantId,
            @Valid @RequestBody ValiderCorrectionsDetailRequest body,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        examenCorrectionService.validerCorrectionsDetail(id, etudiantId, body);
        supervisionService.synchroniserMemoireAvecResultatPersistant(id, etudiantId);
        return ResponseEntity.ok(new MessageResponse(
                "Correction enregistrée, note validée et publiée pour l'étudiant.", true, 200));
    }

    @PostMapping("/{id}/supervision/forcer-consolidation")
    public ResponseEntity<MessageResponse> forcerConsolidation(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        supervisionService.synchroniserParticipantsVersBase(id);
        supervisionService.finaliserSoumissionsAutomatiques(id, true);
        return ResponseEntity.ok(new MessageResponse(
                "Consolidation des copies effectuée (mémoire serveur + lignes déjà en base).", true, 200));
    }

    @PostMapping("/{id}/supervision/etudiants/{etudiantId}/synchroniser-note-workbench")
    public ResponseEntity<MessageResponse> synchroniserNoteWorkbench(
            @PathVariable Long id,
            @PathVariable Long etudiantId,
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
        examenCorrectionService.marquerSynchronisationWorkbench(id, etudiantId);
        return ResponseEntity.ok(new MessageResponse("Synchronisation enregistrée ; la note est visible pour l'étudiant.", true, 200));
    }

    // --- Anciens endpoints (compat) ---

    @PatchMapping("/{id}/demarrer")
    public ResponseEntity<ExamenSupervisionService.SnapshotResponse> demarrerLegacy(@PathVariable Long id) {
        examenPublieService.demarrer(id);
        return ResponseEntity.ok(supervisionService.snapshot(id));
    }

    @PatchMapping("/{id}/terminer")
    public ResponseEntity<Void> terminerLegacy(@PathVariable Long id) {
        supervisionService.terminer(id);
        return ResponseEntity.ok().build();
    }

    private static String normalize(String email) {
        return email == null ? "" : email.trim().toLowerCase(Locale.ROOT);
    }

    private Etudiant requireEtudiant(UserDetails userDetails) {
        return etudiantRepository.findByEmailIgnoreCase(normalize(userDetails.getUsername()))
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN, "Compte étudiant requis."));
    }

    private void assertProfOwnsExamen(Long examenId, Long professeurId) {
        ExamenPublie ex = examenPublieRepository.findById(examenId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND));
        if (ex.getProfesseur() == null || !ex.getProfesseur().getId().equals(professeurId)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN,
                    "Cet examen n'appartient pas à votre compte professeur.");
        }
    }

    private void assertProfOwnsExamenByEmail(Long examenId, String emailProf) {
        Professeur prof = professeurRepository.findByEmailIgnoreCase(normalize(emailProf))
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.FORBIDDEN,
                        "Compte professeur introuvable ou identité incompatible avec cette session."));
        assertProfOwnsExamen(examenId, prof.getId());
    }
}
