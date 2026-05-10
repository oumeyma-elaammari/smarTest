import { useCallback, useEffect, useMemo, useState } from 'react'
import type { CSSProperties, ReactNode } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import axios from 'axios'
import { Client } from '@stomp/stompjs'
import {
    CalendarClock,
    ChevronLeft,
    ChevronRight,
    Loader2,
    Pause,
    Play,
    Radio,
    Settings2,
    Square,
    Timer,
    Users,
} from 'lucide-react'
import { examenApi } from '../api/examenApi'
import useAuth from '../hooks/useAuth'
import { useExamenTempsRestantLive } from '../hooks/useExamenTempsRestantLive'
import type { ExamenMeta, ExamenSnapshot } from '../api/quizSchemas'

const sans = "'DM Sans', system-ui, sans-serif"
const serif = "'DM Serif Display', Georgia, serif"
const WS_BASE_URL = import.meta.env.VITE_WS_BASE_URL || 'ws://localhost:8081/ws'

export type ExamenSupervisionPageProps = {
    /** Aligné sur le thème web (ex. `#4f8ef7` dans `App.tsx`). */
    readonly accentBleu?: string
}

function supervisionFeedbackBannerStyles(
    tone: 'neutral' | 'success' | 'error',
): { bg: string; border: string; fg: string } {
    if (tone === 'success') {
        return { bg: '#ecfdf5', border: '#bbf7d0', fg: '#166534' }
    }
    if (tone === 'error') {
        return { bg: '#fef2f2', border: '#fecaca', fg: '#b91c1c' }
    }
    return { bg: '#f8fafc', border: '#e2e8f0', fg: '#475569' }
}

function modeListeParticipantsFromEtat(etat: string): 'attente' | 'actifs' | 'terminee' {
    if (etat === 'PLANIFIE') return 'attente'
    if (etat === 'EN_COURS' || etat === 'EN_PAUSE') return 'actifs'
    return 'terminee'
}

function formatDateTime(value?: string): string {
    if (!value) return '-'
    const d = new Date(value)
    if (Number.isNaN(d.getTime())) return value
    return d.toLocaleString('fr-FR', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
    })
}

/** Titre affiché dans l’interface : métadonnée en priorité, puis snapshot supervision. */
function titrePrincipal(meta: ExamenMeta | null, snap: ExamenSnapshot | null, examId: number): string {
    const m = typeof meta?.titre === 'string' ? meta.titre.trim() : ''
    const s = typeof snap?.titre === 'string' ? snap.titre.trim() : ''
    if (m.length > 0) return m
    if (s.length > 0) return s
    return `Examen #${examId}`
}

function extractApiMessage(error: unknown, fallback: string): string {
    if (axios.isAxiosError(error)) {
        const data = error.response?.data
        if (typeof data === 'string' && data.trim()) {
            return data.trim()
        }
        if (data && typeof data === 'object' && 'message' in data) {
            const message = (data as { message?: unknown }).message
            if (typeof message === 'string' && message.trim()) {
                return message.trim()
            }
        }
    }
    if (error instanceof Error && error.message.trim()) {
        return error.message.trim()
    }
    return fallback
}

/** GET `/salle-attente` ou message WS `/topic/examen/:id/salle-attente` (même JSON que le backend). */
function connectesLabelsFromSalleAttentePayload(raw: unknown): string[] {
    if (raw === null || typeof raw !== 'object') return []
    const r = raw as {
        connectes?: { email?: string; etudiantId?: number }[]
        Connectes?: { email?: string; etudiantId?: number }[]
    }
    const list = r.connectes ?? r.Connectes ?? []
    return list.map((p) => {
        const mail = (p.email ?? '').trim()
        return mail || 'Étudiant'
    })
}

/** Réponse Axios (`status` + `data`), sans `axios.isAxiosResponse` (pas toujours exposé par le bundle). */
function snapshotBodyFromMaybeAxiosResponse(raw: unknown): Partial<ExamenSnapshot> | null {
    if (raw === null || typeof raw !== 'object') return null
    const r = raw as { status?: unknown; data?: unknown }
    if (typeof r.status !== 'number') return null
    const body = r.data
    if (body === null || typeof body !== 'object') return null
    const etat = (body as { etat?: unknown }).etat
    if (typeof etat !== 'string') return null
    return body as Partial<ExamenSnapshot>
}

