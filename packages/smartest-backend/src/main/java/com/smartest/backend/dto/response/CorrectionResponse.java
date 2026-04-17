package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class CorrectionResponse {

    private Long questionId;
    private String enonce;

    private Long reponseChoisieId;
    private String reponseChoisieContenu;

    private boolean correct;

    private List<ReponseResponse> reponsesCorrectes;

    private String explication;
}