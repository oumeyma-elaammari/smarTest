import type { CSSProperties } from 'react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import axios from 'axios'
import type {
    QuestionDto,
    QrLiveSubmitAnswerResponseDto,
    StreamEnvelope,
} from '../types/quizLive'
import { shuffleCopy } from '../utils/shuffle'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:8081'

/** Aligné sur QuizPassageWeb (`styleOption`) */
const QP = {
    borderDefault: '#e2e8f4',
    bgDefault: '#ffffff',
    textDefault: '#0f1e3d',
    borderSelected: '#4f8ef7',
    bgSelected: '#eef3fd',
    borderCorrect: '#22c55e',
    bgCorrect: '#f0fdf4',
    textCorrect: '#166534',
    borderWrong: '#ef4444',
    bgWrong: '#fef2f2',
    textWrong: '#b91c1c',
    borderDimmed: '#e8eef7',
    bgDimmed: '#f8fafc',
    textDimmed: '#64748b',
}

type UiQuestion = {
    id: number
    enonce: string
    options: { id: number; texte: string }[]
}

function buildSecureId(prefix: string): string {
    const webCrypto = globalThis.crypto
    if (webCrypto?.randomUUID) return `${prefix}-${webCrypto.randomUUID()}`
    if (webCrypto?.getRandomValues) {
        const bytes = new Uint8Array(16)
        webCrypto.getRandomValues(bytes)
        const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('')
        return `${prefix}-${hex}`
    }
    return `${prefix}-${Date.now()}`
}

function participantIdPourSession(sessionTok: string): string {
    const key = `qr-live-participant-${sessionTok}`
    try {
        let id = sessionStorage.getItem(key)
        if (!id) {
            id = buildSecureId('p')
            sessionStorage.setItem(key, id)
        }
        return id
    } catch {
        return buildSecureId('p')
    }
}

function mapQuestions(list: QuestionDto[]): UiQuestion[] {
    return list.map((q) => ({
        id: q.id,
        enonce: q.enonce,
        options: q.reponses.map((r) => ({ id: r.id, texte: r.contenu })),
    }))
}

/** Persistance du mélange par participant / session (les snapshots QR se répètent ~10 s). */
type QrPresentationState = {
    questionIds: number[]
    optionOrderByQuestion: Record<string, number[]>
}

function presentationStorageKey(sessionTok: string): string {
    return `smartest.qr-live.presentation.${sessionTok}`
}

function sameQuestionSet(mapped: UiQuestion[], prev: QrPresentationState): boolean {
    if (mapped.length !== prev.questionIds.length) return false
    const have = new Set(mapped.map((q) => q.id))
    if (have.size !== prev.questionIds.length) return false
    for (const id of prev.questionIds) {
        if (!have.has(id)) return false
    }
    return true
}

function applyStoredPresentation(mapped: UiQuestion[], prev: QrPresentationState): UiQuestion[] {
    const byId = new Map(mapped.map((q) => [q.id, q]))
    const out: UiQuestion[] = []
    for (const qid of prev.questionIds) {
        const q = byId.get(qid)
        if (!q) return []
        const order = prev.optionOrderByQuestion[String(qid)]
        const optById = new Map(q.options.map((o) => [o.id, o]))
        let options: UiQuestion['options']
        if (order?.length) {
            const reordered = order.map((oid) => optById.get(oid)).filter(Boolean) as UiQuestion['options']
            options =
                reordered.length === q.options.length ? reordered : shuffleCopy(q.options)
        } else {
            options = shuffleCopy(q.options)
        }
        out.push({ ...q, options })
    }
    return out
}

/**
 * Ordre des questions et des réponses mélangés pour chaque participant ; stable tant que la liste des questions ne change pas.
 */
function buildQrPresentation(dtos: QuestionDto[], sessionTok: string): UiQuestion[] {
    const mapped = mapQuestions(dtos)
    if (mapped.length === 0) return []

    const key = presentationStorageKey(sessionTok)
    try {
        const raw = sessionStorage.getItem(key)
        if (raw) {
            const prev = JSON.parse(raw) as QrPresentationState
            if (sameQuestionSet(mapped, prev)) {
                const applied = applyStoredPresentation(mapped, prev)
                if (applied.length === mapped.length) return applied
            }
        }
    } catch {
        /* sessionStorage indisponible ou JSON invalide */
    }

    const questionIds = shuffleCopy(mapped.map((q) => q.id))
    const byId = new Map(mapped.map((q) => [q.id, q]))
    const optionOrderByQuestion: Record<string, number[]> = {}
    const out: UiQuestion[] = []
    for (const qid of questionIds) {
        const q = byId.get(qid)!
        const optIds = shuffleCopy(q.options.map((o) => o.id))
        optionOrderByQuestion[String(qid)] = optIds
        const optById = new Map(q.options.map((o) => [o.id, o]))
        out.push({
            ...q,
            options: optIds.map((oid) => optById.get(oid)!),
        })
    }
    try {
        sessionStorage.setItem(
            key,
            JSON.stringify({ questionIds, optionOrderByQuestion } satisfies QrPresentationState),
        )
    } catch {
        /* ignore */
    }
    return out
}

