package com.smartest.backend.entity;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;

@Entity
@Table(name = "examen_correction_ligne",
        uniqueConstraints = @UniqueConstraint(name = "uk_ecl_examen_etu_q", columnNames = {"examen_publie_id", "etudiant_id", "question_id"}))
@Getter
@Setter
@NoArgsConstructor
public class ExamenCorrectionLigne {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "examen_publie_id", nullable = false)
    private ExamenPublie examenPublie;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "etudiant_id", nullable = false)
    private Etudiant etudiant;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "question_id", nullable = false)
    private Question question;

    @Column(name = "reponse_json", nullable = false, columnDefinition = "TEXT")
    private String reponseJson;

    @Column(name = "score_partiel")
    private Double scorePartiel;

    @Column(name = "note_finale_ligne")
    private Double noteFinaleLigne;

    @Column(name = "corrige_par_ia", nullable = false)
    private boolean corrigeParIa;

    @Column(name = "groq_statut", length = 20)
    private String groqStatut;

    @Column(name = "date_correction", nullable = false)
    private LocalDateTime dateCorrection = LocalDateTime.now();
}
