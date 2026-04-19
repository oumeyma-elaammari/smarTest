package com.smartest.backend.dto.request;

import lombok.Data;
import java.util.List;

@Data
public class SoumissionQuizRequest {
    private Long etudiantId;
    private List<ReponseQuizDTO> reponses;
}