import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import api from '../api/axiosConfig'

type ReponseWeb = { id: number; contenu: string }
type QuestionWeb = { id: number; enonce: string; reponses: ReponseWeb[] }
type QuizPassageWeb = {
    id: number
    titre: string
    duree?: number
    nombreQuestions: number
    questions: QuestionWeb[]
}

type CorrectionWeb = {
    questionId: number
    enonce: string
    reponseChoisieId?: number | null
    reponseChoisieContenu?: string | null
    reponseCorrecteId?: number | null
    reponseCorrecteContenu?: string | null
    correcte: boolean
    explication?: string | null
}

type ResultatWeb = {
    score: number
    bonnesReponses: number
    totalQuestions: number
    estPremiereTentative: boolean
    corrections: CorrectionWeb[]
}

type VerificationFeedback = {
    correcte: boolean
    reponseCorrecteId: number | null
    reponseCorrecteContenu: string | null
}

const sans = "'DM Sans', system-ui, sans-serif"
const storageKey = (quizId: number) => `smartest.quiz.web.draft.${quizId}`

export default function QuizPassageWeb() {
    const navigate = useNavigate()
    const { quizId } = useParams()
    const [quiz, setQuiz] = useState<QuizPassageWeb | null>(null)
    const [loading, setLoading] = useState(true)
    const [submitting, setSubmitting] = useState(false)
    const [verifying, setVerifying] = useState(false)
    const [err, setErr] = useState<string | null>(null)
    const [resultat, setResultat] = useState<ResultatWeb | null>(null)
    /** Réponse API reçue ; affichage du score après 3 s. */
    const [resultatEnAttente, setResultatEnAttente] = useState<ResultatWeb | null>(null)
    const [indexCourant, setIndexCourant] = useState(0)
    const [choix, setChoix] = useState<Record<number, number>>({})
    const [verification, setVerification] = useState<Record<number, VerificationFeedback>>({})

    useEffect(() => {
        let cancelled = false
        ;(async () => {
            try {
                const id = Number(quizId)
                if (!Number.isFinite(id) || id <= 0) {
                    throw new Error('Quiz invalide')
                }
                const { data } = await api.get<QuizPassageWeb>(`/api/quizs/${id}/passage-web`)
                if (!cancelled) {
                    setQuiz(data)
                    const raw = sessionStorage.getItem(storageKey(id))
                    if (raw) {
                        try {
                            const parsed = JSON.parse(raw) as {
                                choix?: Record<number, number>
                                indexCourant?: number
                                verification?: Record<string, VerificationFeedback>
                            }
                            if (parsed.choix && typeof parsed.choix === 'object') {
                                setChoix(parsed.choix)
                            }
                            if (
                                typeof parsed.indexCourant === 'number' &&
                                parsed.indexCourant >= 0 &&
                                parsed.indexCourant < (data.questions?.length ?? 0)
                            ) {
                                setIndexCourant(parsed.indexCourant)
                            }
                            if (parsed.verification && typeof parsed.verification === 'object') {
                                const v: Record<number, VerificationFeedback> = {}
                                for (const [key, val] of Object.entries(parsed.verification)) {
                                    const qid = Number(key)
                                    if (Number.isFinite(qid) && val && typeof val.correcte === 'boolean') {
                                        v[qid] = {
                                            correcte: val.correcte,
                                            reponseCorrecteId:
                                                val.reponseCorrecteId != null ? Number(val.reponseCorrecteId) : null,
                                            reponseCorrecteContenu:
                                                val.reponseCorrecteContenu != null
                                                    ? String(val.reponseCorrecteContenu)
                                                    : null,
                                        }
                                    }
                                }
                                setVerification(v)
                            }
                        } catch {
                            sessionStorage.removeItem(storageKey(id))
                        }
                    }
                }
            } catch (e: unknown) {
                const res = (e as { response?: { status?: number; data?: unknown } })?.response
                const data = res?.data
                if (!cancelled) {
                    if (typeof data === 'string' && data.trim()) setErr(data.trim())
                    else if (res?.status === 403) setErr("Vous n'êtes pas autorisé à passer ce quiz.")
                    else if (res?.status === 404) setErr('Quiz introuvable.')
                    else setErr('Impossible de charger ce quiz.')
                }
            } finally {
                if (!cancelled) setLoading(false)
            }
        })()
        return () => {
            cancelled = true
        }
    }, [quizId])

    useEffect(() => {
        if (!resultatEnAttente) return
        const id = window.setTimeout(() => {
            setResultat(resultatEnAttente)
            setResultatEnAttente(null)
        }, 1500)
        return () => window.clearTimeout(id)
    }, [resultatEnAttente])

    useEffect(() => {
        if (!quiz || resultat || resultatEnAttente) return
        const verificationForStorage: Record<string, VerificationFeedback> = {}
        for (const [k, v] of Object.entries(verification)) {
            verificationForStorage[String(k)] = v
        }
        sessionStorage.setItem(
            storageKey(quiz.id),
            JSON.stringify({
                choix,
                indexCourant,
                verification: verificationForStorage,
            }),
        )
    }, [quiz, choix, indexCourant, verification, resultat, resultatEnAttente])

    const questionCourante = useMemo(
        () => (quiz?.questions?.length ? quiz.questions[indexCourant] : null),
        [quiz, indexCourant],
    )

    const peutSoumettre = useMemo(() => {
        if (!quiz) return false
        return quiz.questions.every((q) => !!choix[q.id] && !!verification[q.id])
    }, [quiz, choix, verification])

    const soumettre = async () => {
        if (!quiz) return
        if (!peutSoumettre) {
            setErr('Répondez et vérifiez chaque question avant de soumettre.')
            return
        }
        setErr(null)
        setSubmitting(true)
        try {
            const reponses = quiz.questions.map((q) => ({
                questionId: q.id,
                reponseId: choix[q.id],
            }))
            const { data } = await api.post<ResultatWeb>(`/api/quizs/${quiz.id}/soumettre-web`, { reponses })
            sessionStorage.removeItem(storageKey(quiz.id))
            setResultatEnAttente(data)
        } catch (e: unknown) {
            const res = (e as { response?: { data?: unknown } })?.response
            const data = res?.data
            if (typeof data === 'string' && data.trim()) setErr(data.trim())
            else setErr('La soumission a échoué.')
        } finally {
            setSubmitting(false)
        }
    }

    const verifier = async () => {
        if (!quiz || !questionCourante) return
        const rid = choix[questionCourante.id]
        if (!rid) return
        setVerifying(true)
        setErr(null)
        try {
            const { data } = await api.post<{
                correcte: boolean
                reponseCorrecteId?: number | null
                reponseCorrecteContenu?: string | null
            }>(`/api/quizs/${quiz.id}/verifier-question-web`, {
                questionId: questionCourante.id,
                reponseId: rid,
            })
            setVerification((prev) => ({
                ...prev,
                [questionCourante.id]: {
                    correcte: data.correcte,
                    reponseCorrecteId: data.reponseCorrecteId ?? null,
                    reponseCorrecteContenu: data.reponseCorrecteContenu ?? null,
                },
            }))
        } catch (e: unknown) {
            const res = (e as { response?: { data?: unknown } })?.response
            const data = res?.data
            if (typeof data === 'string' && data.trim()) setErr(data.trim())
            else setErr('La vérification a échoué.')
        } finally {
            setVerifying(false)
        }
    }

    const styleOption = (q: QuestionWeb, r: ReponseWeb) => {
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
        if (choix[q.id] === r.id && !v.correcte) {
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

    if (loading) {
        return (
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, fontFamily: sans, color: '#64748b' }}>
                <Loader2 size={20} className="animate-spin" aria-hidden />
                Chargement du quiz…
            </div>
        )
    }

    if (err && !quiz && !resultat) {
        return (
            <div style={{ fontFamily: sans }}>
                <p style={{ color: '#b91c1c' }}>{err}</p>
                <button type="button" onClick={() => navigate('/dashboard')}>
                    Retour
                </button>
            </div>
        )
    }

    if (!quiz) return null

    if (resultatEnAttente && !resultat) {
        return (
            <div
                style={{
                    fontFamily: sans,
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    justifyContent: 'center',
                    gap: 16,
                    minHeight: 220,
                    color: '#475569',
                }}
                role="status"
                aria-live="polite"
                aria-busy="true"
            >
                <Loader2 size={36} className="animate-spin" aria-hidden style={{ color: '#0f1e3d' }} />
                <p style={{ margin: 0, fontSize: 16 }}>Calcul de votre score…</p>
            </div>
        )
    }

    if (resultat) {
        return (
            <div style={{ fontFamily: sans, maxWidth: 860}}>
                <h2 style={{ margin: '0 0 8px', color: '#0f1e3d', fontWeight: 600 }}>Résultat du quiz</h2>
                <p style={{ margin: '0 0 16px', color: '#475569', fontWeight: 600 }}>
                    Score :{' '}
                    <strong
                        style={{
                            color: resultat.score > 75 ? '#4f8ef7' : '#b91c1c',
                        }}
                    >
                        {resultat.score.toFixed(2)}%
                    </strong>{' '}
                    ({resultat.bonnesReponses}/{resultat.totalQuestions})
                </p>
                {resultat.corrections.map((c, idx) => (
                    <div
                        key={c.questionId}
                        style={{
                            border: '1px solid #e2e8f4',
                            borderRadius: 10,
                            padding: 18,
                            marginBottom: 10,
                            background: '#fff',
                        }}
                    >
                        <p style={{ margin: '0 0 8px', color: '#0f1e3d', fontWeight: 700 }}>
                            Q{idx + 1}. {c.enonce}
                        </p>
                        <p style={{ margin: '0 0 6px', color: c.correcte ? '#166534' : '#b91c1c', fontWeight: 500 }}>
                            {c.correcte ? 'Bonne réponse' : 'Réponse incorrecte'}
                        </p>
                        {!c.correcte ? (
                            <p style={{ margin: '0 0 4px', color: '#334155', fontSize: 13 }}>
                                <strong>Votre réponse :</strong> {c.reponseChoisieContenu ?? 'Aucune réponse'}
                            </p>
                        ) : null}
                        <p style={{ margin: '0 0 4px', color: '#334155', fontSize: 13 }}>
                            <strong>Bonne réponse :</strong> {c.reponseCorrecteContenu ?? 'N/A'}
                        </p>
                        <p style={{ margin: 0, color: '#475569', fontSize: 13 }}>
                            <strong>Explication :</strong>{' '}
                            {c.explication?.trim() ? c.explication : 'Aucune explication fournie.'}
                        </p>
                    </div>
                ))}
                <div style={{ display: 'flex', gap: 8, marginTop: 16 }}>
                    <button
                        type="button"
                        onClick={() => {
                            setResultat(null)
                            setResultatEnAttente(null)
                            setChoix({})
                            setVerification({})
                            setIndexCourant(0)
                            setErr(null)
                            sessionStorage.removeItem(storageKey(quiz.id))
                        }}
                        style={{
                            height: 30,
                            border: '1px solid #d1d5db',
                            borderRadius: 8,
                            padding: '0 14px',
                            cursor: 'pointer',
                            fontSize : 13
                        }}
                    >
                        Refaire le quiz
                    </button>
                    <button
                        type="button"
                        onClick={() => navigate('/dashboard')}
                        style={{
                            height: 30,
                            border: 'none',
                            borderRadius: 8,
                            padding: '0 14px',
                            cursor: 'pointer',
                            background: '#0f1e3d',
                            color: '#fff',
                            fontSize : 13
                        }}
                    >
                        Retour au tableau de bord
                    </button>
                </div>
            </div>
        )
    }

    if (!questionCourante) {
        return <p style={{ fontFamily: sans, color: '#64748b' }}>Ce quiz ne contient aucune question.</p>
    }

    const vCourante = verification[questionCourante.id]
    const derniereQuestion = indexCourant >= quiz.questions.length - 1
    const verrouillee = !!vCourante

    return (
        <div style={{ fontFamily: sans, minWidth: 750, maxWidth: 800,  }}>
            <h2 style={{ margin: '0 0 6px', color: '#0f1e3d', fontWeight:650 }}>{quiz.titre}</h2>
            <p style={{ margin: '0 0 18px', color: '#64748b', fontSize: 13 }}>
                Question {indexCourant + 1} / {quiz.questions.length}
            </p>

            <div style={{ border: '1px solid #e2e8f4', borderRadius: 10, padding: "35px 30px", background: '#fff', width: '100%' }}>
                <p style={{ margin: '0 0 13px', color: '#0f1e3d', fontWeight: 500 }}>{questionCourante.enonce}</p>
                <div style={{ display: 'grid', gap: 9 }} role="radiogroup" aria-label="Réponses possibles">
                    {questionCourante.reponses.map((r) => {
                        const checked = choix[questionCourante.id] === r.id
                        const st = styleOption(questionCourante, r)
                        return (
                            <label
                                key={r.id}
                                style={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    gap: 8,
                                    border: st.border,
                                    background: st.background,
                                    color: st.color,
                                    borderRadius: 8,
                                    padding: '10px ',
                                    cursor: verrouillee ? 'default' : 'pointer',
                                }}
                            >
                                <input
                                    type="radio"
                                    name={`question-${questionCourante.id}`}
                                    checked={checked}
                                    disabled={verrouillee}
                                    onChange={() =>
                                        setChoix((prev) => ({
                                            ...prev,
                                            [questionCourante.id]: r.id,
                                        }))
                                    }
                                />
                                <span>{r.contenu}</span>
                            </label>
                        )
                    })}
                </div>

                {vCourante ? (
                    <p
                        role="status"
                        style={{
                            margin: '14px 0 0',
                            fontSize: 14,
                            fontWeight: 600,
                            color: vCourante.correcte ? '#166534' : '#b91c1c',
                        }}
                    >
                        {vCourante.correcte ? 'Réponse correcte.' : 'Réponse incorrecte.'}
                    </p>
                ) : null}
            </div>

            {err ? <p style={{ color: '#b91c1c', marginTop: 10 }}>{err}</p> : null}

            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 14, gap: 8 }}>
                <button
                    type="button"
                    disabled={indexCourant === 0}
                    onClick={() => setIndexCourant((i) => Math.max(0, i - 1))}
                    style={{
                        height: 30,
                        borderRadius: 8,
                        border: '1px solid #d1d5db',
                        padding: '0 14px',
                        fontSize : 14,
                        cursor: indexCourant === 0 ? 'not-allowed' : 'pointer',
                        opacity: indexCourant === 0 ? 0.5 : 1,
                    }}
                >
                    Précédent
                </button>

                {!vCourante ? (
                    <button
                        type="button"
                        disabled={!choix[questionCourante.id] || verifying}
                        onClick={verifier}
                        style={{
                            height: 30,
                            borderRadius: 8,
                            border: 'none',
                            padding: '0 14px',
                            cursor: 'pointer',
                            background: '#0f1e3d',
                            color: '#fff',
                            fontSize : 14,
                            opacity: !choix[questionCourante.id] || verifying ? 0.55 : 1,
                        }}
                    >
                        {verifying ? 'Vérification…' : 'Vérifier'}
                    </button>
                ) : !derniereQuestion ? (
                    <button
                        type="button"
                        onClick={() =>
                            setIndexCourant((i) => Math.min(quiz.questions.length - 1, i + 1))
                        }
                        style={{
                            height: 30,
                            borderRadius: 8,
                            border: 'none',
                            padding: '0 14px',
                            cursor: 'pointer',
                            background: '#0f1e3d',
                            color: '#fff',
                            fontSize : 14,
                        }}
                    >
                        Suivant
                    </button>
                ) : (
                    <button
                        type="button"
                        disabled={!peutSoumettre || submitting}
                        onClick={soumettre}
                        style={{
                            height: 30,
                            borderRadius: 8,
                            border: 'none',
                            padding: '0 14px',
                            cursor: 'pointer',
                            background: '#0f1e3d',
                            color: '#fff',
                            fontSize : 14,
                            opacity: !peutSoumettre || submitting ? 0.55 : 1,
                        }}
                    >
                        {submitting ? 'Soumission…' : 'Soumettre'}
                    </button>
                )}
            </div>
        </div>
    )
}
