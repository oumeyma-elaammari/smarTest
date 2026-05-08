import { useEffect, useMemo, useState, type ReactElement } from 'react'
import { useParams } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import axios from 'axios'
import api from '../api/axiosConfig'
import { shuffleCopy } from '../utils/shuffle'
import { getUserErrorMessage } from '../utils/userErrorMessage'

type ReponseWeb = { id: number; contenu: string }
type QuestionWeb = { id: number; enonce: string; reponses: ReponseWeb[] }
type QuizPassageWeb = { id: number; titre: string; nombreQuestions: number; questions: QuestionWeb[] }
type CorrectionWeb = {
    questionId: number
    enonce: string
    reponseChoisieContenu?: string | null
    reponseCorrecteContenu?: string | null
    correcte: boolean
}
type ResultatWeb = {
    score: number
    bonnesReponses: number
    totalQuestions: number
    corrections: CorrectionWeb[]
}
type VerificationFeedback = { correcte: boolean; reponseCorrecteId: number | null }

const sans = "'DM Sans', system-ui, sans-serif"
const storageKey = (quizId: number) => `smartest.quiz.web.qr.draft.${quizId}`
const participantKeyName = 'smartest.qr.participant'

let memoryParticipantId: string | null = null

function makeRandomParticipantId(): string {
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
        return `qr-${crypto.randomUUID()}`
    }
    const bytes = new Uint8Array(16)
    crypto.getRandomValues(bytes)
    const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('')
    return `qr-${hex}`
}

function apiErrorMessage(e: unknown, fallback: string): string {
    if (axios.isAxiosError(e) && e.code === 'ECONNABORTED') return 'Delai depasse. Reessayez.'
    if (axios.isAxiosError(e) && e.response?.status === 404) return 'Quiz introuvable.'
    return getUserErrorMessage(e, fallback)
}

function buildQrRightActionButton(params: {
    vCourante: VerificationFeedback | null
    questionCourante: QuestionWeb | null
    choix: Record<number, number>
    verifying: boolean
    derniereQuestion: boolean
    submitting: boolean
    peutSoumettre: boolean
    onVerifier: () => void
    onSuivant: () => void
    onSoumettre: () => void
}): ReactElement {
    const {
        vCourante,
        questionCourante,
        choix,
        verifying,
        derniereQuestion,
        submitting,
        peutSoumettre,
        onVerifier,
        onSuivant,
        onSoumettre,
    } = params
    const hasCurrentQuestion = !!questionCourante
    const questionId = questionCourante?.id ?? -1

    if (vCourante == null) {
        return (
            <button
                type="button"
                disabled={!hasCurrentQuestion || !choix[questionId] || verifying}
                onClick={onVerifier}
                style={{ height: 30, borderRadius: 8, border: 'none', padding: '0 14px', cursor: 'pointer', background: '#0f1e3d', color: '#fff', opacity: !hasCurrentQuestion || !choix[questionId] || verifying ? 0.55 : 1 }}
            >
                {verifying ? 'Vérification…' : 'Soumettre'}
            </button>
        )
    }

    if (!derniereQuestion) {
        return (
            <button
                type="button"
                onClick={onSuivant}
                style={{ height: 30, borderRadius: 8, border: 'none', padding: '0 14px', cursor: 'pointer', background: '#0f1e3d', color: '#fff' }}
            >
                Suivant
            </button>
        )
    }

    return (
        <button
            type="button"
            disabled={!peutSoumettre || submitting}
            onClick={onSoumettre}
            style={{ height: 30, borderRadius: 8, border: 'none', padding: '0 14px', cursor: 'pointer', background: '#0f1e3d', color: '#fff', opacity: !peutSoumettre || submitting ? 0.55 : 1 }}
        >
            {submitting ? 'Soumission…' : 'Terminer'}
        </button>
    )
}

function getParticipantKey(): string {
    try {
        const existing = localStorage.getItem(participantKeyName)
        if (existing?.trim()) return existing.trim()
        const created = makeRandomParticipantId()
        localStorage.setItem(participantKeyName, created)
        return created
    } catch {
        if (!memoryParticipantId) memoryParticipantId = makeRandomParticipantId()
        return memoryParticipantId
    }
}

function PassageQrResultatEcran({ resultat, fontFamily }: { resultat: ResultatWeb; fontFamily: string }) {
    const scoreColor = resultat.score >= 50 ? '#2563eb' : '#dc2626'
    return (
        <div
            style={{
                fontFamily,
                width: '100%',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
            }}
        >
            <div style={{ textAlign: 'center', lineHeight: 1.45 }}>
                <p style={{ margin: 0, color: scoreColor, fontWeight: 700, fontSize: 34 }}>
                    Score : {resultat.score.toFixed(2)}%
                </p>
                <p style={{ margin: '10px 0 0', color: '#334155', fontSize: 24, fontWeight: 600 }}>
                    {resultat.bonnesReponses}/{resultat.totalQuestions} bonnes réponses
                </p>
                <p style={{ margin: '14px 0 0', color: '#64748b', fontSize: 20 }}>
                    Merci d'avoir terminé ce quiz.
                </p>
            </div>
        </div>
    )
}

