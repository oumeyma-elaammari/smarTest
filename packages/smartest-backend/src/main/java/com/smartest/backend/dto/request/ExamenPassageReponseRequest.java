package com.smartest.backend.dto.request;

import lombok.Data;

import java.util.List;


@Data
public class ExamenPassageReponseRequest {
    private Long questionId;
    // QCM ou Vrai/Faux : une seule réponse 
    private Long reponseId;
    // Cases à cocher : plusieurs ids de réponses sélectionnées
    private List<Long> reponseIds;
    //Rédaction : texte libre 
    private String reponseTexte;
}
