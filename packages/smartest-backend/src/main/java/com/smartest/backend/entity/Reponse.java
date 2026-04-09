package com.smartest.backend.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import lombok.*;

@Entity
@Table(name = "reponse")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Reponse {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(columnDefinition = "TEXT")
    private String contenu;

    private Boolean correcte;

    @JsonIgnore
    @ToString.Exclude
    @ManyToOne
    @JoinColumn(name = "utilisateur_id", nullable = false)
    private Utilisateur utilisateur;

    @JsonIgnore
    @ToString.Exclude
    @ManyToOne
    @JoinColumn(name = "session_examen_id", nullable = false)
    private SessionExamen sessionExamen;

    @JsonIgnore
    @ToString.Exclude
    @ManyToOne
    @JoinColumn(name = "question_id", nullable = false)
    private Question question;

    public Reponse(String contenu, Boolean correcte, Utilisateur utilisateur,
            SessionExamen sessionExamen, Question question) {
        this.contenu = contenu;
        this.correcte = correcte;
        this.utilisateur = utilisateur;
        this.sessionExamen = sessionExamen;
        this.question = question;
    }
}