function qrStyleOption(
    q: QuestionWeb,
    r: ReponseWeb,
    verification: Record<number, VerificationFeedback>,
    choix: Record<number, number>,
) {
    const v = verification[q.id]
    const selected = choix[q.id] === r.id
    if (!v) {
        return {
            border: selected ? '1px solid #4f8ef7' : '1px solid #e2e8f4',
            background: selected ? '#eef3fd' : '#fff',
            color: '#0f1e3d',
        }
    }
    const bonneId = v.reponseCorrecteId
    if (bonneId != null && r.id === bonneId) {
        return {
            border: '1px solid #22c55e',
            background: '#f0fdf4',
            color: '#166534',
        }
    }
    if (selected && !v.correcte) {
        return {
            border: '1px solid #ef4444',
            background: '#fef2f2',
            color: '#b91c1c',
        }
    }
    return {
        border: '1px solid #e8eef7',
        background: '#f8fafc',
        color: '#64748b',
    }
}

export default function QuizPassageQr() {
    const { quizId } = useParams()
    const [quiz, setQuiz] = useState<QuizPassageWeb | null>(null)
    const [loading, setLoading] = useState(true)
    const [err, setErr] = useState<string | null>(null)
    const [submitting, setSubmitting] = useState(false)
    const [verifying, setVerifying] = useState(false)
    const [resultat, setResultat] = useState<ResultatWeb | null>(null)
    const [indexCourant, setIndexCourant] = useState(0)
    const [choix, setChoix] = useState<Record<number, number>>({})
    const [verification, setVerification] = useState<Record<number, VerificationFeedback>>({})
    const qrHeaders = useMemo(() => ({ 'X-QR-Participant': getParticipantKey() }), [])

    useEffect(() => {
        let cancelled = false
        ;(async () => {
            try {
                const id = Number(quizId)
                if (!Number.isFinite(id) || id <= 0) throw new Error('Quiz invalide')
                const { data } = await api.get<QuizPassageWeb>(`/api/quizs/${id}/passage-qr`)
                if (cancelled) return
                setQuiz(data)
                const raw = (() => {
                    try {
                        return sessionStorage.getItem(storageKey(id))
                    } catch {
                        return null
                    }
                })()
                if (!raw) return
                let parsed: {
                    choix?: Record<number, number>
                    indexCourant?: number
                    verification?: Record<number, VerificationFeedback>
                }
                try {
                    parsed = JSON.parse(raw) as {
                        choix?: Record<number, number>
                        indexCourant?: number
                        verification?: Record<number, VerificationFeedback>
                    }
                } catch {
                    try {
                        sessionStorage.removeItem(storageKey(id))
                    } catch {
                        /* ignore */
                    }
                    if (!cancelled) setErr('Brouillon de session corrompu. Recommencez le quiz.')
                    return
                }
                if (parsed.choix) setChoix(parsed.choix)
                if (typeof parsed.indexCourant === 'number') setIndexCourant(parsed.indexCourant)
                if (parsed.verification) setVerification(parsed.verification)
            } catch (e) {
                if (!cancelled) setErr(apiErrorMessage(e, 'Impossible de charger le quiz QR.'))
            } finally {
                if (!cancelled) setLoading(false)
            }
        })()
        return () => {
            cancelled = true
        }
    }, [quizId])

    useEffect(() => {
        if (!quiz || resultat) return
        try {
            sessionStorage.setItem(storageKey(quiz.id), JSON.stringify({ choix, indexCourant, verification }))
        } catch (err) {
            console.warn('[QuizPassageQr] Impossible de sauvegarder le brouillon', err)
        }
    }, [quiz, choix, indexCourant, verification, resultat])

    const questionCourante = useMemo(
        () => (quiz?.questions?.length ? quiz.questions[indexCourant] : null),
        [quiz, indexCourant],
    )
    const reponsesMelangeesParQuestion = useMemo(() => {
        const map: Record<number, ReponseWeb[]> = {}
        if (!quiz) return map
        for (const question of quiz.questions) {
            map[question.id] = shuffleCopy(question.reponses ?? [])
        }
        return map
    }, [quiz])
    const peutSoumettre = useMemo(
        () => !!quiz && quiz.questions.every((q) => !!choix[q.id] && !!verification[q.id]),
        [quiz, choix, verification],
    )

    const verifier = async () => {
        if (!quiz || !questionCourante) return
        const rid = choix[questionCourante.id]
        if (!rid) return
        setVerifying(true)
        setErr(null)
        try {
            const { data } = await api.post<{ correcte: boolean; reponseCorrecteId?: number | null }>(
                `/api/quizs/${quiz.id}/verifier-question-qr`,
                { questionId: questionCourante.id, reponseId: rid },
                { headers: qrHeaders },
            )
            setVerification((prev) => ({
                ...prev,
                [questionCourante.id]: {
                    correcte: data.correcte,
                    reponseCorrecteId: data.reponseCorrecteId ?? null,
                },
            }))
        } catch (e) {
            setErr(apiErrorMessage(e, 'La vérification a échoué.'))
        } finally {
            setVerifying(false)
        }
    }

    const soumettre = async () => {
        if (!quiz || !peutSoumettre) return
        setSubmitting(true)
        setErr(null)
        try {
            const reponses = quiz.questions.map((q) => ({ questionId: q.id, reponseId: choix[q.id] }))
            const { data } = await api.post<ResultatWeb>(`/api/quizs/${quiz.id}/soumettre-qr`, { reponses }, { headers: qrHeaders })
            try {
                sessionStorage.removeItem(storageKey(quiz.id))
            } catch {
                /* ignore */
            }
            setResultat(data)
        } catch (e) {
            setErr(apiErrorMessage(e, 'La soumission a échoué.'))
        } finally {
            setSubmitting(false)
        }
    }

    if (loading) {
        return (
            <div
                style={{
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    gap: 8,
                    color: '#64748b',
                    fontFamily: sans,
                    width: '100%',
                }}
            >
                <Loader2 size={18} className="animate-spin" aria-hidden />
                Chargement…
            </div>
        )
    }
    if (err && !quiz) {
        return (
            <p style={{ color: '#b91c1c', fontFamily: sans, textAlign: 'center', width: '100%', margin: 0, lineHeight: 1.5 }}>
                {err}
            </p>
        )
    }
    if (!quiz) return null

    if (resultat) {
        return <PassageQrResultatEcran resultat={resultat} fontFamily={sans} />
    }

    if (!questionCourante) {
        return (
            <p
                style={{
                    color: '#64748b',
                    fontFamily: sans,
                    textAlign: 'center',
                    width: '100%',
                    margin: 0,
                    lineHeight: 1.5,
                }}
            >
                Ce quiz ne contient aucune question ou les données sont incomplètes.
            </p>
        )
    }

    const vCourante = verification[questionCourante.id] ?? null
    const derniereQuestion = indexCourant >= quiz.questions.length - 1
    return (
        <div style={{ fontFamily: sans, width: '100%', maxWidth: 800, margin: '0 auto' }}>
            <h2 style={{ margin: '0 0 6px', color: '#0f1e3d', fontWeight: 700, textAlign: 'left' }}>{quiz.titre}</h2>
            <p style={{ margin: '0 0 14px', color: '#64748b', fontSize: 13, textAlign: 'left' }}>
                Question {indexCourant + 1} / {quiz.questions.length}
            </p>
            <div style={{ border: '1px solid #e2e8f4', borderRadius: 10, padding: '28px 24px', background: '#fff' }}>
                <p style={{ margin: '0 0 12px', color: '#0f1e3d', fontWeight: 500 }}>{questionCourante.enonce}</p>
                <div style={{ display: 'grid', gap: 9 }}>
                    {(reponsesMelangeesParQuestion[questionCourante.id] ?? questionCourante.reponses).map((r) => {
                        const st = qrStyleOption(questionCourante, r, verification, choix)
                        return (
                        <label key={r.id} style={{ display: 'flex', alignItems: 'center', gap: 8, border: st.border, background: st.background, color: st.color, borderRadius: 8, padding: '10px' }}>
                            <input
                                type="radio"
                                name={`q-${questionCourante.id}`}
                                checked={choix[questionCourante.id] === r.id}
                                disabled={!!vCourante}
                                onChange={() => setChoix((p) => ({ ...p, [questionCourante.id]: r.id }))}
                            />
                            <span>{r.contenu}</span>
                        </label>
                    )})}
                </div>
                {vCourante ? <p style={{ margin: '12px 0 0', color: vCourante.correcte ? '#166534' : '#b91c1c', fontWeight: 600 }}>{vCourante.correcte ? 'Réponse correcte.' : 'Réponse incorrecte.'}</p> : null}
            </div>
            {err ? <p style={{ color: '#b91c1c', marginTop: 10 }}>{err}</p> : null}
            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 14, gap: 8 }}>
                <button
                    type="button"
                    disabled={indexCourant === 0}
                    onClick={() => setIndexCourant((i) => Math.max(0, i - 1))}
                    style={{ height: 30, borderRadius: 8, border: '1px solid #d1d5db', padding: '0 14px', cursor: indexCourant === 0 ? 'not-allowed' : 'pointer', opacity: indexCourant === 0 ? 0.5 : 1 }}
                >
                    Précédent
                </button>
                {buildQrRightActionButton({
                    vCourante,
                    questionCourante,
                    choix,
                    verifying,
                    derniereQuestion,
                    submitting,
                    peutSoumettre,
                    onVerifier: verifier,
                    onSuivant: () => setIndexCourant((i) => Math.min(quiz.questions.length - 1, i + 1)),
                    onSoumettre: soumettre,
                })}
            </div>
        </div>
    )
}
