package com.smartest.backend.dto.response;

import lombok.Data;

import java.util.List;

@Data
public class QuizPassageWebResponse {
    private Long id;
    private String titre;
    private Integer duree;
    private Integer nombreQuestions;
    private List<QuestionPassageWebResponse> questions;
}
