package com.smartest.backend.dto.response;

import com.smartest.backend.entity.Reponse;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@AllArgsConstructor
@NoArgsConstructor
@Data
public class ReponseResponse {
    private Long id;
    private String contenu;
    private Boolean correcte;

    public ReponseResponse(Reponse reponse) {
        this.id = reponse.getId();
        this.contenu = reponse.getContenu();
        this.correcte = reponse.getCorrecte();
    }
}