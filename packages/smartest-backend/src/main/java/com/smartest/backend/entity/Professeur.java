package com.smartest.backend.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.*;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "professeur")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Professeur {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, length = 100)
    private String nom;

    @Column(nullable = false, unique = true, length = 150)
    private String email;

    @Column(nullable = false)
    private String password;

    @Column(nullable = false)
    private boolean emailVerifie = false;

    @Column(unique = true)
    private String tokenVerification;

    @Column(unique = true)
    private String resetPasswordToken;

    private LocalDateTime resetPasswordExpiry;

    @JsonIgnore @ToString.Exclude
    @OneToMany(mappedBy = "professeur", cascade = CascadeType.ALL)
    private List<Cours> cours = new ArrayList<>();

    @JsonIgnore @ToString.Exclude
    @OneToMany(mappedBy = "professeur", cascade = CascadeType.ALL)
    private List<Question> questions = new ArrayList<>();

    @JsonIgnore @ToString.Exclude
    @OneToMany(mappedBy = "professeur", cascade = CascadeType.ALL)
    private List<Quiz> quizs = new ArrayList<>();

    @JsonIgnore @ToString.Exclude
    @OneToMany(mappedBy = "professeur", cascade = CascadeType.ALL)
    private List<Examen> examens = new ArrayList<>();
}