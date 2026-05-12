package com.smartest.backend.repository;

import com.smartest.backend.entity.ExamenPublie;

import java.util.Optional;

/** Implémentée dans {@link ExamenPublieRepositoryImpl} pour éviter MultipleBagFetchException. */
public interface ExamenPublieRepositoryCustom {

    Optional<ExamenPublie> findWithQuestionsAndProfesseurById(Long id);
}
