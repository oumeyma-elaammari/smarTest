package com.smartest.backend.service;

import com.smartest.backend.entity.Resultat;
import com.smartest.backend.repository.ResultatRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
@RequiredArgsConstructor
public class ResultatService {

    private final ResultatRepository resultatRepository;

    public List<Resultat> getAll() {
        return resultatRepository.findAll();
    }

    public List<Resultat> getByEtudiant(Long etudiantId) {
        return resultatRepository.findByEtudiantId(etudiantId);
    }

    public List<Resultat> getBySession(Long sessionId) {
        return resultatRepository.findBySessionExamenId(sessionId);
    }

    /**
     * 🔹 Score global étudiant (TOUT mélangé)
     */
    public double calculerScoreQuiz(Long etudiantId) {

        List<Resultat> resultats = resultatRepository
                .findByEtudiantIdAndSessionExamenIsNull(etudiantId);

        int total = resultats.size();
        int correct = 0;

        for (Resultat r : resultats) {
            if (Boolean.TRUE.equals(r.getCorrecte())) {
                correct++;
            }
        }

        return total == 0 ? 0 : (double) correct / total * 100;
    }
    /**
     * 🔹 Score EXAMEN uniquement
     */
    public double calculerScoreSession(Long sessionId) {

        List<Resultat> resultats = resultatRepository.findBySessionExamenId(sessionId);

        return calcul(resultats);
    }

    /**
     * 🔹 Méthode factorisée
     */
    private double calcul(List<Resultat> resultats) {

        int total = resultats.size();
        int correct = 0;

        for (Resultat r : resultats) {
            if (Boolean.TRUE.equals(r.getCorrecte())) {
                correct++;
            }
        }

        return total == 0 ? 0 : (double) correct / total * 100;
    }

    public void delete(Long id) {
        resultatRepository.deleteById(id);
    }

    public List<Resultat> getHistoriqueExamen(Long etudiantId, Long sessionId) {
        return resultatRepository.findByEtudiantIdAndSessionExamenId(etudiantId, sessionId);
    }
}