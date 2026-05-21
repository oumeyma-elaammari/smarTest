package com.smartest.backend.entity;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;

@Entity
@Table(name = "examen_passage_resultat",
        uniqueConstraints = @UniqueConstraint(name = "uk_epr_examen_etudiant", columnNames = {"examen_publie_id", "etudiant_id"}))
@Getter
@Setter
@NoArgsConstructor
public class ExamenPassageResultat {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "examen_publie_id", nullable = false)
    private ExamenPublie examenPublie;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "etudiant_id", nullable = false)
    private Etudiant etudiant;

    @Column(name = "note_proposee", nullable = false)
    private double noteProposee;

    @Column(name = "note_finale")
    private Double noteFinale;

    @Column(name = "bareme_reference", nullable = false)
    private double baremeReference = 20.0;

    @Column(name = "remarque", columnDefinition = "TEXT")
    private String remarque;

    @Column(name = "validee_par_prof", nullable = false)
    private boolean valideeParProf;

    @Column(name = "date_validation")
    private LocalDateTime dateValidation;

    @Column(name = "note_visible_etudiant", nullable = false)
    private boolean noteVisibleEtudiant;

    @Column(name = "date_publication_note_etudiant")
    private LocalDateTime datePublicationNoteEtudiant;

    @Column(name = "date_synchronisation_workbench")
    private LocalDateTime dateSynchronisationWorkbench;

    @Column(name = "date_creation", nullable = false)
    private LocalDateTime dateCreation = LocalDateTime.now();

    @Column(name = "date_mise_a_jour")
    private LocalDateTime dateMiseAJour;
}
