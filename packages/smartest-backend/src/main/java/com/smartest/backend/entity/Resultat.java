package com.smartest.backend.entity;

import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;

@Entity
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Resultat {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "question_id", nullable = false)
    private Question question;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "reponse_id", nullable = false)
    private Reponse reponse;

    private Boolean correcte;

    /** Null pour un quiz (web ou local) ; renseigné pour un passage en session d'examen. */
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "session_examen_id", nullable = true)
    private SessionExamen sessionExamen;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "etudiant_id", nullable = false)
    private Etudiant etudiant;

    // 🔥 NOUVEAUX CHAMPS

    private Double score;   // % ou points
    private Double note;    // note finale

    private LocalDateTime datePassage;

    private Boolean estPremiereTentative;

    private Long quizId; // null si examen
}