export default function ExamenSupervisionPage({ accentBleu = '#4f8ef7' }: ExamenSupervisionPageProps) {
    const { examenId } = useParams()
    const navigate = useNavigate()
    const [searchParams] = useSearchParams()
    const isProf = (useAuth((s) => s.role) ?? '').trim().toUpperCase() === 'PROFESSEUR'
    const [meta, setMeta] = useState<ExamenMeta | null>(null)
    const [snap, setSnap] = useState<ExamenSnapshot | null>(null)
    const [feedback, setFeedback] = useState('')
    const [feedbackTone, setFeedbackTone] = useState<'neutral' | 'success' | 'error'>('neutral')
    const [isSubmitting, setIsSubmitting] = useState(false)
    const [wsNotice, setWsNotice] = useState<string | null>(null)
    const [connectesLabels, setConnectesLabels] = useState<string[]>([])
    const [pageReady, setPageReady] = useState(false)
    const id = Number(examenId)

    const shellCard = useMemo(
        (): CSSProperties => ({
            background: '#fff',
            border: '1px solid #e2e8f4',
            borderRadius: 12,
            padding: '1.1rem 1.25rem',
            boxShadow: '0 1px 2px rgba(15, 30, 61, 0.04)',
            position: 'relative',
            overflow: 'hidden',
            boxSizing: 'border-box',
            width: '100%',
        }),
        [],
    )

    const btnBase = useMemo(
        (): CSSProperties => ({
            height: 38,
            borderRadius: 8,
            padding: '0 14px',
            fontFamily: sans,
            fontWeight: 600,
            fontSize: 13,
            cursor: 'pointer',
            transition: 'background 0.15s, border-color 0.15s, opacity 0.15s',
        }),
        [],
    )

    /** Fusionne un snapshot incomplet sans écraser l’état (évite faux « encore en cours » après pause si le WS perd un champ). */
    const mergeSnapshot = useCallback((prev: ExamenSnapshot | null, incoming: Partial<ExamenSnapshot>): ExamenSnapshot => {
        const merged = { ...(prev ?? {}), ...incoming } as ExamenSnapshot
        const t = incoming.titre
        const trimmed = typeof t === 'string' ? t.trim() : ''
        if (trimmed.length === 0 && prev?.titre != null) {
            merged.titre = prev.titre
        }
        const idxIn = incoming.questionCouranteIndex
        const idxPrev = prev?.questionCouranteIndex
        const sameQuestion =
            idxIn === undefined ? true : idxPrev === undefined ? true : idxIn === idxPrev
        if (
            (incoming.questionCourante === undefined || incoming.questionCourante === null) &&
            prev?.questionCourante != null &&
            sameQuestion
        ) {
            merged.questionCourante = prev.questionCourante
        }
        return merged
    }, [])

    const refresh = useCallback(async () => {
        if (!Number.isFinite(id) || id <= 0) return
        const settled = await Promise.allSettled([
            examenApi.getMetadata(id),
            examenApi.snapshot(id),
            examenApi.getSalleAttente(id),
        ])
        const [mr, sr, rr] = settled
        if (mr.status === 'fulfilled') {
            setMeta(mr.value.data)
        }
        if (sr.status === 'fulfilled') {
            const sd = sr.value.data
            setSnap((prev) => mergeSnapshot(prev, sd))
        }
        if (rr.status === 'fulfilled') {
            setConnectesLabels(connectesLabelsFromSalleAttentePayload(rr.value.data))
        }
    }, [id, mergeSnapshot])

    useEffect(() => {
        if (!Number.isFinite(id) || id <= 0) {
            setPageReady(true)
            return
        }
        let cancelled = false
        refresh()
            .catch(() => undefined)
            .finally(() => {
                if (!cancelled) setPageReady(true)
            })
        const t = globalThis.setInterval(() => refresh().catch(() => undefined), 2500)
        return () => {
            cancelled = true
            globalThis.clearInterval(t)
        }
    }, [id, refresh])

    const sessionDemarreeHint = searchParams.get('started')
    useEffect(() => {
        if (sessionDemarreeHint !== '1') return
        setFeedbackTone('neutral')
        setFeedback(
            'Session lancée pour les étudiants : ils passent sur l’écran d’épreuve quand la phase est « En cours » (même flux que la question active ci-dessous).',
        )
        globalThis.scrollTo({ top: 0, behavior: 'smooth' })
        const t = globalThis.setTimeout(() => {
            navigate(`/supervision/examen/${id}`, { replace: true })
        }, 6500)
        return () => globalThis.clearTimeout(t)
    }, [sessionDemarreeHint, id, navigate])

    useEffect(() => {
        if (!Number.isFinite(id) || id <= 0) return

        let cancelled = false
        const client = new Client({
            brokerURL: WS_BASE_URL,
            reconnectDelay: 3000,
            onConnect: () => {
                client.subscribe(`/topic/examen/${id}/etat`, (message) => {
                    try {
                        const data = JSON.parse(message.body) as Partial<ExamenSnapshot>
                        if (cancelled) return
                        setSnap((prev) => mergeSnapshot(prev, data))
                        setWsNotice(null)
                    } catch {
                        if (!cancelled) setWsNotice('Message temps réel invalide (supervision examen).')
                    }
                })
                client.subscribe(`/topic/examen/${id}/salle-attente`, (message) => {
                    try {
                        const payload = JSON.parse(message.body) as unknown
                        if (cancelled) return
                        setConnectesLabels(connectesLabelsFromSalleAttentePayload(payload))
                        setWsNotice(null)
                    } catch {
                        // ignoré : le polling GET salle-attente garde un fallback
                    }
                })
            },
            onWebSocketClose: () => {
                if (!cancelled) setWsNotice('Connexion temps réel fermée. Bascule sur rafraîchissement automatique.')
            },
            onWebSocketError: () => {
                if (!cancelled) setWsNotice('Erreur WebSocket. Bascule sur rafraîchissement automatique.')
            },
            onStompError: () => {
                if (!cancelled) setWsNotice('Erreur STOMP. Bascule sur rafraîchissement automatique.')
            },
        })

        client.activate()
        return () => {
            cancelled = true
            client.deactivate()
        }
    }, [id, mergeSnapshot])

    const supervisionEnPause =
        ((snap?.etat ?? meta?.statut ?? '').trim().toUpperCase() === 'EN_PAUSE') || !!snap?.enPause
    const tempsRestantSupervision = useExamenTempsRestantLive(
        snap?.tempsRestantMinutes,
        snap?.etat ?? meta?.statut ?? '',
        supervisionEnPause,
    )

    const action = async (run: () => Promise<unknown>) => {
        try {
            setIsSubmitting(true)
            const raw = await run()
            const patchBody = snapshotBodyFromMaybeAxiosResponse(raw)
            if (patchBody != null) {
                setSnap((prev) => mergeSnapshot(prev, patchBody))
            }
            await refresh()
            setFeedbackTone('success')
            setFeedback('Action appliquée avec succès.')
        } catch (error: unknown) {
            setFeedbackTone('error')
            setFeedback(extractApiMessage(error, 'Action impossible. Vérifiez l’état actuel de l’examen.'))
        } finally {
            setIsSubmitting(false)
        }
    }

    if (!Number.isFinite(id) || id <= 0) {
        return (
            <p style={{ fontFamily: sans, color: '#64748b', margin: 0 }}>Examen invalide.</p>
        )
    }

    if (!pageReady) {
        return (
            <output
                style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    gap: 10,
                    fontFamily: sans,
                    color: '#64748b',
                    width: '100%',
                    padding: '3rem 0',
                }}
                aria-busy="true"
                aria-live="polite"
            >
                <Loader2 size={22} className="animate-spin" aria-hidden style={{ color: accentBleu }} />
                Chargement de la supervision…
            </output>
        )
    }

    const totalQuestions = Math.max(0, snap?.totalQuestions ?? meta?.totalQuestions ?? 0)
    const currentIndex = Math.max(0, snap?.questionCouranteIndex ?? 0)
    const questionNumero = totalQuestions > 0 ? Math.min(currentIndex + 1, totalQuestions) : 0
    const etat = (snap?.etat ?? meta?.statut ?? 'PLANIFIE').toUpperCase()
    /** Avant lancement : liste d’attente. Pendant l’épreuve : participants actifs (même API, libellés différents). */
    const modeListeParticipants = modeListeParticipantsFromEtat(etat)
    const estEnCours = etat === 'EN_COURS'
    const peutLancer = etat === 'PLANIFIE' || etat === 'EN_PAUSE'
    const peutPause = etat === 'EN_COURS'
    const peutReprendre = etat === 'EN_PAUSE'
    const peutTerminer = etat !== 'TERMINE' && etat !== 'ARRETE'
    const questionPills = totalQuestions > 0 ? Array.from({ length: totalQuestions }, (_, i) => i + 1) : []

    const questionCouranteBloc = snap?.questionCourante as
        | { id?: number; enonce?: string; reponses?: Array<{ id?: number; contenu?: string }> }
        | undefined
    const reponsesPilotage = Array.isArray(questionCouranteBloc?.reponses) ? questionCouranteBloc.reponses : []
    const planListe = snap?.planQuestions ?? []

    const feedbackBanner = supervisionFeedbackBannerStyles(feedbackTone)

    const stripeLeft: CSSProperties = {
        position: 'absolute',
        left: 0,
        top: 0,
        bottom: 0,
        width: 4,
        background: `linear-gradient(180deg, ${accentBleu} 0%, #0f1e3d 100%)`,
        borderRadius: '12px 0 0 12px',
    }

    const statTile = (icon: ReactNode, label: string, value: ReactNode) => (
        <div style={{ ...shellCard, padding: 14 }}>
            <div
                style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 8,
                    marginBottom: 6,
                }}
            >
                <span style={{ display: 'inline-flex', color: accentBleu, flexShrink: 0 }}>{icon}</span>
                <div
                    style={{
                        color: '#64748b',
                        fontSize: 11,
                        fontWeight: 600,
                        letterSpacing: '0.04em',
                        textTransform: 'uppercase',
                        lineHeight: 1.3,
                    }}
                >
                    {label}
                </div>
            </div>
            <div style={{ fontWeight: 700, fontSize: '0.98rem', color: '#0f1e3d', lineHeight: 1.35 }}>
                {value}
            </div>
        </div>
    )

    const btnGhost = (disabled: boolean): CSSProperties => ({
        ...btnBase,
        border: '1px solid #dbe3f1',
        background: disabled ? '#f1f5f9' : '#fff',
        color: disabled ? '#94a3b8' : '#0f1e3d',
        opacity: disabled ? 0.85 : 1,
        cursor: disabled ? 'not-allowed' : 'pointer',
    })

    const btnPrimary = (enabled: boolean): CSSProperties => ({
        ...btnBase,
        border: 'none',
        background: enabled ? accentBleu : '#94a3b8',
        color: '#fff',
        opacity: enabled ? 1 : 1,
        cursor: enabled ? 'pointer' : 'not-allowed',
        boxShadow: enabled ? '0 1px 4px rgba(79, 142, 247, 0.28)' : 'none',
    })

    const btnNavy = (enabled: boolean): CSSProperties => ({
        ...btnBase,
        border: 'none',
        background: enabled ? '#0f1e3d' : '#94a3b8',
        color: '#fff',
        cursor: enabled ? 'pointer' : 'not-allowed',
        boxShadow: enabled ? '0 1px 4px rgba(15, 30, 61, 0.12)' : 'none',
    })

    const btnDangerOutline = (enabled: boolean): CSSProperties => ({
        ...btnBase,
        border: '1px solid #fecaca',
        background: '#fff',
        color: '#b91c1c',
        cursor: enabled ? 'pointer' : 'not-allowed',
        opacity: enabled ? 1 : 0.55,
    })

    const pillIdle = `${accentBleu}33`

    return (
        <div
            style={{
                width: '100%',
                maxWidth: '100%',
                margin: 0,
                fontFamily: sans,
                color: '#0f1e3d',
                display: 'flex',
                flexDirection: 'column',
                gap: 16,
            }}
        >
            <nav style={{ marginBottom: 2 }} aria-label="Fil d’ariane interne">
                <button
                    type="button"
                    onClick={() => navigate(isProf ? '/dashboard?tab=examens' : '/dashboard')}
                    style={{
                        border: 'none',
                        background: 'none',
                        padding: 0,
                        fontFamily: sans,
                        fontSize: 13,
                        fontWeight: 500,
                        color: '#64748b',
                        cursor: 'pointer',
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: 6,
                    }}
                >
                    <ChevronLeft size={18} strokeWidth={2} aria-hidden style={{ color: accentBleu }} />
                    Mes examens / tableau de bord
                </button>
            </nav>

            <div style={{ ...shellCard, padding: '1.25rem 1.25rem 1.2rem', paddingLeft: '1rem' }}>
                <div aria-hidden style={stripeLeft} />
                <div style={{ position: 'relative', paddingLeft: 8 }}>
                    <div style={{ minWidth: 0 }}>
                        <p
                            style={{
                                margin: 0,
                                color: '#64748b',
                                fontSize: 11,
                                fontWeight: 600,
                                textTransform: 'uppercase',
                                letterSpacing: '0.06em',
                            }}
                        >
                            Pilotage web — superviseur
                        </p>
                        <h1
                            style={{
                                margin: '6px 0 0',
                                fontFamily: serif,
                                fontWeight: 550,
                                fontSize: 'clamp(1.35rem, 2.8vw, 1.85rem)',
                                lineHeight: 1.22,
                                color: '#0f1e3d',
                            }}
                        >
                            {titrePrincipal(meta, snap, id)}
                        </h1>
                        {meta?.description ? (
                            <p style={{ color: '#475569', marginTop: 10, marginBottom: 0, lineHeight: 1.55, fontSize: 14 }}>
                                {meta.description}
                            </p>
                        ) : null}
                    </div>
                </div>
            </div>

            {wsNotice ? (
                <output
                    style={{
                        display: 'flex',
                        alignItems: 'flex-start',
                        gap: 10,
                        background: '#fffbeb',
                        border: '1px solid #fde68a',
                        borderRadius: 10,
                        color: '#92400e',
                        fontSize: 13,
                        padding: '12px 14px',
                        boxSizing: 'border-box',
                    }}
                    aria-live="polite"
                >
                    <span style={{ color: '#eab308', flexShrink: 0, marginTop: 1 }}>
                        <Radio size={18} strokeWidth={2} />
                    </span>
                    <span style={{ lineHeight: 1.5 }}>{wsNotice}</span>
                </output>
            ) : null}

            <div style={shellCard}>
                <div
                    style={{
                        display: 'flex',
                        alignItems: 'flex-start',
                        justifyContent: 'space-between',
                        gap: 12,
                        marginBottom: connectesLabels.length > 0 ? 10 : 8,
                        borderBottom: '1px solid #f1f5f9',
                        paddingBottom: 10,
                        flexWrap: 'wrap',
                    }}
                >
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10, minWidth: 0 }}>
                        <Users size={20} strokeWidth={2} style={{ color: accentBleu, flexShrink: 0 }} aria-hidden />
                        <div>
                            <h2 style={{ margin: 0, fontFamily: serif, fontWeight: 550, fontSize: '1.08rem', color: '#0f1e3d' }}>
                                {modeListeParticipants === 'attente'
                                    ? 'Salle d’attente'
                                    : modeListeParticipants === 'actifs'
                                      ? 'Étudiants actifs'
                                      : 'Participants'}
                            </h2>
                            {modeListeParticipants === 'attente' ? (
                                <p style={{ margin: '6px 0 0', color: '#64748b', fontSize: 13, lineHeight: 1.5, maxWidth: 560 }}>
                                    Liste d’attente avant le lancement : les élèves connectés (après le créneau) apparaissent ici ;
                                    vous pouvez lancer l’épreuve quand vous le décidez (mise à jour automatique).
                                </p>
                            ) : modeListeParticipants === 'actifs' ? (
                                <p style={{ margin: '6px 0 0', color: '#64748b', fontSize: 13, lineHeight: 1.5, maxWidth: 560 }}>
                                    Étudiants qui suivent l’examen en temps réel pendant la session.
                                </p>
                            ) : (
                                <p style={{ margin: '6px 0 0', color: '#64748b', fontSize: 13, lineHeight: 1.45 }}>
                                    Session terminée ou arrêtée — la liste reflète le dernier état connu si disponible.
                                </p>
                            )}
                        </div>
                    </div>
                    <span
                        style={{
                            fontSize: 13,
                            fontWeight: 700,
                            color: '#0f1e3d',
                            background: '#f1f5f9',
                            borderRadius: 999,
                            padding: '6px 12px',
                            flexShrink: 0,
                            alignSelf: 'flex-start',
                        }}
                        aria-live="polite"
                    >
                        {modeListeParticipants === 'attente'
                            ? `${connectesLabels.length} en attente`
                            : modeListeParticipants === 'actifs'
                              ? `${connectesLabels.length} actif${connectesLabels.length !== 1 ? 's' : ''}`
                              : `${connectesLabels.length} participant${connectesLabels.length !== 1 ? 's' : ''}`}
                    </span>
                </div>
                {connectesLabels.length > 0 ? (
                    <ul style={{ margin: 0, paddingLeft: 20, color: '#475569', lineHeight: 1.65, fontSize: 14 }}>
                        {connectesLabels.map((label, i) => (
                            <li key={`${i}-${label}`}>{label}</li>
                        ))}
                    </ul>
                ) : (
                    <p
                        style={{
                            margin: 0,
                            color: '#94a3b8',
                            fontSize: 14,
                            lineHeight: 1.55,
                            fontStyle: 'italic',
                        }}
                    >
                        {modeListeParticipants === 'attente'
                            ? 'Aucun étudiant en salle d’attente pour le moment. Les élèves qui ouvrent la page de l’examen (après le créneau) s’affichent ici.'
                            : modeListeParticipants === 'actifs'
                              ? 'Aucun étudiant connecté à l’épreuve pour le moment.'
                              : 'Aucun participant listé.'}
                    </p>
                )}
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(168px, 1fr))', gap: 10 }}>
                {statTile(
                    <CalendarClock size={18} strokeWidth={2} />,
                    'Créneau prévu',
                    formatDateTime(meta?.dateDebut),
                )}
                {statTile(
                    <Play size={18} strokeWidth={2} />,
                    'Question en cours',
                    <>{totalQuestions > 0 ? `${questionNumero} / ${totalQuestions}` : '—'}</>,
                )}
                {statTile(
                    <Timer size={18} strokeWidth={2} />,
                    'Temps restant',
                    <span aria-live="polite" aria-atomic="true">
                        {tempsRestantSupervision ??
                            (snap?.tempsRestantMinutes != null ? `${snap.tempsRestantMinutes} min` : '—')}
                    </span>,
                )}
            </div>

            <section style={shellCard} aria-labelledby="supervision-session-heading">
                <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 12 }}>
                    <Settings2 size={20} strokeWidth={2} style={{ color: accentBleu }} aria-hidden />
                    <h2 id="supervision-session-heading" style={{ margin: 0, fontFamily: serif, fontWeight: 550, fontSize: '1.08rem' }}>
                        Pilotage de session
                    </h2>
                </div>
                <div
                    style={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(auto-fit, minmax(154px, 1fr))',
                        gap: 10,
                        marginBottom: 12,
                    }}
                >
                    <button
                        type="button"
                        style={{
                            ...btnPrimary(!isSubmitting && peutLancer),
                            display: 'inline-flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            gap: 8,
                        }}
                        disabled={isSubmitting || !peutLancer}
                        onClick={async () => {
                            try {
                                setIsSubmitting(true)
                                await examenApi.lancer(id)
                                await refresh()
                                setFeedbackTone('success')
                                setFeedback(
                                    'Session lancée. Les étudiants encore en salle passent sur l’épreuve ; le suivi reste sur cette page.',
                                )
                                navigate(`/supervision/examen/${id}?started=1`, { replace: true })
                                globalThis.scrollTo({ top: 0, behavior: 'smooth' })
                            } catch (error: unknown) {
                                setFeedbackTone('error')
                                setFeedback(extractApiMessage(error, 'Lancement impossible dans l’état actuel.'))
                            } finally {
                                setIsSubmitting(false)
                            }
                        }}
                    >
                        <Play size={17} strokeWidth={2} aria-hidden /> Lancer
                    </button>
                    <button
                        type="button"
                        style={{ ...btnGhost(isSubmitting || !peutPause), display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 8 }}
                        disabled={isSubmitting || !peutPause}
                        onClick={() => action(() => examenApi.pause(id))}
                    >
                        <Pause size={17} strokeWidth={2} aria-hidden /> Pause
                    </button>
                    <button
                        type="button"
                        style={{ ...btnGhost(isSubmitting || !peutReprendre), display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 8 }}
                        disabled={isSubmitting || !peutReprendre}
                        onClick={() => action(() => examenApi.reprendre(id))}
                    >
                        <Play size={17} strokeWidth={2} aria-hidden /> Reprendre
                    </button>
                    <button
                        type="button"
                        style={{
                            ...btnDangerOutline(!isSubmitting && peutTerminer),
                            gridColumn: 'span 1',
                            display: 'inline-flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            gap: 8,
                        }}
                        disabled={isSubmitting || !peutTerminer}
                        onClick={() => action(() => examenApi.terminer(id))}
                    >
                        <Square size={15} strokeWidth={2} aria-hidden /> Terminer
                    </button>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 10, marginBottom: 12 }}>
                    <button
                        type="button"
                        style={{
                            ...btnNavy(!isSubmitting && estEnCours && questionNumero > 1),
                            display: 'inline-flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            gap: 8,
                        }}
                        disabled={isSubmitting || !estEnCours || questionNumero <= 1}
                        onClick={() => action(() => examenApi.questionPrecedente(id))}
                    >
                        <ChevronLeft size={18} strokeWidth={2} aria-hidden /> Question précédente
                    </button>
                    <button
                        type="button"
                        style={{
                            ...btnNavy(!isSubmitting && estEnCours && totalQuestions > 0 && questionNumero < totalQuestions),
                            display: 'inline-flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            gap: 8,
                        }}
                        disabled={isSubmitting || !estEnCours || totalQuestions <= 0 || questionNumero >= totalQuestions}
                        onClick={() => action(() => examenApi.questionSuivante(id))}
                    >
                        Question suivante <ChevronRight size={18} strokeWidth={2} aria-hidden />
                    </button>
                </div>

                <div
                    style={{
                        borderRadius: 10,
                        border: '1px solid #e2e8f0',
                        background: '#f8fafc',
                        padding: '12px 14px',
                        display: 'flex',
                        flexWrap: 'wrap',
                        alignItems: 'center',
                        gap: 10,
                    }}
                >
                    <span style={{ fontSize: 12, fontWeight: 600, color: '#64748b', flex: '1 1 140px' }}>Ajustements</span>
                    <button type="button" style={btnGhost(isSubmitting)} disabled={isSubmitting} onClick={() => action(() => examenApi.ajusterTemps(id, -1))}>
                        −1 min
                    </button>
                    <button type="button" style={btnGhost(isSubmitting)} disabled={isSubmitting} onClick={() => action(() => examenApi.ajusterTemps(id, 1))}>
                        +1 min
                    </button>
                </div>

                {feedback ? (
                    <p
                        style={{
                            marginTop: 12,
                            marginBottom: 0,
                            padding: '10px 12px',
                            borderRadius: 10,
                            background: feedbackBanner.bg,
                            border: `1px solid ${feedbackBanner.border}`,
                            color: feedbackBanner.fg,
                            fontSize: 13,
                            lineHeight: 1.5,
                        }}
                    >
                        {feedback}
                    </p>
                ) : null}
            </section>

            <section style={shellCard} aria-labelledby="supervision-questions-heading">
                <h2 id="supervision-questions-heading" style={{ marginTop: 0, marginBottom: 12, fontFamily: serif, fontWeight: 550, fontSize: '1.08rem' }}>
                    Supervision question par question
                </h2>
                <div
                    style={{
                        background: `#f4f7fc`,
                        border: `1px solid ${pillIdle}`,
                        borderRadius: 10,
                        padding: 14,
                        marginBottom: 14,
                        boxSizing: 'border-box',
                    }}
                >
                    <div style={{ color: accentBleu, fontSize: 11, fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase', marginBottom: 6 }}>
                        Question active (copie écran élève)
                    </div>
                    <div style={{ fontWeight: 700, marginBottom: 8, fontSize: '1rem', color: '#0f1e3d' }}>
                        {totalQuestions > 0 ? `Question ${questionNumero} / ${totalQuestions}` : 'Aucune question disponible'}
                    </div>
                    <div style={{ color: '#334155', lineHeight: 1.58, fontSize: 14, whiteSpace: 'pre-wrap' }}>
                        {questionCouranteBloc?.enonce?.trim() ||
                            (totalQuestions <= 0
                                ? 'Aucune question liée à cet examen sur le serveur. Depuis SmarTest bureau : fermez puis rouvrez l’application si besoin, cliquez à nouveau « Publier sur le web » pour ré-envoyer les QCM (messages de confirmation avec le nombre de questions), puis actualisez cette page. Conditions : au moins 2 réponses renseignées parmi les options A–D par question. Si la situation continue, vérifiez que le backend a bien exécuté la migration Flyway (table examen_publie_question).'
                                : estEnCours || etat === 'EN_PAUSE'
                                  ? 'Contenu non chargé ou indisponible pour cet index.'
                                  : 'Lancez la session (« En cours ») pour voir l’énoncé et les propositions exactement comme les étudiants.')}
                    </div>
                    {reponsesPilotage.length > 0 ? (
                        <div style={{ marginTop: 14 }}>
                            <div
                                style={{
                                    color: '#475569',
                                    fontSize: 11,
                                    fontWeight: 700,
                                    letterSpacing: '0.05em',
                                    textTransform: 'uppercase',
                                    marginBottom: 8,
                                }}
                            >
                                Propositions
                            </div>
                            <ul style={{ margin: 0, paddingLeft: 18, color: '#334155', lineHeight: 1.55, fontSize: 14 }}>
                                {reponsesPilotage.map((r, idx) => (
                                    <li key={typeof r.id === 'number' ? r.id : `p-${idx}`} style={{ marginBottom: 6 }}>
                                        {r.contenu?.trim() || '—'}
                                    </li>
                                ))}
                            </ul>
                        </div>
                    ) : null}
                </div>
                {planListe.length > 0 ? (
                    <details
                        style={{
                            border: '1px solid #e2e8f4',
                            borderRadius: 10,
                            marginBottom: 14,
                            boxSizing: 'border-box',
                            background: '#fff',
                        }}
                    >
                        <summary
                            style={{
                                cursor: 'pointer',
                                padding: '12px 14px',
                                fontWeight: 600,
                                fontSize: 13,
                                color: '#475569',
                                listStyle: 'none',
                            }}
                        >
                            Vue d’ensemble du sujet ({planListe.length} question{planListe.length > 1 ? 's' : ''}) — optionnel
                        </summary>
                        <div
                            style={{
                                padding: '0 12px 12px',
                                maxHeight: 320,
                                overflowY: 'auto',
                                boxSizing: 'border-box',
                            }}
                            aria-label="Plan de l’épreuve"
                        >
                            <ol style={{ margin: 0, paddingLeft: 20, display: 'flex', flexDirection: 'column', gap: 12 }}>
                                {planListe.map((row, idx) => {
                                    const num = typeof row.numero === 'number' ? row.numero : idx + 1
                                    const actifPlan = questionNumero > 0 && num === questionNumero
                                    return (
                                        <li
                                            key={typeof row.id === 'number' ? row.id : `plan-${idx}`}
                                            style={{
                                                listStylePosition: 'outside',
                                                borderLeft: actifPlan ? `3px solid ${accentBleu}` : '3px solid transparent',
                                                paddingLeft: actifPlan ? 10 : 8,
                                                marginLeft: -4,
                                                background: actifPlan ? `${accentBleu}0f` : 'transparent',
                                                borderRadius: 6,
                                                paddingTop: 4,
                                                paddingBottom: 4,
                                            }}
                                        >
                                            <div style={{ fontSize: 12, color: '#64748b', marginBottom: 4 }}>
                                                Question {num}
                                                {typeof row.type === 'string' && row.type.trim() ? ` · ${row.type}` : ''}
                                                {actifPlan ? (
                                                    <span style={{ color: accentBleu, fontWeight: 700, marginLeft: 6 }}>· en diffusion</span>
                                                ) : null}
                                            </div>
                                            <div style={{ color: '#0f1e3d', fontSize: 14, lineHeight: 1.55, whiteSpace: 'pre-wrap' }}>
                                                {typeof row.enonce === 'string' && row.enonce.trim() ? row.enonce : '—'}
                                            </div>
                                        </li>
                                    )
                                })}
                            </ol>
                        </div>
                    </details>
                ) : null}
                {questionPills.length > 0 ? (
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }} role="toolbar" aria-label="Aller à une question">
                        {questionPills.map((num) => {
                            const actif = num === questionNumero
                            const dis = isSubmitting || !estEnCours || num === questionNumero
                            return (
                                <button
                                    key={num}
                                    type="button"
                                    disabled={dis}
                                    onClick={() => action(() => examenApi.allerAQuestion(id, num))}
                                    style={{
                                        minWidth: 36,
                                        height: 36,
                                        borderRadius: 999,
                                        display: 'inline-flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        border: actif ? `2px solid ${accentBleu}` : '1px solid #dbe3f1',
                                        background: actif ? `${accentBleu}18` : '#fff',
                                        color: actif ? accentBleu : '#475569',
                                        fontWeight: 700,
                                        fontSize: 12,
                                        cursor: dis ? 'default' : 'pointer',
                                        opacity: isSubmitting ? 0.75 : estEnCours ? 1 : 0.6,
                                        transition: 'border-color 0.15s, background 0.15s',
                                    }}
                                    aria-current={actif ? 'step' : undefined}
                                >
                                    {num}
                                </button>
                            )
                        })}
                    </div>
                ) : null}
            </section>

            <button
                type="button"
                style={{
                    ...btnGhost(false),
                    alignSelf: 'flex-start',
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: 8,
                    paddingLeft: 12,
                    paddingRight: 16,
                    borderRadius: 8,
                }}
                onClick={() => navigate(isProf ? '/dashboard?tab=examens' : '/dashboard')}
            >
                <ChevronLeft size={18} strokeWidth={2} aria-hidden />
                Retour au tableau de bord
            </button>
        </div>
    )
}
