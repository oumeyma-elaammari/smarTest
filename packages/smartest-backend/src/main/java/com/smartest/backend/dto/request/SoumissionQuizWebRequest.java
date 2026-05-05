package com.smartest.backend.dto.request;

import lombok.Data;

import java.util.List;

@Data
public class SoumissionQuizWebRequest {
    private List<ReponseQuizDTO> reponses;
}
