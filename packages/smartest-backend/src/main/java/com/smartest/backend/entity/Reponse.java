package com.smartest.backend.entity;

import jakarta.persistence.*;
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


    @ManyToOne
    @JoinColumn(name = "etudiant_id")
    private Etudiant etudiant;  // ← rajouter ceci

    @ManyToOne
    @JoinColumn(name = "question_id")
    private Question question;

    @ManyToOne
    @JoinColumn(name = "session_examen_id")
    private SessionExamen sessionExamen; // ← manquant
}