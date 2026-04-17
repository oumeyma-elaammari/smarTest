package com.smartest.backend.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.*;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "etudiant")
@Getter @Setter @NoArgsConstructor @AllArgsConstructor
public class Etudiant {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(length = 100)
    private String nom;

    @Column(unique = true, length = 150)
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

    @JsonIgnore
    @ToString.Exclude
    @OneToMany(mappedBy = "etudiant", cascade = CascadeType.ALL)
    private List<Reponse> reponses = new ArrayList<>();

    @JsonIgnore
    @ToString.Exclude
    @OneToMany(mappedBy = "etudiant", cascade = CascadeType.ALL)
    private List<Resultat> resultats = new ArrayList<>();



}