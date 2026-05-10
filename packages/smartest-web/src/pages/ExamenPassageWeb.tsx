import axios from 'axios'
import React, { useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { Client } from '@stomp/stompjs'
import { examenApi } from '../api/examenApi'
import {
    examenMetaSchema,
    examenSnapshotSchema,
    mapQuestionStateToSnapshot,
    type ExamenMeta,
    type ExamenSnapshot,
} from '../api/quizSchemas'
import {
    formatDateTime,
    formatTime,
    getEtatSessionLabel as getEtatLabel,
    parseDebutExamenMs,
} from '../utils/examenDisplay'
import { useExamenTempsRestantLive } from '../hooks/useExamenTempsRestantLive'

const sans = "'DM Sans', system-ui, sans-serif"
const serif = "'DM Serif Display', Georgia, serif"
const WS_BASE_URL = import.meta.env.VITE_WS_BASE_URL || 'ws://localhost:8081/ws'

const card: React.CSSProperties = {
    background: '#fff',
    border: '1px solid #e2e8f4',
    borderRadius: 14,
    padding: '1.25rem 1.35rem',
    boxShadow: '0 1px 2px rgba(15, 30, 61, 0.04)',
}

function joinedStorageKey(examenId: number): string {
    return `smartest.examen.web.joined.${examenId}`
}

function readEtudiantId(): number {
    try {
        const token = localStorage.getItem('token')
        if (token) {
            const payload = token.split('.')[1]
            if (payload) {
                const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as { userId?: unknown; id?: unknown; sub?: unknown }
                const candidate = Number(decoded.userId ?? decoded.id ?? decoded.sub)
                if (Number.isFinite(candidate) && candidate > 0) return candidate
            }
        }
    } catch {
        // fallback localStorage userId
    }
    const raw = localStorage.getItem('userId')
    const n = Number(raw)
    return Number.isFinite(n) && n > 0 ? n : 1
}

function extractApiMessage(e: unknown, fallback: string): string {
    if (axios.isAxiosError(e)) {
        const d = e.response?.data
        if (d && typeof d === 'object' && 'message' in d) {
            const m = (d as { message?: unknown }).message
            if (typeof m === 'string' && m.trim()) return m.trim()
        }
    }
    if (e instanceof Error && e.message.trim()) return e.message.trim()
    return fallback
}

const metaTile: React.CSSProperties = {
    background: '#f8fafc',
    border: '1px solid #e2e8f0',
    borderRadius: 10,
    padding: '0.75rem 0.85rem',
}

export default function ExamenPassageWeb() {
    const navigate = useNavigate()
    const location = useLocation()
    const { examenId } = useParams()
    const id = Number(examenId)
    const isEpreuve = /\/examen\/[^/]+\/epreuve\/?$/.test(location.pathname)
    const [meta, setMeta] = useState<ExamenMeta | null>(null)
    const [snap, setSnap] = useState<ExamenSnapshot | null>(null)
    const [joined, setJoined] = useState(false)
    const [status, setStatus] = useState<string>('')
    const [loading, setLoading] = useState(true)
    const [selectedResponseId, setSelectedResponseId] = useState<number | null>(null)
    const [lastAnsweredQuestionId, setLastAnsweredQuestionId] = useState<number | null>(null)
    const [submittingAnswer, setSubmittingAnswer] = useState(false)
    const [wsNotice, setWsNotice] = useState<string | null>(null)
    /** Rafraîchit l’affichage pour débloquer le créneau à l’heure prévue. */
    const [creneauTick, setCreneauTick] = useState(0)
    /** Évite d’appeler sans cesse l’API après une inscription réussie en salle d’attente. */
    const autoJoinReussi = useRef(false)

    useEffect(() => {
        const timer = window.setInterval(() => setCreneauTick((n) => n + 1), 4000)
        return () => window.clearInterval(timer)
    }, [])

    useEffect(() => {
        if (!Number.isFinite(id) || id <= 0) return
        let cancelled = false
        setLoading(true)
        examenApi
            .getMetadata(id)
            .then((r) => {
                if (cancelled) return
                const parsed = examenMetaSchema.safeParse(r.data)
                if (parsed.success) {
                    setMeta(parsed.data)
                } else {
                    setMeta(null)
                    setStatus('Métadonnées de l’examen invalides.')
                }
            })
            .catch(() => {
                if (!cancelled) setStatus('Examen indisponible ou introuvable.')
            })
            .finally(() => {
                if (!cancelled) setLoading(false)
            })
        return () => {
            cancelled = true
        }
    }, [id])

    useEffect(() => {
        autoJoinReussi.current = false
    }, [id])

    useEffect(() => {
        if (!Number.isFinite(id) || id <= 0 || !meta) return

        const syncPresence = (payload: unknown) => {
            const etudiantId = readEtudiantId()
            const connectes = (payload as { connectes?: unknown })?.connectes
            const inRoom =
                Array.isArray(connectes) &&
                connectes.some((p: unknown) => {
                    if (!p || typeof p !== 'object' || !('etudiantId' in p)) return false
                    const eid = Number((p as { etudiantId: unknown }).etudiantId)
                    return Number.isFinite(eid) && eid === etudiantId
                })
            setJoined(inRoom)
            try {
                if (inRoom) sessionStorage.setItem(joinedStorageKey(id), '1')
                else sessionStorage.removeItem(joinedStorageKey(id))
            } catch {
                /* quota / navigation privée */
            }
        }

        const runPolling = () => {
            const debutMs = parseDebutExamenMs(meta.dateDebut)
            const avantCreneau = debutMs != null && Date.now() < debutMs
            if (avantCreneau) {
                setJoined(false)
                setSnap(null)
                try {
                    sessionStorage.removeItem(joinedStorageKey(id))
                } catch {
                    /* ignore */
                }
                return
            }

            const etudiantIdPoll = readEtudiantId()
            examenApi
                .getQuestionCourante(id, etudiantIdPoll)
                .then((r) => {
                    const mapped = mapQuestionStateToSnapshot(r.data)
                    if (mapped) setSnap(mapped)
                })
                .catch(() => undefined)
            examenApi
                .getSalleAttente(id)
                .then((r) => syncPresence(r.data))
                .catch(() => {
                    try {
                        if (sessionStorage.getItem(joinedStorageKey(id)) === '1') setJoined(true)
                    } catch {
                        /* ignore */
                    }
                })
        }

        runPolling()
        const t = window.setInterval(runPolling, 2500)
        return () => window.clearInterval(t)
    }, [id, meta])

    useEffect(() => {
        if (!Number.isFinite(id) || id <= 0) return
        const etudiantId = readEtudiantId()
        let cancelled = false

        const client = new Client({
            brokerURL: WS_BASE_URL,
            reconnectDelay: 3000,
            onConnect: () => {
                client.subscribe(`/topic/examen/${id}/etat`, (message) => {
                    try {
                        const raw = JSON.parse(message.body) as unknown
                        const parsed = examenSnapshotSchema.safeParse(raw)
                        if (cancelled) return
                        if (parsed.success) {
                            setSnap(parsed.data)
                            setWsNotice(null)
                        } else if (!cancelled) {
                            setWsNotice('Message temps réel invalide.')
                        }
                    } catch {
                        if (!cancelled) setWsNotice('Message temps réel invalide.')
                    }
                })
                client.subscribe(`/topic/examen/${id}/salle-attente`, (message) => {
                    try {
                        const payload = JSON.parse(message.body) as { connectes?: Array<{ etudiantId?: number }> }
                        if (cancelled) return
                        const connectes = Array.isArray(payload.connectes) ? payload.connectes : []
                        const inRoom = connectes.some((p) => Number(p?.etudiantId) === etudiantId)
                        setJoined(inRoom)
                    } catch {
                        // ignore, polling garde un fallback
                    }
                })
            },
            onWebSocketClose: () => {
                if (!cancelled) setWsNotice('Connexion temps réel fermée. Mode rafraîchissement actif.')
            },
            onWebSocketError: () => {
                if (!cancelled) setWsNotice('Erreur WebSocket. Mode rafraîchissement actif.')
            },
            onStompError: () => {
                if (!cancelled) setWsNotice('Erreur STOMP. Mode rafraîchissement actif.')
            },
        })

        client.activate()
        return () => {
            cancelled = true
            client.deactivate()
        }
    }, [id])

    useEffect(() => {
        const et = (snap?.etat ?? '').toUpperCase()
        if (et !== 'ARRETE') return
        setJoined(false)
        try {
            sessionStorage.removeItem(joinedStorageKey(id))
        } catch {
            /* ignore */
        }
    }, [snap?.etat, id])

    /** Page attente → passage automatique vers l’écran d’épreuve quand la session démarre ou est en pause. */
    useEffect(() => {
        if (isEpreuve) return
        const et = (snap?.etat ?? '').toUpperCase()
        if (et === 'EN_COURS' || et === 'EN_PAUSE') {
            navigate(`/examen/${id}/epreuve`, { replace: true })
        }
    }, [snap?.etat, id, navigate, isEpreuve])

    /**
     * Si le professeur a déjà lancé (statut persisté en base), l’étudiant qui ouvre la page accède tout de suite
     * à l’épreuve sans passer par l’affichage « salle d’attente » jusqu’au prochain poll.
     */
    useEffect(() => {
        if (loading) return
        if (isEpreuve) return
        const s = (meta?.statut ?? '').trim().toUpperCase()
        if (s === 'EN_COURS' || s === 'EN_PAUSE') {
            navigate(`/examen/${id}/epreuve`, { replace: true })
        }
    }, [loading, isEpreuve, meta?.statut, id, navigate])

    /** URL /epreuve ouverte trop tôt : retour à la page attente tant que la session n’a pas commencé. */
    useEffect(() => {
        if (!isEpreuve || snap == null) return
        const et = (snap.etat ?? '').toUpperCase()
        if (et === 'PLANIFIE') {
            navigate(`/examen/${id}`, { replace: true })
        }
    }, [snap, id, navigate, isEpreuve])

    /** Session annulée par le professeur : quitter la page (pas de soumission). */
    useEffect(() => {
        const et = (snap?.etat ?? '').toUpperCase()
        if (et !== 'ARRETE') return
        const t = window.setTimeout(() => navigate('/dashboard', { replace: true }), 700)
        return () => window.clearTimeout(t)
    }, [snap?.etat, navigate])

    /** Inscription automatique en salle d’attente dès le créneau (pas de bouton manuel). */
    const debutMsPourJoin = meta ? parseDebutExamenMs(meta.dateDebut) : null
    const creneauOkPourJoin =
        debutMsPourJoin == null || Date.now() >= debutMsPourJoin
    useEffect(() => {
        if (!Number.isFinite(id) || id <= 0 || !meta) return
        if (!creneauOkPourJoin) return
        if (autoJoinReussi.current) return
        const phase = (snap?.etat ?? '').toUpperCase()
        const metaStatut = (meta?.statut ?? '').trim().toUpperCase()
        if (phase === 'TERMINE' || phase === 'ARRETE') return
        if (metaStatut === 'TERMINE' || metaStatut === 'ANNULE') return

        let cancelled = false
        ;(async () => {
            try {
                const email = localStorage.getItem('email') || 'etudiant@smartest.local'
                const etudiantId = readEtudiantId()
                const { data } = await examenApi.rejoindreSalleAttente(id, etudiantId, email)
                if (cancelled) return
                autoJoinReussi.current = true
                setJoined(true)
                try {
                    sessionStorage.setItem(joinedStorageKey(id), '1')
                } catch {
                    /* ignore */
                }
                setStatus('')
                const etatReponse = (data as { etat?: string } | undefined)?.etat
                const phaseJoin = typeof etatReponse === 'string' ? etatReponse.trim().toUpperCase() : ''
                if (phaseJoin === 'EN_COURS' || phaseJoin === 'EN_PAUSE') {
                    navigate(`/examen/${id}/epreuve`, { replace: true })
                }
            } catch (e: unknown) {
                if (!cancelled) {
                    setStatus(extractApiMessage(e, 'Connexion à la salle d’attente en cours…'))
                }
            }
        })()
        return () => {
            cancelled = true
        }
    }, [id, meta, creneauOkPourJoin, creneauTick, snap?.etat, navigate])

    /** Dès que le prof lance l’épreuve, considérer l’étudiant « dans l’examen » sans autre action. */
    useEffect(() => {
        const p = (snap?.etat ?? '').toUpperCase()
        if (p === 'EN_COURS' || p === 'EN_PAUSE') {
            setJoined(true)
        }
    }, [snap?.etat])

    const enPausePourTemps =
        Boolean(snap?.enPause) || (snap?.etat ?? '').trim().toUpperCase() === 'EN_PAUSE'
    const tempsRestantAffiche = useExamenTempsRestantLive(
        snap?.tempsRestantMinutes,
        snap?.etat ?? meta?.statut ?? '',
        enPausePourTemps,
    )

    if (!Number.isFinite(id) || id <= 0) {
        return (
            <p style={{ fontFamily: sans, color: '#64748b' }}>Examen invalide.</p>
        )
    }

    void creneauTick // dépendance logique pour réévaluer Date.now() après quelques secondes
    const debutMsAffichage = parseDebutExamenMs(meta?.dateDebut)
    const creneauAtteint = debutMsAffichage == null || Date.now() >= debutMsAffichage

    const phaseSession = (snap?.etat ?? '').trim().toUpperCase()
    const canStart = phaseSession === 'EN_COURS'
    const enPause = phaseSession === 'EN_PAUSE'
    const sessionTerminee =
        phaseSession === 'TERMINE' || (meta?.statut ?? '').trim().toUpperCase() === 'TERMINE'
    const etat = snap?.etat ?? meta?.statut ?? 'PLANIFIE'
    const etudiantId = readEtudiantId()
    const questionCourante = snap?.questionCourante
    const questionCouranteAvecReponses = questionCourante as
        | (typeof questionCourante & { reponses?: Array<{ id?: number; contenu?: string }> })
        | undefined
    const questionId = typeof questionCourante?.id === 'number' ? questionCourante.id : null
    const reponses = Array.isArray(questionCouranteAvecReponses?.reponses)
        ? (questionCouranteAvecReponses.reponses as Array<{ id?: number; contenu?: string }>)
        : []

    useEffect(() => {
        if (questionId == null) {
            setSelectedResponseId(null)
            return
        }
        if (lastAnsweredQuestionId !== questionId) {
            setSelectedResponseId(null)
        }
    }, [questionId, lastAnsweredQuestionId])

    const soumettreReponseCourante = async () => {
        if (!Number.isFinite(id) || id <= 0) return
        if (!canStart || enPause) return
        if (questionId == null || selectedResponseId == null) {
            setStatus('Sélectionnez une réponse avant de valider.')
            return
        }

        try {
            setSubmittingAnswer(true)
            await examenApi.repondreQuestionCourante(id, etudiantId, questionId, selectedResponseId)
            setLastAnsweredQuestionId(questionId)
            setStatus('Réponse enregistrée. Aucune correction immédiate n’est affichée pendant l’examen.')
        } catch (e: unknown) {
            setStatus(extractApiMessage(e, 'Impossible d’enregistrer la réponse pour la question active.'))
        } finally {
            setSubmittingAnswer(false)
        }
    }

    const heureLancement = formatTime(meta?.dateDebut)
    const dateExamen = formatDateTime(meta?.dateDebut)

    if (loading) {
        return (
            <div
                style={{
                    maxWidth: 920,
                    margin: '0 auto',
                    fontFamily: sans,
                    color: '#64748b',
                    padding: '2rem 0',
                    textAlign: 'center',
                }}
            >
                Chargement des informations de l’examen…
            </div>
        )
    }

    const shell: React.CSSProperties = {
        width: '100%',
        maxWidth: 920,
        margin: '0 auto',
        fontFamily: sans,
        color: '#0f1e3d',
        display: 'flex',
        flexDirection: 'column',
        gap: 18,
    }

    if (sessionTerminee) {
        return (
            <div style={shell}>
                <div style={card}>
                    <p
                        style={{
                            margin: 0,
                            fontSize: 12,
                            fontWeight: 600,
                            color: '#64748b',
                            letterSpacing: '0.04em',
                            textTransform: 'uppercase',
                        }}
                    >
                        Examen terminé
                    </p>
                    <h1
                        style={{
                            margin: '8px 0 0',
                            fontFamily: serif,
                            fontSize: 'clamp(1.25rem, 2.2vw, 1.55rem)',
                            fontWeight: 550,
                            lineHeight: 1.25,
                        }}
                    >
                        {meta?.titre?.trim() ? meta.titre : `Examen #${id}`}
                    </h1>
                    <p style={{ margin: '14px 0 0', color: '#475569', lineHeight: 1.6, fontSize: 15 }}>
                        Cette session est close : vous ne pouvez plus ouvrir l’épreuve ni modifier vos réponses. Votre note
                        sera communiquée ultérieurement par votre professeur (validation des résultats sur la plateforme).
                    </p>
                    <button
                        type="button"
                        onClick={() => navigate('/dashboard', { replace: true })}
                        style={{
                            marginTop: 18,
                            padding: '10px 18px',
                            borderRadius: 10,
                            border: 'none',
                            background: '#0f1e3d',
                            color: '#fff',
                            fontWeight: 600,
                            fontFamily: sans,
                            fontSize: 14,
                            cursor: 'pointer',
                        }}
                    >
                        Retour au tableau de bord
                    </button>
                </div>
            </div>
        )
    }

    const blocQuestion = (
        <>
            {enPause ? (
                <>
                    <p style={{ margin: '0 0 12px', color: '#92400e', lineHeight: 1.5 }}>
                        Examen en pause. Attendez la reprise par le professeur — vous ne pouvez pas modifier vos réponses
                        pendant la pause.
                    </p>
                    {questionCourante ? (
                        <>
                            <p style={{ margin: '0 0 8px', color: '#64748b', fontSize: 13 }}>
                                Question {(snap?.questionCouranteIndex ?? 0) + 1} / {snap?.totalQuestions ?? '—'}{' '}
                                <span style={{ color: '#94a3b8' }}>(affichage seul)</span>
                            </p>
                            <p style={{ margin: '0 0 10px', fontWeight: 600, lineHeight: 1.55 }}>
                                {questionCourante.enonce || 'Question'}
                            </p>
                            <ul style={{ margin: 0, paddingLeft: 18, color: '#475569' }}>
                                {reponses.map((r, idx) => (
                                    <li key={typeof r.id === 'number' ? r.id : `r-${idx}`} style={{ marginBottom: 6 }}>
                                        {r.contenu || '—'}
                                    </li>
                                ))}
                            </ul>
                        </>
                    ) : null}
                </>
            ) : canStart && questionCourante ? (
                <>
                    <p style={{ margin: '0 0 8px', color: '#64748b', fontSize: 13 }}>
                        Question {(snap?.questionCouranteIndex ?? 0) + 1} / {snap?.totalQuestions ?? '—'}
                    </p>
                    <p style={{ margin: '0 0 14px', fontWeight: 600, lineHeight: 1.55 }}>
                        {questionCourante.enonce || 'Question en cours'}
                    </p>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                        {reponses.map((r) => {
                            const rid = typeof r.id === 'number' ? r.id : -1
                            const checked = rid > 0 && selectedResponseId === rid
                            return (
                                <label
                                    key={rid}
                                    style={{
                                        display: 'flex',
                                        alignItems: 'flex-start',
                                        gap: 10,
                                        border: `1px solid ${checked ? '#93c5fd' : '#e2e8f0'}`,
                                        borderRadius: 10,
                                        background: checked ? '#eff6ff' : '#fff',
                                        padding: '10px 12px',
                                        cursor: 'pointer',
                                    }}
                                >
                                    <input
                                        type="radio"
                                        name={`question-${questionId ?? 'x'}`}
                                        checked={checked}
                                        onChange={() => setSelectedResponseId(rid > 0 ? rid : null)}
                                    />
                                    <span style={{ lineHeight: 1.5, color: '#0f1e3d' }}>{r.contenu || 'Réponse'}</span>
                                </label>
                            )
                        })}
                    </div>

                    <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', marginTop: 14 }}>
                        <button
                            type="button"
                            onClick={soumettreReponseCourante}
                            disabled={submittingAnswer || selectedResponseId == null}
                            style={{
                                background: '#0f1e3d',
                                color: '#fff',
                                border: '1px solid #0f1e3d',
                                borderRadius: 10,
                                padding: '10px 14px',
                                fontWeight: 700,
                                cursor: submittingAnswer || selectedResponseId == null ? 'default' : 'pointer',
                                opacity: submittingAnswer || selectedResponseId == null ? 0.65 : 1,
                            }}
                        >
                            {submittingAnswer ? 'Validation...' : 'Valider ma réponse'}
                        </button>
                    </div>

                    <p style={{ margin: '12px 0 0', color: '#64748b', fontSize: 13 }}>
                        Le passage entre les questions est imposé par le professeur. Aucune correction ni score pendant
                        l’épreuve.
                    </p>
                </>
            ) : canStart ? (
                <p style={{ margin: 0, color: '#64748b', lineHeight: 1.5 }}>
                    En attente du contenu de la question… Si cela dure, vérifiez votre connexion. Le professeur pilote l’épreuve
                    : une seule question à la fois s’affiche pour toute la classe, comme un quiz guidé.
                </p>
            ) : (
                <p style={{ margin: 0, color: '#64748b', lineHeight: 1.5 }}>
                    Les questions apparaîtront une par une dès que le professeur lance l’examen ; la question affichée est la même
                    pour tout le monde, contrôlée par le professeur.
                </p>
            )}
        </>
    )

    if (isEpreuve) {
        return (
            <div style={shell}>
                <div style={card}>
                    <div
                        style={{
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'flex-start',
                            gap: 16,
                            flexWrap: 'wrap',
                        }}
                    >
                        <div style={{ flex: '1 1 240px' }}>
                            <p
                                style={{
                                    margin: 0,
                                    fontSize: 12,
                                    fontWeight: 600,
                                    color: '#64748b',
                                    letterSpacing: '0.04em',
                                    textTransform: 'uppercase',
                                }}
                            >
                                Épreuve en cours
                            </p>
                            <h1
                                style={{
                                    margin: '6px 0 0',
                                    fontFamily: serif,
                                    fontSize: 'clamp(1.25rem, 2.2vw, 1.6rem)',
                                    fontWeight: 550,
                                    lineHeight: 1.2,
                                }}
                            >
                                {meta?.titre ?? `Examen #${id}`}
                            </h1>
                            {tempsRestantAffiche != null ? (
                                <p
                                    style={{ margin: '10px 0 0', color: '#475569', fontSize: 14 }}
                                    aria-live="polite"
                                    aria-atomic="true"
                                >
                                    Temps restant : <strong>{tempsRestantAffiche}</strong>
                                </p>
                            ) : null}
                        </div>
                        <span
                            style={{
                                background: canStart ? '#dcfce7' : '#fff7ed',
                                color: canStart ? '#166534' : '#b45309',
                                borderRadius: 999,
                                padding: '8px 14px',
                                fontWeight: 700,
                                fontSize: 12,
                                whiteSpace: 'nowrap',
                                alignSelf: 'flex-start',
                            }}
                        >
                            {getEtatLabel(etat)}
                        </span>
                    </div>
                </div>

                {status ? (
                    <p style={{ margin: 0, color: '#64748b', fontSize: 14 }}>{status}</p>
                ) : null}
                {wsNotice ? (
                    <p style={{ margin: 0, color: '#9a3412', fontSize: 13 }}>{wsNotice}</p>
                ) : null}

                <div style={card}>
                    <h2
                        style={{
                            margin: '0 0 10px',
                            fontFamily: serif,
                            fontSize: '1.25rem',
                            fontWeight: 550,
                        }}
                    >
                        Question en cours
                    </h2>
                    {blocQuestion}
                </div>
            </div>
        )
    }

    return (
        <div style={shell}>
            {/* En-tête */}
            <div style={card}>
                <div
                    style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'flex-start',
                        gap: 16,
                        flexWrap: 'wrap',
                    }}
                >
                    <div style={{ flex: '1 1 240px' }}>
                        <p
                            style={{
                                margin: 0,
                                fontSize: 12,
                                fontWeight: 600,
                                color: '#64748b',
                                letterSpacing: '0.04em',
                                textTransform: 'uppercase',
                            }}
                        >
                            Examen
                        </p>
                        <h1
                            style={{
                                margin: '6px 0 0',
                                fontFamily: serif,
                                fontSize: 'clamp(1.35rem, 2.5vw, 1.75rem)',
                                fontWeight: 550,
                                lineHeight: 1.2,
                            }}
                        >
                            {meta?.titre ?? `Examen #${id}`}
                        </h1>
                        <p style={{ margin: '10px 0 0', color: '#475569', lineHeight: 1.55, fontSize: 15 }}>
                            {meta?.description?.trim()
                                ? meta.description
                                : 'Votre professeur a publié cet examen sur la plateforme. Les questions ne sont visibles qu’une fois la session lancée.'}
                        </p>
                    </div>
                    <span
                        style={{
                            background: canStart ? '#dcfce7' : '#fff7ed',
                            color: canStart ? '#166534' : '#b45309',
                            borderRadius: 999,
                            padding: '8px 14px',
                            fontWeight: 700,
                            fontSize: 12,
                            whiteSpace: 'nowrap',
                            alignSelf: 'flex-start',
                        }}
                    >
                        {getEtatLabel(etat)}
                    </span>
                </div>
            </div>

            {/* Date, durée, enseignant */}
            <div style={card}>
                <h2
                    style={{
                        margin: '0 0 14px',
                        fontFamily: serif,
                        fontSize: '1.25rem',
                        fontWeight: 550,
                    }}
                >
                    Informations sur l’épreuve
                </h2>
                <div
                    style={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
                        gap: 10,
                    }}
                >
                    <div style={metaTile}>
                        <div style={{ color: '#64748b', fontSize: 12, marginBottom: 4 }}>
                            Date et heure de lancement (créneau saisi pour l’épreuve)
                        </div>
                        <div style={{ fontWeight: 600, fontSize: 14 }}>{dateExamen}</div>
                    </div>
                    <div style={metaTile}>
                        <div style={{ color: '#64748b', fontSize: 12, marginBottom: 4 }}>Durée</div>
                        <div style={{ fontWeight: 600, fontSize: 14 }}>{meta?.duree != null ? `${meta.duree} min` : '—'}</div>
                    </div>
                    <div style={metaTile}>
                        <div style={{ color: '#64748b', fontSize: 12, marginBottom: 4 }}>Enseignant</div>
                        <div style={{ fontWeight: 600, fontSize: 14 }}>{meta?.professeurNom?.trim() || '—'}</div>
                    </div>
                </div>
            </div>

            {/* Salle d’attente */}
            <div style={card}>
                <h2
                    style={{
                        margin: '0 0 8px',
                        fontFamily: serif,
                        fontSize: '1.25rem',
                        fontWeight: 550,
                    }}
                >
                    Salle d’attente
                </h2>
                <p style={{ color: '#64748b', margin: '0 0 16px', lineHeight: 1.55, fontSize: 15 }}>
                    {creneauAtteint ? (
                        <>
                            Votre inscription à la salle d’attente est automatique sur cette page. Lorsque le professeur
                            démarre l’examen depuis l’application enseignant, vous êtes redirigé vers la page d’épreuve
                            pour répondre aux questions.
                        </>
                    ) : (
                        <>
                            L’examen devient accessible lorsque l’heure prévue du créneau est atteinte : vous pourrez alors
                            rejoindre la salle d’attente sur cette page. En attendant, consultez les informations
                            ci-dessus ; les questions ne sont pas disponibles avant le lancement par le professeur.
                        </>
                    )}
                </p>

                {joined && !canStart && creneauAtteint ? (
                    <div
                        style={{
                            marginTop: 4,
                            color: '#92400e',
                            background: '#fffbeb',
                            border: '1px solid #fde68a',
                            borderRadius: 10,
                            padding: 14,
                            lineHeight: 1.5,
                        }}
                    >
                        <strong>L’examen va bientôt commencer, veuillez patienter.</strong>
                        <span style={{ display: 'block', marginTop: 8 }}>
                            Vous êtes bien connecté à la salle d’attente (créneau prévu vers{' '}
                            <strong>{heureLancement}</strong>). Dès que le professeur clique sur « Démarrer l’examen », vous
                            passerez automatiquement à la page d’épreuve.
                        </span>
                    </div>
                ) : null}

                {status ? (
                    <p style={{ marginTop: 14, marginBottom: 0, color: '#64748b', fontSize: 14 }}>{status}</p>
                ) : null}
                {wsNotice ? (
                    <p style={{ marginTop: 10, marginBottom: 0, color: '#9a3412', fontSize: 13 }}>{wsNotice}</p>
                ) : null}
            </div>
        </div>
    )
}
