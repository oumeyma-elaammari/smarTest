package com.smartest.backend.entity;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.CascadeType;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.OneToMany;
import jakarta.persistence.Table;
import lombok.*;

@Entity
@Table(name = "session_examen")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class SessionExamen {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "date_debut")
    private LocalDateTime dateDebut;

    @Column(name = "date_fin")
    private LocalDateTime dateFin;

    private String statut;

    @JsonIgnore
    @ToString.Exclude
    @ManyToOne
    @JoinColumn(name = "examen_id", nullable = false)
    private Examen examen;

    @JsonIgnore
    @ToString.Exclude
    @OneToMany(mappedBy = "sessionExamen", cascade = CascadeType.ALL)
    private List<Reponse> reponses = new ArrayList<>();

    @JsonIgnore
    @ToString.Exclude
    @OneToMany(mappedBy = "sessionExamen", cascade = CascadeType.ALL)
    private List<Resultat> resultats = new ArrayList<>();

    public SessionExamen(LocalDateTime dateDebut, LocalDateTime dateFin, String statut, Examen examen) {
        this.dateDebut = dateDebut;
        this.dateFin = dateFin;
        this.statut = statut;
        this.examen = examen;
    }
}
