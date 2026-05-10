/** Stats agrégées par question (aligné backend QuestionQrLiveStatResponse). */
export type QuizStatQuestion = {
    questionId: number
    numeroQuestion: number
    questionEnonce: string
    nombreReponses: number
    nombreCorrectes: number
    nombreIncorrectes: number
    pourcentageReussite: number
    pourcentageEchec: number
}

export type QuizQrLiveStatsPayload = {
    quizTitre?: string
    nombreParticipants?: number
    totalSoumissionsQuestions?: number
    tauxReussiteGlobal?: number
    statistiquesParQuestion?: QuizStatQuestion[]
}

export type StreamType = 'QUIZ' | 'STATS' | 'FEEDBACK' | 'CLOSED'

export type ReponseDto = {
    id: number
    contenu: string
}

export type QuestionDto = {
    id: number
    enonce: string
    reponses: ReponseDto[]
}

export type QuizSnap = {
    id: number
    titre?: string
    nombreQuestions?: number
    questions: QuestionDto[]
}

/** Réponse POST `/api/qr-live/sessions/{token}/reponses` (correction immédiate). */
export type QrLiveSubmitAnswerResponseDto = {
    correct: boolean
    bonneReponseId: number
}

export type StreamEnvelope = {
    type: StreamType
    quiz?: QuizSnap
    stats?: QuizQrLiveStatsPayload
    feedback?: {
        correlationId: string
        questionIndex: number
        correcte: boolean
    }
    /** Extension possible côté backend ; non utilisée aujourd’hui (CLOSED suffit). */
    statut?: string
}
