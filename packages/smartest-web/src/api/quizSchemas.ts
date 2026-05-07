import { z } from 'zod'

const reponseWebSchema = z.object({
    id: z.number(),
    contenu: z.string(),
})

const questionWebSchema = z.object({
    id: z.number(),
    enonce: z.string(),
    reponses: z.array(reponseWebSchema),
})

/** Réponse GET `/api/quizs/{id}/passage-web` */
export const quizPassageWebSchema = z.object({
    id: z.number(),
    titre: z.string(),
    duree: z.number().optional(),
    nombreQuestions: z.number(),
    questions: z.array(questionWebSchema),
})

export const quizWebItemSchema = z.object({
    id: z.number(),
    titre: z.string(),
    duree: z.number(),
    statut: z.string().optional(),
    datePublication: z.string().optional(),
    professeurNom: z.string().optional(),
    nombreQuestions: z.number().optional(),
    premiereTentative: z.boolean().optional(),
    meilleurScore: z.number().nullable().optional(),
})

export const quizWebListSchema = z.array(quizWebItemSchema)

export type QuizWebItem = z.infer<typeof quizWebItemSchema>
export type QuizPassageWebDto = z.infer<typeof quizPassageWebSchema>
