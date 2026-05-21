import api from './axiosConfig'
import type { ExamenMeta, ExamenSnapshot } from './quizSchemas'

export const examenApi = {
    /** Liste métadonnées (étudiant autorisé par email). */
    getMesPublicationsWeb(signal?: AbortSignal) {
        return api.get<unknown>('/api/examens-publies/mes-publications-web', { signal })
    },
    /** Suppression par le professeur : retire l'examen pour le prof et les étudiants. */
    supprimerExamen(examenId: number) {
        return api.delete(`/api/examens-publies/${examenId}`)
    },
    getMetadata(examenId: number, signal?: AbortSignal) {
        return api.get<ExamenMeta>(`/api/examens-publies/${examenId}/metadata`, { signal })
    },
    rejoindreSalleAttente(examenId: number, etudiantId: number, email: string) {
        return api.post(`/api/examens-publies/${examenId}/salle-attente/rejoindre`, null, {
            params: { etudiantId, email },
        })
    },
    getSalleAttente(examenId: number, signal?: AbortSignal) {
        return api.get(`/api/examens-publies/${examenId}/salle-attente`, { signal })
    },
    getQuestionCourante(examenId: number, etudiantId: number, signal?: AbortSignal) {
        return api.get(`/api/examens-publies/${examenId}/passage/question-courante`, {
            signal,
            params: { etudiantId },
        })
    },
    repondreQuestionCourante(
        examenId: number,
        etudiantId: number,
        body: {
            questionId: number
            reponseId?: number | null
            reponseIds?: number[] | null
            reponseTexte?: string | null
        },
    ) {
        return api.post(`/api/examens-publies/${examenId}/passage/reponse`, body, {
            params: { etudiantId },
            headers: { 'Content-Type': 'application/json' },
        })
    },
    soumettreFinal(examenId: number, etudiantId: number) {
        return api.post(`/api/examens-publies/${examenId}/passage/soumettre-final`, null, {
            params: { etudiantId },
        })
    },
    /** Alias explicite pour la soumission finale étudiant. */
    soumettreExamenFinal(examenId: number, etudiantId: number) {
        return api.post(`/api/examens-publies/${examenId}/passage/soumettre-final`, null, {
            params: { etudiantId },
        })
    },
    lancer(examenId: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/controle/lancer`)
    },
    pause(examenId: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/controle/pause`)
    },
    reprendre(examenId: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/controle/reprendre`)
    },
    terminer(examenId: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/controle/terminer`)
    },
    questionSuivante(examenId: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/controle/question/suivante`)
    },
    questionPrecedente(examenId: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/controle/question/precedente`)
    },
    allerAQuestion(examenId: number, numeroQuestion: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/controle/question/aller`, null, {
            params: { numeroQuestion },
        })
    },
    ajusterTemps(examenId: number, deltaMinutes: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/controle/temps`, null, {
            params: { deltaMinutes },
        })
    },
    ajusterMinuteurQuestion(examenId: number, deltaSeconds: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/controle/minuteur-question`, null, {
            params: { deltaSeconds },
        })
    },
    /** Durée par défaut du minuteur pour chaque question (≥ 5 s). Au lancement et à chaque changement de question. */
    definirDureeMinuteurParQuestion(examenId: number, questionDurationSeconds: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/controle/mode-passage`, null, {
            params: { mode: 'MANUAL', questionDurationSeconds },
        })
    },
    definirBareme(examenId: number, baremeSur20: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/bareme`, null, {
            params: { baremeSur20 },
        })
    },
    snapshot(examenId: number, signal?: AbortSignal) {
        return api.get<ExamenSnapshot>(`/api/examens-publies/${examenId}/supervision/snapshot`, { signal })
    },
    getResultatsEnAttente(examenId: number, signal?: AbortSignal) {
        return api.get<unknown>(`/api/examens-publies/${examenId}/supervision/resultats-en-attente`, { signal })
    },
    getCorrectionsEtudiant(examenId: number, etudiantId: number, signal?: AbortSignal) {
        return api.get<unknown>(
            `/api/examens-publies/${examenId}/supervision/etudiants/${etudiantId}/corrections`,
            { signal },
        )
    },
    validerCorrectionsDetail(
        examenId: number,
        etudiantId: number,
        body: { notesFinales: Record<string, number>; noteTotale: number; remarque?: string | null },
    ) {
        return api.post(`/api/examens-publies/${examenId}/supervision/etudiants/${etudiantId}/valider-corrections-detail`, body)
    },
    synchroniserNoteWorkbench(examenId: number, etudiantId: number) {
        return api.post(
            `/api/examens-publies/${examenId}/supervision/etudiants/${etudiantId}/synchroniser-note-workbench`,
        )
    },
    getEtudiantsPassages(examenId: number, signal?: AbortSignal) {
        return api.get<unknown>(`/api/examens-publies/${examenId}/supervision/etudiants-passages`, { signal })
    },
    getResultatVisible(examenId: number, etudiantId: number, signal?: AbortSignal) {
        return api.get<{ visible: boolean; noteFinale?: number | null; bareme?: number | null }>(
            `/api/examens-publies/${examenId}/passage/resultat-visible`,
            { signal, params: { etudiantId } },
        )
    },
}
