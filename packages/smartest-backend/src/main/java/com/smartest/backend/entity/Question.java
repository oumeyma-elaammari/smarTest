package com.smartest.backend.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
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

    // ✅ CORRECTION : Utiliser les types Enum
    @Enumerated(EnumType.STRING)
    private TypeQuestion type;

    @Enumerated(EnumType.STRING)
    private Difficulte difficulte;

    @Column(columnDefinition = "TEXT")
    private String explication;  // ← Ajouté pour l'explication pédagogique

    // ===== RELATIONS =====
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
    private List<Quiz> quizzes = new ArrayList<>();

    @JsonIgnore
    @ToString.Exclude
    @ManyToMany(mappedBy = "questions")
    private List<Examen> examens = new ArrayList<>();

    @JsonIgnore
    @ToString.Exclude
    @OneToMany(mappedBy = "question", cascade = CascadeType.ALL, orphanRemoval = true)
    private List<Reponse> reponses = new ArrayList<>();

    // Constructeur personnalisé
    public Question(String enonce, TypeQuestion type, Difficulte difficulte, Professeur professeur) {
        this.enonce = enonce;
        this.type = type;
        this.difficulte = difficulte;
        this.professeur = professeur;
    }

    // ==================== GETTERS ET SETTERS ====================

    public Long getId() {
        return id;
    }

    public String getEnonce() {
        return enonce;
    }

    public TypeQuestion getType() {
        return type;
    }

    public Difficulte getDifficulte() {
        return difficulte;
    }

    public String getExplication() {
        return explication;
    }

    public Professeur getProfesseur() {
        return professeur;
    }

    public Cours getCours() {
        return cours;
    }

    public List<Reponse> getReponses() {
        return reponses;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public void setEnonce(String enonce) {
        this.enonce = enonce;
    }

    public void setType(TypeQuestion type) {
        this.type = type;
    }

    public void setDifficulte(Difficulte difficulte) {
        this.difficulte = difficulte;
    }

    public void setExplication(String explication) {
        this.explication = explication;
    }

    public void setProfesseur(Professeur professeur) {
        this.professeur = professeur;
    }

    public void setCours(Cours cours) {
        this.cours = cours;
    }

    public void setReponses(List<Reponse> reponses) {
        this.reponses = reponses;
    }
}