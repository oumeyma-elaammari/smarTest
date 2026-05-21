package com.smartest.backend.integration;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.enumeration.StatutExamen;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ExamenPublieRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.security.JwtUtil;
import com.smartest.backend.service.EmailService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.webmvc.test.autoconfigure.AutoConfigureMockMvc;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;

/**
 * Base pour tests d'intégration : contexte Spring complet, H2 in-memory (profil {@code test}),
 * aucune base MySQL ni Flyway.
 */
@SpringBootTest
@ActiveProfiles("test")
@AutoConfigureMockMvc
@Transactional
public abstract class BaseIntegrationTest {

    @Autowired
    protected MockMvc mockMvc;

    @Autowired
    protected ObjectMapper objectMapper;

    @Autowired
    protected JwtUtil jwtUtil;

    @Autowired
    protected PasswordEncoder passwordEncoder;

    @Autowired
    protected ProfesseurRepository professeurRepository;

    @Autowired
    protected EtudiantRepository etudiantRepository;

    @Autowired
    protected ExamenPublieRepository examenPublieRepository;

    @MockitoBean
    protected EmailService emailService;

    protected Professeur saveProfesseurVerified(String email, String rawPassword, String nom) {
        Professeur p = new Professeur();
        p.setEmail(email);
        p.setNom(nom);
        p.setPassword(passwordEncoder.encode(rawPassword));
        p.setEmailVerifie(true);
        return professeurRepository.save(p);
    }

    protected Etudiant saveEtudiantVerified(String email, String rawPassword, String nom) {
        Etudiant e = new Etudiant();
        e.setEmail(email);
        e.setNom(nom);
        e.setPassword(passwordEncoder.encode(rawPassword));
        e.setEmailVerifie(true);
        return etudiantRepository.save(e);
    }

    protected ExamenPublie saveExamenPublie(Professeur prof) {
        ExamenPublie ex = new ExamenPublie();
        ex.setTitre("Examen test");
        ex.setDuree(60);
        ex.setDescription("desc");
        ex.setProfesseur(prof);
        ex.setStatut(StatutExamen.PLANIFIE);
        LocalDateTime now = LocalDateTime.now();
        ex.setDateDebut(now.minusDays(1));
        ex.setDateFin(now.plusDays(1));
        ex.setDateCreation(now);
        return examenPublieRepository.save(ex);
    }

    protected String bearerProf(Professeur p) {
        return "Bearer " + jwtUtil.generateToken(p.getEmail(), "PROFESSEUR", p.getId());
    }

    protected String bearerEtudiant(Etudiant e) {
        return "Bearer " + jwtUtil.generateToken(e.getEmail(), "ETUDIANT", e.getId());
    }
}
