package com.smartest.backend.controller;

import com.smartest.backend.dto.request.PublicationExamenQuestionsRequest;
import com.smartest.backend.dto.request.ValiderResultatExamenRequest;
import com.smartest.backend.dto.response.ExamenPublieMetadataResponse;
import com.smartest.backend.dto.response.MessageResponse;
import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ExamenPublieRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.service.ExamenPublieService;
import com.smartest.backend.service.ExamenSupervisionService;
import com.smartest.backend.service.ExamenSupervisionService.NoteDraft;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;

import java.time.LocalDateTime;
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
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
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
        if (!etu.getId().equals(etudiantId)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Identité étudiant incohérente.");
        }
        examenPublieService.getMetadataPourEtudiant(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.rejoindreSalleAttente(id, etudiantId, email));
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
        return ResponseEntity.ok(supervisionService.getSalleAttente(id));
    }

    @GetMapping("/{id}/passage/question-courante")
    public ResponseEntity<ExamenSupervisionService.ExamQuestionStateResponse> questionCourante(
            @PathVariable Long id,
            @RequestParam Long etudiantId,
            @AuthenticationPrincipal UserDetails userDetails) {
        Etudiant etu = requireEtudiant(userDetails);
        if (!etu.getId().equals(etudiantId)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN);
        }
        examenPublieService.getMetadataPourEtudiant(id, userDetails.getUsername());
        return ResponseEntity.ok(
                supervisionService.getQuestionCouranteEtudiant(id, etudiantId, userDetails.getUsername()));
    }

    @PostMapping("/{id}/passage/reponse")
    public ResponseEntity<Map<String, Object>> enregistrerReponse(
            @PathVariable Long id,
            @RequestParam Long etudiantId,
            @RequestParam Long questionId,
            @RequestParam Long reponseId,
            @AuthenticationPrincipal UserDetails userDetails) {
        Etudiant etu = requireEtudiant(userDetails);
        if (!etu.getId().equals(etudiantId)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN);
        }
        examenPublieService.getMetadataPourEtudiant(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.enregistrerReponseEtudiant(id, etudiantId, questionId, reponseId));
    }

    @PostMapping("/{id}/passage/soumettre-final")
    public ResponseEntity<Map<String, Object>> soumettreFinal(
            @PathVariable Long id,
            @RequestParam Long etudiantId,
            @AuthenticationPrincipal UserDetails userDetails) {
        Etudiant etu = requireEtudiant(userDetails);
        if (!etu.getId().equals(etudiantId)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN);
        }
        examenPublieService.getMetadataPourEtudiant(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.soumettreExamenEtudiant(id, etudiantId));
    }

    @GetMapping("/{id}/passage/resultat-visible")
    public ResponseEntity<NoteDraft> resultatVisible(
            @PathVariable Long id,
            @RequestParam Long etudiantId,
            @AuthenticationPrincipal UserDetails userDetails) {
        Etudiant etu = requireEtudiant(userDetails);
        if (!etu.getId().equals(etudiantId)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN);
        }
        examenPublieService.getMetadataPourEtudiant(id, userDetails.getUsername());
        return ResponseEntity.ok(supervisionService.getResultatVisibleEtudiant(id, etudiantId));
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
            @AuthenticationPrincipal UserDetails userDetails) {
        assertProfOwnsExamenByEmail(id, userDetails.getUsername());
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
        return ResponseEntity.ok(supervisionService.snapshot(id));
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

    // --- Anciens endpoints (compat) ---

    @PatchMapping("/{id}/demarrer")
    public ExamenPublie demarrer(@PathVariable Long id) {
        return examenPublieService.demarrer(id);
    }

    @PatchMapping("/{id}/terminer")
    public ExamenPublie terminer(@PathVariable Long id) {
        return examenPublieService.terminer(id);
    }

    private static String normalize(String email) {
        return email == null ? "" : email.trim().toLowerCase(Locale.ROOT);
    }

    private Etudiant requireEtudiant(UserDetails userDetails) {
        return etudiantRepository.findByEmail(normalize(userDetails.getUsername()))
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
