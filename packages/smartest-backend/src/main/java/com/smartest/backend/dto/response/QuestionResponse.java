package com.smartest.backend.dto.response;

import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import lombok.Data;
import java.util.List;

@Data
public class QuestionResponse {

    private Long id;
    private String enonce;
    private TypeQuestion type;
    private Difficulte difficulte;

    private List<ReponseResponse> reponses;
}