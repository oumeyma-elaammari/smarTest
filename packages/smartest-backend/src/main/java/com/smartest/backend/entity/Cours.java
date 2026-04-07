package com.smartest.backend.entity;

import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "cours")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class Cours {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, length = 200)
    private String titre;

    @Column(columnDefinition = "TEXT")
    private String contenu;

    @ManyToOne
    @JoinColumn(name = "professeur_id", nullable = false)
    private Professeur professeur;

    @OneToMany(mappedBy = "cours", cascade = CascadeType.ALL)
    private List<Question> questions = new ArrayList<>();

    public Cours(String titre, String contenu, Professeur professeur) {
        this.titre = titre;
        this.contenu = contenu;
        this.professeur = professeur;
    }
}
