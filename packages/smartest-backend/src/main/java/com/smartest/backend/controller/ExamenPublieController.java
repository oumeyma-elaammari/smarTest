package com.smartest.backend.controller;

import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.service.ExamenPublieService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDateTime;
import java.util.List;

@RestController
@RequestMapping("/api/examens-publies")
@RequiredArgsConstructor
public class ExamenPublieController {

    private final ExamenPublieService service;

    @PostMapping
    public ExamenPublie publier(@RequestParam Long professeurId,
                                @RequestParam String titre,
                                @RequestParam Integer duree,
                                @RequestParam String description,
                                @RequestParam String dateDebut,
                                @RequestParam String dateFin) {

        return service.publier(
                professeurId,
                titre,
                duree,
                description,
                LocalDateTime.parse(dateDebut),
                LocalDateTime.parse(dateFin)
        );
    }

    @GetMapping("/disponibles")
    public List<ExamenPublie> disponibles() {
        return service.getDisponibles();
    }

    @PatchMapping("/{id}/demarrer")
    public ExamenPublie demarrer(@PathVariable Long id) {
        return service.demarrer(id);
    }

    @PatchMapping("/{id}/terminer")
    public ExamenPublie terminer(@PathVariable Long id) {
        return service.terminer(id);
    }
}