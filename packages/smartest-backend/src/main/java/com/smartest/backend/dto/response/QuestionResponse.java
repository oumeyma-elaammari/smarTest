package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;

import java.util.List;

    @Data
    @Builder
    @AllArgsConstructor
    public class QuestionResponse {

        private Long id;
        private String enonce;
        private String type;
        private String difficulte;
        private String explication;
        private List<ReponseResponse> reponses;

        // ===== Informations du professeur =====
        private Long professeurId;
        private String professeurNom;

        // ===== Informations du cours =====
        private Long coursId;
        private String coursTitre;
        private String coursDescription;

        public QuestionResponse (){}

    }


