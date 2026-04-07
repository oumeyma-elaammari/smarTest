package com.smartest.backend.entity;

import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "question")
@Data
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

    @ManyToOne
    @JoinColumn(name = "professeur_id", nullable = false)
    private Professeur professeur;

    @ManyToOne
    @JoinColumn(name = "cours_id")
    private Cours cours;

    @ManyToMany(mappedBy = "questions")
    private List<Quiz> quizs = new ArrayList<>();

    @ManyToMany(mappedBy = "questions")
    private List<Examen> examens = new ArrayList<>();

    @OneToMany(mappedBy = "question", cascade = CascadeType.ALL)
    private List<Reponse> reponses = new ArrayList<>();

    public Question(String enonce, String type, String difficulte, Professeur professeur) {
        this.enonce = enonce;
        this.type = type;
        this.difficulte = difficulte;
        this.professeur = professeur;
    }
}
