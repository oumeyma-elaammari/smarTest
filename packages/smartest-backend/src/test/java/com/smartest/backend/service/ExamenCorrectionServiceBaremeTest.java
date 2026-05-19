package com.smartest.backend.service;

import com.smartest.backend.dto.examen.StudentAnswerSnapshot;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

import static org.assertj.core.api.Assertions.assertThat;

class ExamenCorrectionServiceBaremeTest {

    @Test
    @DisplayName("pointsPourQuestion utilise le barème de la question quand il est défini")
    void pointsPourQuestion_utiliseBaremeQuestion() {
        Question q = new Question();
        q.setBaremePoints(2.0);

        assertThat(ExamenCorrectionService.pointsPourQuestion(q, 20.0, 12)).isEqualTo(2.0);
    }

    @Test
    @DisplayName("pointsPourQuestion retombe sur la répartition uniforme si barème question absent")
    void pointsPourQuestion_fallbackUniforme() {
        Question q = new Question();

        assertThat(ExamenCorrectionService.pointsPourQuestion(q, 20.0, 10)).isEqualTo(2.0);
    }

    @Test
    @DisplayName("QCM correct : note égale au barème de la question (ex. 2 pt)")
    void qcmCorrect_noteEgaleBareme() {
        Question q = questionQcmAvecBareme(2.0, 100L, true);
        StudentAnswerSnapshot snap = new StudentAnswerSnapshot(1L, 100L, Set.of(), null);

        double score = invokeScoreQcm(q, snap, 2.0);
        assertThat(score).isEqualTo(2.0);
    }

    @Test
    @DisplayName("libellé sans réponse")
    void libellePasDeReponse() {
        assertThat(ExamenCorrectionService.LIBELLE_PAS_DE_REPONSE).isEqualTo("Pas de réponse donnée");
    }

    @Test
    @DisplayName("Cases à cocher : bonnes cochées et mauvaises non cochées → note complète")
    void checkbox_parfait_noteComplete() {
        Question q = questionCheckbox(1.0, List.of(true, true, false, false));
        Set<Long> sel = new LinkedHashSet<>(List.of(10L, 11L));

        assertThat(ExamenCorrectionService.scoreCasesACocher(q, snap(sel), 1.0)).isEqualTo(1.0);
    }

    @Test
    @DisplayName("Cases à cocher : tout cocher → moitié de la note si 2 bonnes et 2 mauvaises")
    void checkbox_toutCocher_moitieNote() {
        Question q = questionCheckbox(1.0, List.of(true, true, false, false));
        Set<Long> sel = new LinkedHashSet<>(List.of(10L, 11L, 12L, 13L));

        assertThat(ExamenCorrectionService.scoreCasesACocher(q, snap(sel), 1.0)).isEqualTo(0.5);
    }

    @Test
    @DisplayName("Cases à cocher : seulement mauvaises cochées → 0")
    void checkbox_seulementFaux_zero() {
        Question q = questionCheckbox(1.0, List.of(true, true, false, false));
        Set<Long> sel = new LinkedHashSet<>(List.of(12L, 13L));

        assertThat(ExamenCorrectionService.scoreCasesACocher(q, snap(sel), 1.0)).isEqualTo(0.0);
    }

    private static StudentAnswerSnapshot snap(Set<Long> ids) {
        return new StudentAnswerSnapshot(1L, null, ids, null);
    }

    private static Question questionCheckbox(double bareme, List<Boolean> correctesParSlot) {
        Question q = new Question();
        q.setId(1L);
        q.setType(TypeQuestion.CASES_A_COCHER);
        q.setBaremePoints(bareme);
        List<Reponse> reps = new java.util.ArrayList<>();
        long id = 10L;
        for (Boolean ok : correctesParSlot) {
            Reponse r = new Reponse();
            r.setId(id++);
            r.setCorrecte(ok);
            reps.add(r);
        }
        q.setReponses(reps);
        return q;
    }

    private static Question questionQcmAvecBareme(double bareme, long reponseIdCorrecte, boolean correcte) {
        Question q = new Question();
        q.setId(1L);
        q.setType(TypeQuestion.QCM);
        q.setBaremePoints(bareme);
        Reponse r = new Reponse();
        r.setId(reponseIdCorrecte);
        r.setCorrecte(correcte);
        r.setContenu("Bonne réponse");
        q.setReponses(List.of(r));
        return q;
    }

    private static double invokeScoreQcm(Question q, StudentAnswerSnapshot snap, double pts) {
        try {
            var m = ExamenCorrectionService.class.getDeclaredMethod(
                    "scoreQcmOuVf", Question.class, StudentAnswerSnapshot.class, double.class);
            m.setAccessible(true);
            return (double) m.invoke(null, q, snap, pts);
        } catch (ReflectiveOperationException e) {
            throw new RuntimeException(e);
        }
    }
}
