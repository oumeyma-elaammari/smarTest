package com.smartest.backend.dto.response;

import com.smartest.backend.entity.Reponse;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class ReponseResponse {

    private Long id;

    private String contenu;
    private Boolean correcte;

    private Long questionId;

    private String questionEnonce;

    /**
     * Constructeur de conversion depuis l'entité Reponse
     */
    public ReponseResponse(Reponse reponse) {
        this.id = reponse.getId();
        this.contenu = reponse.getContenu();
        this.correcte = reponse.getCorrecte();

        if (reponse.getQuestion() != null) {
            this.questionId = reponse.getQuestion().getId();
            this.questionEnonce = reponse.getQuestion().getEnonce();
        }
    }

    /**
     * Méthode statique pour convertir une entité en DTO
     */
    public static ReponseResponse fromEntity(Reponse reponse) {
        return new ReponseResponse(reponse);
    }
}
