package com.smartest.backend.entity;

import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

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

    @ManyToOne
    @JoinColumn(name = "etudiant_id")  // ← Vérifiez ce nom
    private Etudiant etudiant;          // ← Au lieu de "utilisateur"

    @ManyToOne
    @JoinColumn(name = "session_examen_id")
    private SessionExamen sessionExamen;

    @ManyToOne
    @JoinColumn(name = "question_id")
    private Question question;
}