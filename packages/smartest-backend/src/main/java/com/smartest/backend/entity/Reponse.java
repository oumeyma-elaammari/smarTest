package com.smartest.backend.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import lombok.*;

@Entity
@Table(name = "reponse")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class Reponse {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(columnDefinition = "TEXT")
    private String contenu;

    private Boolean correcte;

    // ❌ SUPPRIMÉ (très important)
    // private Etudiant etudiant;
    // private SessionExamen sessionExamen;


    /** Null pour les propositions QCM « modèle » liées à une question ; renseigné pour une réponse d’étudiant en session. */
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "etudiant_id", nullable = true)
    private Etudiant etudiant;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "question_id", nullable = false)
    private Question question;

    /** Null pour les propositions QCM ; renseigné en contexte d’examen supervisé. */
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "session_examen_id", nullable = true)
    private SessionExamen sessionExamen;
}