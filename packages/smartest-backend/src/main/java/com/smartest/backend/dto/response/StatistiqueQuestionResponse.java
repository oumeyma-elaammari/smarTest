package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class StatistiqueQuestionResponse {
    private Long questionId;
    private String questionEnonce;
    private String typeQuestion;
    private Integer nombreReponses;
    private Integer nombreCorrectes;
    private Integer nombreIncorrectes;
    private Double pourcentageReussite;
    private Double pourcentageEchec;
    private Boolean alerteEchec;
}