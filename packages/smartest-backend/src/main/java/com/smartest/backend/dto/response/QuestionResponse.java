package com.smartest.backend.dto.response;

import java.util.List;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.entity.enumeration.Difficulte;

public class QuestionResponse {
    private Long id;
    private String enonce;
    private TypeQuestion type;
    private Difficulte difficulte;
    private List<ReponseResponse> reponses;

    // setters
    public void setId(Long id) { this.id = id; }
    public void setEnonce(String enonce) { this.enonce = enonce; }
    public void setType(TypeQuestion type) { this.type = type; }
    public void setDifficulte(Difficulte difficulte) { this.difficulte = difficulte; }
    public void setReponses(List<ReponseResponse> reponses) { this.reponses = reponses; }

    // getters (optionnels mais utiles)
    public Long getId() { return id; }
    public String getEnonce() { return enonce; }
    public TypeQuestion getType() { return type; }
    public Difficulte getDifficulte() { return difficulte; }
    public List<ReponseResponse> getReponses() { return reponses; }
}
