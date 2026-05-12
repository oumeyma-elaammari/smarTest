import api from './axiosConfig'
import type { ExamenMeta, ExamenSnapshot } from './quizSchemas'

export const examenApi = {
    /** Liste métadonnées (étudiant autorisé par email). */
    getMesPublicationsWeb(signal?: AbortSignal) {
        return api.get<unknown>('/api/examens-publies/mes-publications-web', { signal })
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
    repondreQuestionCourante(examenId: number, etudiantId: number, questionId: number, reponseId: number) {
        return api.post(`/api/examens-publies/${examenId}/passage/reponse`, null, {
            params: { etudiantId, questionId, reponseId },
        })
    },
    soumettreFinal(examenId: number, etudiantId: number) {
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
    definirBareme(examenId: number, baremeSur20: number) {
        return api.patch<ExamenSnapshot>(`/api/examens-publies/${examenId}/bareme`, null, {
            params: { baremeSur20 },
        })
    },
    snapshot(examenId: number, signal?: AbortSignal) {
        return api.get<ExamenSnapshot>(`/api/examens-publies/${examenId}/supervision/snapshot`, { signal })
    },
}
