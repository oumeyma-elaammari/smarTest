package com.smartest.backend.service;

import com.smartest.backend.entity.*;
import com.smartest.backend.entity.enumeration.StatutExamen;
import com.smartest.backend.repository.*;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.List;

@Service
@RequiredArgsConstructor
public class ExamenPublieService {

    private final ExamenPublieRepository examenPublieRepository;
    private final ProfesseurRepository professeurRepository;

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