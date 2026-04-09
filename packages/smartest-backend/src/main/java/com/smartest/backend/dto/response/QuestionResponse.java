package com.smartest.backend.dto.response;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import java.util.List;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.entity.enumeration.Difficulte;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class QuestionResponse {

    private Long id;

    private String enonce;

    private TypeQuestion type;

    private Difficulte difficulte;

    private List<ReponseResponse> reponses;
}
