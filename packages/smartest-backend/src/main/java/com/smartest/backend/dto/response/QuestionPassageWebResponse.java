package com.smartest.backend.dto.response;

import lombok.Data;

import java.util.List;

@Data
public class QuestionPassageWebResponse {
    private Long id;
    private String enonce;
    private List<ReponsePassageWebResponse> reponses;
}
