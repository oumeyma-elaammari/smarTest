package com.smartest.backend.controller;

import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.service.QuestionService;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/questions")
public class QuestionController {

    @Autowired
    private QuestionService questionService;

    //  créer question
    @PostMapping
    public Question create(
            @RequestBody Question question,
            @RequestParam Long professeurId,
            @RequestParam(required = false) Long coursId
    ) {
        return questionService.createQuestion(question, professeurId, coursId);
    }

    //  toutes
    @GetMapping
    public List<Question> getAll() {
        return questionService.getAll();
    }

    //  par type
    @GetMapping("/type")
    public List<Question> getByType(@RequestParam TypeQuestion type) {
        return questionService.getByType(type);
    }

    //  par difficulté
    @GetMapping("/difficulte")
    public List<Question> getByDiff(@RequestParam Difficulte difficulte) {
        return questionService.getByDifficulte(difficulte);
    }

    //  par cours + difficulté
    @GetMapping("/cours")
    public List<Question> getByCours(
            @RequestParam Long coursId,
            @RequestParam Difficulte difficulte
    ) {
        return questionService.getByCoursAndDifficulte(coursId, difficulte);
    }

    //  supprimer
    @DeleteMapping("/{id}")
    public void delete(@PathVariable Long id) {
        questionService.delete(id);
    }
}