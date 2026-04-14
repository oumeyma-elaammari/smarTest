package com.smartest.backend.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.*;

import java.time.LocalDateTime;

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
    @JoinColumn(name = "etudiant_id", nullable = false)
    private Etudiant etudiant;

    @JsonIgnore
    @ToString.Exclude
    @ManyToOne
    @JoinColumn(name = "session_examen_id", nullable = false)
    private SessionExamen sessionExamen;

    @Column(name = "date_obtention")
    private LocalDateTime dateObtention;

    public Resultat(Float note, Float score, Etudiant etudiant, SessionExamen sessionExamen) {
        this.note = note;
        this.score = score;
        this.etudiant = etudiant;
        this.sessionExamen = sessionExamen;
    }
}
