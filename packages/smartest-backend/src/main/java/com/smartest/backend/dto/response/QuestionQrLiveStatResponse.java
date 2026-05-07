package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class QuestionQrLiveStatResponse {
    private Long questionId;
    private Integer numeroQuestion;
    private String questionEnonce;
    private Integer nombreReponses;
    private Integer nombreCorrectes;
    private Integer nombreIncorrectes;
    private Double pourcentageReussite;
    private Double pourcentageEchec;
}
