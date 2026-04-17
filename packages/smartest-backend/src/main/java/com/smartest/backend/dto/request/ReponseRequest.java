 package com.smartest.backend.dto.request;


import lombok.Data;

@Data
public class ReponseRequest {
    private Long questionId;
    private Long reponseId;
    private Long sessionId;
    private Long etudiantId;
}

