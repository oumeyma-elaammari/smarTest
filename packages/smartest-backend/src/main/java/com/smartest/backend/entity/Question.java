package com.smartest.backend.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.*;

import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "question")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Question {


    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, columnDefinition = "TEXT")
    private String enonce;

    private String type;

    private String difficulte;

    @JsonIgnore
    @ToString.Exclude
    @ManyToOne
    @JoinColumn(name = "professeur_id", nullable = false)
    private Professeur professeur;

    @JsonIgnore
    @ToString.Exclude
    @ManyToOne
    @JoinColumn(name = "cours_id")
    private Cours cours;

    @JsonIgnore
    @ToString.Exclude
    @ManyToMany(mappedBy = "questions")
    private List<Quiz> quizs = new ArrayList<>();

    @JsonIgnore
    @ToString.Exclude
    @ManyToMany(mappedBy = "questions")
    private List<Examen> examens = new ArrayList<>();

    @JsonIgnore
    @ToString.Exclude
    @OneToMany(mappedBy = "question", cascade = CascadeType.ALL)
    private List<Reponse> reponses = new ArrayList<>();

    public Question(String enonce, String type, String difficulte, Professeur professeur) {
        this.enonce = enonce;
        this.type = type;
        this.difficulte = difficulte;
        this.professeur = professeur;
    }
}
