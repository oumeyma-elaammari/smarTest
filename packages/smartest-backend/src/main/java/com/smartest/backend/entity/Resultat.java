package com.smartest.backend.entity;

import jakarta.persistence.*;
import lombok.*;

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

    @ManyToOne
    private Question question;

    @ManyToOne
    private Reponse reponse;

    private Boolean correcte;

    @ManyToOne
    private SessionExamen sessionExamen;

    @ManyToOne
    private Etudiant etudiant;

    // 🔥 NOUVEAUX CHAMPS

    private Double score;   // % ou points
    private Double note;    // note finale

    private LocalDateTime datePassage;

    private Boolean estPremiereTentative;

    private Long quizId; // null si examen
}