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

/** Accepte une chaîne ISO ou le tableau [y,m,d,…] renvoyé par certains sérialiseurs JSON. */
const dateTimeLike = z.preprocess((val) => {
    if (val == null) return undefined
    if (typeof val === 'string') return val
    if (Array.isArray(val) && val.length >= 3) {
        const y = Number(val[0])
        const mo = Number(val[1]) - 1
        const d = Number(val[2])
        const h = val.length > 3 ? Number(val[3]) : 0
        const mi = val.length > 4 ? Number(val[4]) : 0
        const s = val.length > 5 ? Number(val[5]) : 0
        if (!Number.isFinite(y) || !Number.isFinite(mo) || !Number.isFinite(d)) return undefined
        return new Date(y, mo, d, h, mi, s).toISOString()
    }
    return undefined
}, z.string().optional())

export const examenMetaSchema = z.object({
    id: z.number(),
    titre: z.string(),
    description: z.string().optional(),
    dateDebut: dateTimeLike,
    duree: z.number().optional(),
    totalQuestions: z.number().optional(),
    statut: z.string().optional(),
    bareme: z.number().optional(),
    demarrageAutomatique: z.boolean().optional(),
    professeurNom: z.string().optional(),
})

export const examenMetaListSchema = z.array(examenMetaSchema)

/** Élément liste examens (carte dashboard). */
export const examenListeItemSchema = z
    .object({
        id: z.number(),
        titre: z.string(),
        description: z.string().optional().nullable(),
        duree: z.number().optional().nullable(),
        statut: z.string().optional(),
        dateDebut: dateTimeLike,
        dateFin: dateTimeLike,
        dateCreation: dateTimeLike,
        professeur: z
            .object({
                nom: z.string().optional(),
                email: z.string().optional(),
            })
            .optional()
            .nullable(),
    })
    .passthrough()

export const examenListeSchema = z.array(examenListeItemSchema)

export type ExamenListeItem = z.infer<typeof examenListeItemSchema>

/** Champs souvent null côté Jackson (chaînes) — sans .nullish() le parse Zod échoue et le WS est ignoré. */
const questionCouranteSnapshotPart = z
    .object({
        id: z.coerce.number().nullish(),
        numero: z.coerce.number().nullish(),
        enonce: z.string().nullish(),
        type: z.string().nullish(),
        reponses: z
            .array(
                z
                    .object({
                        id: z.coerce.number().nullish(),
                        contenu: z.string().nullish(),
                    })
                    .passthrough(),
            )
            .nullish(),
    })
    .passthrough()

const planQuestionRowSchema = z
    .object({
        id: z.coerce.number().nullish(),
        numero: z.coerce.number().nullish(),
        enonce: z.string().nullish(),
        type: z.string().nullish(),
    })
    .passthrough()

export const examenSnapshotSchema = z.object({
    examenId: z.coerce.number(),
    /** Souvent fourni avec le snapshot supervision (libellé côté serveur même si métadonnée absente). */
    titre: z.string().nullable().optional(),
    etat: z.string(),
    enPause: z.boolean().optional().default(false),
    questionCouranteIndex: z.number().nullable().optional(),
    totalQuestions: z.coerce.number().optional(),
    questionCourante: questionCouranteSnapshotPart.nullish(),
    /** Ordre et énoncés de toute l’épreuve (supervision prof). */
    planQuestions: z.array(planQuestionRowSchema).optional(),
    tempsRestantMinutes: z.number().nullish(),
    baremeSur20: z.coerce.number().optional(),
    participantsEnAttente: z.coerce.number().optional(),
    resultatsEnAttente: z.coerce.number().optional(),
    resultatsValides: z.coerce.number().optional(),
    advanceMode: z.string().optional(),
    questionDurationSeconds: z.coerce.number().optional(),
})

export type ExamenMeta = z.infer<typeof examenMetaSchema>
export type ExamenSnapshot = z.infer<typeof examenSnapshotSchema>

/** Réponse GET `/passage/question-courante` (étudiant). */
export const examenQuestionStateSchema = z.object({
    examenId: z.coerce.number(),
    etat: z.string(),
    /** Jackson peut omettre les booléens false selon la config ; le polling étudiant ne doit pas casser. */
    enPause: z.boolean().optional().default(false),
    totalQuestions: z.coerce.number(),
    questionCouranteIndex: z.number().nullable().optional(),
    questionCourante: questionCouranteSnapshotPart.nullish(),
    tempsRestantMinutes: z.number().nullish(),
})

export function mapQuestionStateToSnapshot(raw: unknown): ExamenSnapshot | null {
    const parsed = examenQuestionStateSchema.safeParse(raw)
    if (!parsed.success) return null
    const q = parsed.data
    return {
        examenId: q.examenId,
        etat: q.etat,
        enPause: q.enPause,
        questionCouranteIndex: q.questionCouranteIndex ?? undefined,
        totalQuestions: q.totalQuestions,
        questionCourante: q.questionCourante === undefined || q.questionCourante === null ? undefined : q.questionCourante,
        tempsRestantMinutes: q.tempsRestantMinutes ?? undefined,
    }
}
