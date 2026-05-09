package com.smartest.backend.service;

import com.smartest.backend.dto.response.ExamenPublieMetadataResponse;
import com.smartest.backend.entity.*;
import com.smartest.backend.entity.enumeration.StatutExamen;
import com.smartest.backend.repository.*;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Locale;
import java.util.Objects;

@Service
@RequiredArgsConstructor
public class ExamenPublieService {

    private final ExamenPublieRepository examenPublieRepository;
    private final ProfesseurRepository professeurRepository;

    private static final double BAREME_DEFAUT_WEB = 20.0;

    @Transactional(readOnly = true)
    public List<ExamenPublieMetadataResponse> getMesExamensPublicationWeb(String emailEtudiant) {
        if (emailEtudiant == null || emailEtudiant.isBlank()) {
            return List.of();
        }
        String email = emailEtudiant.trim().toLowerCase(Locale.ROOT);
        return examenPublieRepository.findAutorisesPourEmail(email).stream()
                .map(this::toMetadata)
                .toList();
    }

    @Transactional(readOnly = true)
    public ExamenPublieMetadataResponse getMetadataPourEtudiant(Long examenId, String emailEtudiant) {
        ExamenPublie ex = examenPublieRepository.findById(examenId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Examen introuvable"));
        verifierEmailAutorisePourExamen(ex, emailEtudiant);
        return toMetadata(ex);
    }

    /**
     * Met à jour les emails web ; utilisé à la publication depuis le desktop.
     */
    @Transactional
    public void definirEmailsPublicationWeb(Long examenId, Long professeurId, List<String> emailsBruts) {
        ExamenPublie ex = examenPublieRepository.findById(examenId)
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
        int totalQuestions = ex.getQuestions() == null ? 0 : ex.getQuestions().size();
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
                .orElseThrow(() -> new RuntimeException("Professeur non trouvé"));

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

    public List<ExamenPublie> getDisponibles() {
        LocalDateTime now = LocalDateTime.now();
        return examenPublieRepository.findByStatutAndDateDebutBeforeAndDateFinAfter(
                StatutExamen.EN_COURS, now, now);
    }

    public ExamenPublie demarrer(Long id) {
        ExamenPublie exam = examenPublieRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Examen non trouvé"));

        exam.setStatut(StatutExamen.EN_COURS);
        return examenPublieRepository.save(exam);
    }

    public ExamenPublie terminer(Long id) {
        ExamenPublie exam = examenPublieRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Examen non trouvé"));

        exam.setStatut(StatutExamen.TERMINE);
        return examenPublieRepository.save(exam);
    }
}