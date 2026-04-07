package com.smartest.backend.entity;

import java.util.ArrayList;
import java.util.List;

import jakarta.persistence.CascadeType;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.JoinTable;
import jakarta.persistence.ManyToMany;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.OneToMany;
import jakarta.persistence.Table;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Entity
@Table(name = "examen")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class Examen {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, length = 200)
    private String titre;

    private Integer duree;

    @ManyToOne
    @JoinColumn(name = "professeur_id", nullable = false)
    private Professeur professeur;

    @ManyToOne
    @JoinColumn(name = "cours_id")
    private Cours cours;

    @ManyToMany
    @JoinTable(
            name = "examen_question",
            joinColumns = @JoinColumn(name = "examen_id"),
            inverseJoinColumns = @JoinColumn(name = "question_id")
    )
    private List<Question> questions = new ArrayList<>();

    @OneToMany(mappedBy = "examen", cascade = CascadeType.ALL)
    private List<SessionExamen> sessions = new ArrayList<>();

    public Examen(String titre, Integer duree, Professeur professeur) {
        this.titre = titre;
        this.duree = duree;
        this.professeur = professeur;
    }
}
