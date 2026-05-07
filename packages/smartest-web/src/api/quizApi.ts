import api from './axiosConfig'
import type { QuizWebItem } from './quizSchemas'

export type { QuizWebItem }

export const quizApi = {
    async getMesPublicationsWeb(signal?: AbortSignal) {
        try {
            return await api.get<QuizWebItem[]>('/api/quizs/mes-publications-web', { signal })
        } catch (err) {
            console.error('[quizApi] getMesPublicationsWeb', err)
            throw err
        }
    },
}