export default function QuizLiveParticiper() {
    const { sessionToken } = useParams<{ sessionToken: string }>()
    const token = sessionToken?.trim() ?? ''

    const [phase, setPhase] = useState<
        | 'loading'
        | 'missing'
        | 'waiting'
        | 'quiz'
        | 'scoreFinal'
        | 'sessionInterrupted'
        | 'closed'
    >('loading')

    const [titre, setTitre] = useState('')
    const [questions, setQuestions] = useState<UiQuestion[]>([])
    const [idxCourant, setIdxCourant] = useState(0)
    const [selection, setSelection] = useState<number | null>(null)
    /** Après POST : feedback pour colorer les options */
    const [feedback, setFeedback] = useState<{
        correct: boolean
        bonneReponseId: number
        chosenId: number
    } | null>(null)

    const [bonnesCount, setBonnesCount] = useState(0)
    const [reponsesSoumises, setReponsesSoumises] = useState(0)

    const [envoiEnCours, setEnvoiEnCours] = useState(false)
    const [erreurAction, setErreurAction] = useState<string | null>(null)

    const [isMobile, setIsMobile] = useState(false)

    const participantId = useMemo(() => (token ? participantIdPourSession(token) : ''), [token])

    useEffect(() => {
        const mq = globalThis.window.matchMedia('(max-width: 767px)')
        const sync = () => setIsMobile(mq.matches)
        sync()
        mq.addEventListener('change', sync)
        return () => mq.removeEventListener('change', sync)
    }, [])

    const appliquerSnapshot = useCallback((data: StreamEnvelope) => {
        if (data.type === 'CLOSED') {
            try {
                sessionStorage.removeItem(presentationStorageKey(token))
            } catch {
                /* ignore */
            }
            setPhase((prev) => (prev === 'quiz' ? 'sessionInterrupted' : 'closed'))
            return
        }
        const q = data.quiz
        if (q?.questions?.length) {
            setTitre(q.titre?.trim() || 'Quiz')
            setQuestions(buildQrPresentation(q.questions, token))
            setPhase('quiz')
            return
        }
        setPhase('waiting')
        if (data.stats?.quizTitre?.trim()) setTitre(data.stats.quizTitre.trim())
    }, [token])

    const chargerSnapshot = useCallback(async () => {
        if (!token) return
        try {
            const { data } = await axios.get<StreamEnvelope>(
                `${API_BASE}/api/qr-live/public/${encodeURIComponent(token)}/snapshot`,
            )
            appliquerSnapshot(data)
            if (data.type !== 'CLOSED' && !data.quiz?.questions?.length) {
                setPhase((p) => (p === 'quiz' ? p : 'waiting'))
            }
        } catch (e: unknown) {
            if (axios.isAxiosError(e) && e.response?.status === 404) {
                setPhase((p) => (p === 'quiz' ? 'sessionInterrupted' : 'missing'))
            }
        }
    }, [token, appliquerSnapshot])

    useEffect(() => {
        if (!token) return
        let cancelled = false
        setPhase('loading')
        ;(async () => {
            try {
                const { data } = await axios.get<StreamEnvelope>(
                    `${API_BASE}/api/qr-live/public/${encodeURIComponent(token)}/snapshot`,
                )
                if (cancelled) return
                appliquerSnapshot(data)
                if (data.type !== 'CLOSED' && !data.quiz?.questions?.length) {
                    setPhase('waiting')
                }
            } catch {
                if (!cancelled) setPhase('missing')
            }
        })()
        return () => {
            cancelled = true
        }
    }, [token, appliquerSnapshot])

    useEffect(() => {
        if (phase !== 'waiting' && phase !== 'quiz') return
        const id = globalThis.setInterval(() => {
            Promise.resolve(chargerSnapshot()).catch(() => {})
        }, 10000)
        return () => globalThis.clearInterval(id)
    }, [phase, chargerSnapshot])

    const questionCourante = questions[idxCourant]
    const n = questions.length
    const numQuestion = idxCourant + 1
    const pctBarre = n > 0 ? (numQuestion / n) * 100 : 0

    const soumettre = async () => {
        if (!token || !questionCourante || selection == null || envoiEnCours || feedback != null) return
        setEnvoiEnCours(true)
        setErreurAction(null)
        try {
            const res = await axios.post<QrLiveSubmitAnswerResponseDto>(
                `${API_BASE}/api/qr-live/sessions/${encodeURIComponent(token)}/reponses`,
                {
                    participantId,
                    correlationId: buildSecureId('c'),
                    questionId: questionCourante.id,
                    reponseId: selection,
                },
                { validateStatus: (s) => [200, 404, 410].includes(s) },
            )
            if (res.status === 404 || res.status === 410) {
                setPhase('sessionInterrupted')
                setEnvoiEnCours(false)
                return
            }
            const { correct, bonneReponseId } = res.data
            setFeedback({
                correct,
                bonneReponseId,
                chosenId: selection,
            })
            setReponsesSoumises((c) => c + 1)
            if (correct) setBonnesCount((c) => c + 1)
        } catch {
            setErreurAction('Envoi impossible. Réessayez.')
        } finally {
            setEnvoiEnCours(false)
        }
    }

    const questionSuivanteOuScore = () => {
        setFeedback(null)
        setSelection(null)
        setErreurAction(null)
        if (idxCourant >= n - 1) {
            setPhase('scoreFinal')
            return
        }
        setIdxCourant((i) => i + 1)
    }

    const pageShellStyle = useMemo(
        (): CSSProperties => ({
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'flex-start',
            minHeight: '100vh',
            padding: '32px 16px',
            boxSizing: 'border-box',
            width: '100%',
            fontFamily: 'system-ui, -apple-system, sans-serif',
            background: 'var(--color-background-secondary, #f8fafc)',
            color: 'var(--color-text-primary, #0f172a)',
        }),
        [],
    )

    const cardStyle = useMemo((): CSSProperties => {
        const pad = isMobile ? '20px 20px' : '32px 40px'
        return {
            width: '100%',
            maxWidth: 720,
            margin: '0 auto',
            background: 'var(--color-background-primary, #ffffff)',
            border: '0.5px solid var(--color-border-tertiary, #e2e8f0)',
            borderRadius: 'var(--border-radius-lg, 12px)',
            padding: pad,
            boxSizing: 'border-box',
        }
    }, [isMobile])

    const scorePct =
        n > 0 ? Math.round((bonnesCount / n) * 100) : reponsesSoumises > 0
          ? Math.round((bonnesCount / reponsesSoumises) * 100)
          : 0

    const scorePalette =
        scorePct >= 60
            ? {
                  scoreColor: '#185FA5',
                  messageColor: '#475569',
                  message: 'Félicitations ! Bon travail.',
              }
            : {
                  scoreColor: 'var(--color-destructive)',
                  messageColor: 'var(--color-muted-foreground)',
                  message:
                      'Merci pour votre participation.\nContinuez à vous entraîner !',
              }

    if (!token) {
        return (
            <div style={pageShellStyle}>
                <p>Lien invalide.</p>
            </div>
        )
    }

    if (phase === 'loading') {
        return (
            <div style={pageShellStyle}>
                <p style={{ color: '#64748b' }}>Chargement…</p>
            </div>
        )
    }

    if (phase === 'missing') {
        return (
            <div style={pageShellStyle}>
                <div style={{ ...cardStyle, maxWidth: 560 }}>
                    <h1 style={{ fontSize: '1.15rem', fontWeight: 500, marginTop: 0 }}>
                        Session introuvable ou terminée
                    </h1>
                    <p style={{ color: '#64748b' }}>
                        Cette session QR n&apos;est plus disponible ou a été fermée par le professeur.
                    </p>
                </div>
            </div>
        )
    }

    if (phase === 'closed') {
        return (
            <div style={pageShellStyle}>
                <div style={{ ...cardStyle, maxWidth: 560 }}>
                    <h1 style={{ fontSize: '1.15rem', fontWeight: 500, marginTop: 0 }}>
                        La session a été fermée par le professeur.
                    </h1>
                </div>
            </div>
        )
    }

    if (phase === 'waiting') {
        return (
            <div style={pageShellStyle}>
                <div style={{ width: '100%', maxWidth: 720 }}>
                    <h1 style={{ fontSize: 22, fontWeight: 500, margin: '0 0 16px', textAlign: 'center' }}>
                        {titre || 'Quiz en direct'}
                    </h1>
                    <p style={{ color: '#64748b', textAlign: 'center' }}>
                        En attente du lancement du quiz par le professeur…
                    </p>
                    <p style={{ fontSize: 13, color: '#94a3b8', textAlign: 'center' }}>
                        Cette page se met à jour automatiquement.
                    </p>
                </div>
            </div>
        )
    }

    if (phase === 'sessionInterrupted') {
        const partialPct =
            n > 0 && reponsesSoumises > 0 ? Math.round((bonnesCount / n) * 100) : null
        return (
            <div style={pageShellStyle}>
                <div style={{ ...cardStyle, maxWidth: 480, textAlign: 'center' }}>
                    <h1 style={{ fontSize: '1.1rem', fontWeight: 500, marginTop: 0 }}>
                        La session a été clôturée par le professeur.
                    </h1>
                    {reponsesSoumises > 0 ? (
                        <>
                            <p style={{ margin: '16px 0 8px', color: '#64748b', fontSize: 14 }}>
                                Score partiel ({bonnesCount} / {n} bonnes réponses)
                            </p>
                            <p style={{ fontSize: 36, fontWeight: 500, margin: 0, color: '#0f172a' }}>
                                {partialPct != null ? `${partialPct}%` : '—'}
                            </p>
                        </>
                    ) : null}
                </div>
            </div>
        )
    }

    if (phase === 'scoreFinal') {
        return (
            <div
                style={{
                    ...pageShellStyle,
                    justifyContent: 'center',
                    background: 'var(--color-background, var(--background))',
                }}
            >
                <div
                    style={{
                        width: '100%',
                        maxWidth: 480,
                        margin: '0 auto',
                        padding: 0,
                        boxSizing: 'border-box',
                        textAlign: 'center',
                    }}
                >
                    <p
                        style={{
                            margin: '0 0 12px',
                            fontSize: 15,
                            color: 'var(--color-muted-foreground)',
                            fontWeight: 500,
                        }}
                    >
                        Votre score
                    </p>
                    <p
                        style={{
                            margin: '8px 0',
                            fontSize: 48,
                            fontWeight: 500,
                            color: scorePalette.scoreColor,
                            lineHeight: 1.1,
                        }}
                    >
                        {bonnesCount} / {n}
                    </p>
                    <p
                        style={{
                            margin: '4px 0 20px',
                            fontSize: 24,
                            fontWeight: 500,
                            color: scorePalette.scoreColor,
                        }}
                    >
                        {scorePct}%
                    </p>
                    <p
                        style={{
                            margin: 0,
                            fontSize: 14,
                            color: scorePalette.messageColor,
                            lineHeight: 1.5,
                            whiteSpace: 'pre-line',
                        }}
                    >
                        {scorePalette.message}
                    </p>
                </div>
            </div>
        )
    }

    const optPad = isMobile ? '12px 16px' : '14px 20px'
    const optFs = isMobile ? 14 : 15
    const btnPad = isMobile ? '12px' : '14px 32px'
    const btnFs = isMobile ? 14 : 15

    return (
        <div style={pageShellStyle}>
            <header
                style={{
                    width: '100%',
                    maxWidth: 720,
                    marginBottom: 20,
                }}
            >
                <h1
                    style={{
                        fontSize: 22,
                        fontWeight: 500,
                        margin: '0 0 12px',
                        textAlign: 'center',
                        color: 'var(--color-text-primary, #0f172a)',
                    }}
                >
                    {titre}
                </h1>
                <p style={{ margin: '0 0 8px', fontSize: 13, color: '#64748b', textAlign: 'center' }}>
                    Question {numQuestion} sur {n}
                </p>
                <div
                    style={{
                        height: 4,
                        width: '100%',
                        background: '#e2e8f0',
                        borderRadius: 2,
                        overflow: 'hidden',
                    }}
                >
                    <div
                        style={{
                            height: '100%',
                            width: `${pctBarre}%`,
                            background: '#534AB7',
                            transition: 'width 0.25s ease',
                        }}
                    />
                </div>
            </header>

            {questionCourante ? (
                <section style={cardStyle}>
                    <p style={{ margin: '0 0 20px', fontSize: 13, color: '#64748b' }}>
                        Question {numQuestion} sur {n}
                    </p>
                    <p
                        style={{
                            margin: '0 0 24px',
                            fontSize: 16,
                            fontWeight: 500,
                            lineHeight: 1.45,
                            color: 'var(--color-text-primary, #0f172a)',
                        }}
                    >
                        {questionCourante.enonce}
                    </p>

                    <div style={{ marginBottom: 8 }}>
                        {questionCourante.options.map((o) => {
                            const choisi = selection === o.id
                            const apres = feedback != null
                            const bonne = o.id === feedback?.bonneReponseId
                            const mauvaisChoix =
                                apres && choisi && feedback && !feedback.correct && o.id === feedback.chosenId

                            let bg = QP.bgDefault
                            let borderColor = QP.borderDefault
                            let color = QP.textDefault

                            if (!apres) {
                                if (choisi) {
                                    borderColor = QP.borderSelected
                                    bg = QP.bgSelected
                                    color = QP.textDefault
                                }
                            } else if (bonne) {
                                borderColor = QP.borderCorrect
                                bg = QP.bgCorrect
                                color = QP.textCorrect
                            } else if (mauvaisChoix) {
                                borderColor = QP.borderWrong
                                bg = QP.bgWrong
                                color = QP.textWrong
                            } else {
                                borderColor = QP.borderDimmed
                                bg = QP.bgDimmed
                                color = QP.textDimmed
                            }

                            return (
                                <button
                                    key={o.id}
                                    type="button"
                                    disabled={apres}
                                    onClick={() => {
                                        if (!apres) setSelection(o.id)
                                    }}
                                    style={{
                                        width: '100%',
                                        padding: optPad,
                                        border: `1px solid ${borderColor}`,
                                        borderRadius: 'var(--border-radius-md, 10px)',
                                        textAlign: 'left',
                                        fontSize: optFs,
                                        cursor: apres ? 'default' : 'pointer',
                                        marginBottom: 10,
                                        background: bg,
                                        color,
                                        transition: 'border-color 0.15s, background 0.15s, color 0.15s',
                                        boxSizing: 'border-box',
                                        fontWeight: 400,
                                    }}
                                    onMouseEnter={(e) => {
                                        if (apres || choisi) return
                                        e.currentTarget.style.borderColor = QP.borderSelected
                                        e.currentTarget.style.background = QP.bgSelected
                                    }}
                                    onMouseLeave={(e) => {
                                        if (apres || choisi) return
                                        e.currentTarget.style.borderColor = QP.borderDefault
                                        e.currentTarget.style.background = QP.bgDefault
                                    }}
                                >
                                    {o.texte}
                                </button>
                            )
                        })}
                    </div>

                    {feedback ? (
                        <div
                            style={{
                                marginTop: 4,
                                marginBottom: 16,
                                padding: '10px 16px',
                                borderRadius: 'var(--border-radius-md, 10px)',
                                background: feedback.correct ? QP.bgCorrect : QP.bgWrong,
                                color: feedback.correct ? QP.textCorrect : QP.textWrong,
                                fontSize: 14,
                                fontWeight: 600,
                            }}
                        >
                            {feedback.correct ? 'Réponse correcte.' : 'Réponse incorrecte.'}
                        </div>
                    ) : null}

                    {erreurAction ? (
                        <p role="alert" style={{ color: '#b91c1c', marginBottom: 12, fontSize: 14 }}>
                            {erreurAction}
                        </p>
                    ) : null}

                    {feedback ? (
                        <button
                            type="button"
                            onClick={questionSuivanteOuScore}
                            style={{
                                width: '100%',
                                padding: btnPad,
                                fontSize: btnFs,
                                fontWeight: 500,
                                border: 'none',
                                borderRadius: 'var(--border-radius-md, 10px)',
                                background: feedback.correct ? QP.borderCorrect : QP.borderWrong,
                                color: '#fff',
                                cursor: 'pointer',
                                marginTop: 8,
                                display: 'inline-flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                gap: 8,
                                boxSizing: 'border-box',
                            }}
                        >
                            {feedback.correct ? null : (
                                <i className="ti ti-arrows-maximize" aria-hidden style={{ fontSize: btnFs }} />
                            )}
                            {idxCourant >= n - 1 ? 'Voir mon score' : 'Question suivante →'}
                        </button>
                    ) : (
                        <button
                            type="button"
                            disabled={selection == null || envoiEnCours}
                            onClick={() => {
                                Promise.resolve(soumettre()).catch(() => {})
                            }}
                            style={{
                                width: '100%',
                                padding: btnPad,
                                fontSize: btnFs,
                                fontWeight: 500,
                                border: 'none',
                                borderRadius: 'var(--border-radius-md, 10px)',
                                background:
                                    selection == null || envoiEnCours ? '#94a3b8' : '#534AB7',
                                color: '#EEEDFE',
                                cursor: selection == null || envoiEnCours ? 'default' : 'pointer',
                                marginTop: 8,
                            }}
                        >
                            {envoiEnCours ? 'Envoi…' : 'Soumettre'}
                        </button>
                    )}
                </section>
            ) : null}
        </div>
    )
}
