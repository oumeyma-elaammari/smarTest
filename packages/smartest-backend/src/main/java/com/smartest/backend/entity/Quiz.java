package com.smartest.backend.entity;

import com.smartest.backend.entity.enumeration.StatutQuiz;
import jakarta.persistence.*;
import lombok.*;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

@Entity
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Quiz {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String titre;
    private Integer duree;

    // 🔥 NOUVEAU
    @Enumerated(EnumType.STRING)
    private StatutQuiz statut;   // BROUILLON / PUBLIE

    // 🔥 NOUVEAU
    private LocalDateTime datePublication;

    @ManyToOne
    private Professeur professeur;


    //@ManyToMany
    //private List<Question> questions;

    @ManyToMany
    @JoinTable(
            name = "quiz_question",
            joinColumns = @JoinColumn(name = "quiz_id"),
            inverseJoinColumns = @JoinColumn(name = "question_id")
    )
    private List<Question> questions = new ArrayList<>();

}