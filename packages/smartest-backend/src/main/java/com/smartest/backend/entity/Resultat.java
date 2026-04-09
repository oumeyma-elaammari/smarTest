package com.smartest.backend.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import lombok.*;

@Entity
@Table(name = "resultat")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Resultat {


    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private Float note;

    private Float score;

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

    public Resultat(Float note, Float score, Utilisateur utilisateur, SessionExamen sessionExamen) {
        this.note = note;
        this.score = score;
        this.utilisateur = utilisateur;
        this.sessionExamen = sessionExamen;
    }
}
