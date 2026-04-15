package com.smartest.backend.dto.response;

import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import lombok.Data;
import java.util.List;

@Data
public class QuestionResponse {

    private Long id;
    private String enonce;
    private String type;
    private String difficulte;

    private List<ReponseResponse> reponses;
}