import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import axios from 'axios'
import { Client } from '@stomp/stompjs'
import { examenApi } from '../api/examenApi'
import type { ExamenMeta, ExamenSnapshot } from '../api/quizSchemas'

const sans = "'DM Sans', system-ui, sans-serif"
const serif = "'DM Serif Display', Georgia, serif"
const WS_BASE_URL = 'ws://localhost:8081/ws'

const card: React.CSSProperties = {
    background: '#fff',
    border: '1px solid #e2e8f4',
    borderRadius: 14,
    padding: '1.1rem 1.2rem',
    boxShadow: '0 1px 2px rgba(15, 30, 61, 0.04)',
}

const btn: React.CSSProperties = {
    height: 38,
    borderRadius: 10,
    border: '1px solid #dbe3f1',
    padding: '0 14px',
    background: '#fff',
    color: '#0f1e3d',
    fontWeight: 600,
    cursor: 'pointer',
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

function formatEtat(etat?: string): string {
    switch ((etat || '').toUpperCase()) {
        case 'PLANIFIE':
            return 'Planifié'
        case 'EN_ATTENTE':
        case 'EN_ATTENTE_LANCEMENT':
            return 'En attente'
        case 'EN_COURS':
            return 'En cours'
        case 'EN_PAUSE':
            return 'En pause'
        case 'TERMINE':
            return 'Terminé'
        case 'ARRETE':
            return 'Arrêté'
        default:
            return etat || 'Inconnu'
    }
}

function getEtatBadge(etat?: string): { bg: string; fg: string } {
    const key = (etat || '').toUpperCase()
    if (key === 'EN_COURS') return { bg: '#dcfce7', fg: '#166534' }
    if (key === 'EN_PAUSE') return { bg: '#fef3c7', fg: '#92400e' }
    if (key === 'TERMINE' || key === 'ARRETE') return { bg: '#e2e8f0', fg: '#334155' }
    return { bg: '#fff7ed', fg: '#b45309' }
}

function extractApiMessage(error: unknown, fallback: string): string {
    if (axios.isAxiosError(error)) {
        const data = error.response?.data
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

export default function ExamenSupervisionPage() {
    const { examenId } = useParams()
    const navigate = useNavigate()
    const [meta, setMeta] = useState<ExamenMeta | null>(null)
    const [snap, setSnap] = useState<ExamenSnapshot | null>(null)
    const [bareme, setBareme] = useState('20')
    const [feedback, setFeedback] = useState('')
    const [isSubmitting, setIsSubmitting] = useState(false)
    const [wsNotice, setWsNotice] = useState<string | null>(null)
    const [connectesLabels, setConnectesLabels] = useState<string[]>([])
    const id = Number(examenId)

    const refresh = async () => {
        if (!Number.isFinite(id) || id <= 0) return
        const [m, s, room] = await Promise.all([
            examenApi.getMetadata(id),
            examenApi.snapshot(id),
            examenApi.getSalleAttente(id),
        ])
        setMeta(m.data)
        setSnap(s.data)
        if (s.data.baremeSur20 != null) setBareme(String(s.data.baremeSur20))

        const raw = room.data as {
            connectes?: { email?: string; etudiantId?: number }[]
            Connectes?: { email?: string; etudiantId?: number }[]
        }
        const list = raw.connectes ?? raw.Connectes ?? []
        setConnectesLabels(
            list.map((p) => {
                const mail = (p.email ?? '').trim()
                const sid = p.etudiantId != null ? String(p.etudiantId) : '?'
                return mail ? `${mail} (id ${sid})` : `Étudiant id ${sid}`
            }),
        )
    }

    useEffect(() => {
        refresh().catch(() => undefined)
        const t = window.setInterval(() => refresh().catch(() => undefined), 2500)
        return () => window.clearInterval(t)
    }, [id])

    useEffect(() => {
        if (!Number.isFinite(id) || id <= 0) return

        let cancelled = false
        const client = new Client({
            brokerURL: WS_BASE_URL,
            reconnectDelay: 3000,
            onConnect: () => {
                client.subscribe(`/topic/examen/${id}/etat`, (message) => {
                    try {
                        const data = JSON.parse(message.body) as ExamenSnapshot
                        if (cancelled) return
                        setSnap(data)
                        if (data.baremeSur20 != null) setBareme(String(data.baremeSur20))
                        setWsNotice(null)
                    } catch {
                        if (!cancelled) setWsNotice('Message temps réel invalide (supervision examen).')
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
    }, [id])

    const action = async (run: () => Promise<unknown>) => {
        try {
            setIsSubmitting(true)
            await run()
            await refresh()
            setFeedback('Action appliquée avec succès.')
        } catch (error: unknown) {
            setFeedback(extractApiMessage(error, 'Action impossible. Vérifiez l’état actuel de l’examen.'))
        } finally {
            setIsSubmitting(false)
        }
    }

    if (!Number.isFinite(id) || id <= 0) return <p>Examen invalide.</p>

    const totalQuestions = Math.max(0, snap?.totalQuestions ?? meta?.totalQuestions ?? 0)
    const currentIndex = Math.max(0, snap?.questionCouranteIndex ?? 0)
    const questionNumero = totalQuestions > 0 ? Math.min(currentIndex + 1, totalQuestions) : 0
    const etat = (snap?.etat ?? meta?.statut ?? 'PLANIFIE').toUpperCase()
    const estEnCours = etat === 'EN_COURS'
    const peutLancer = etat === 'PLANIFIE' || etat === 'EN_PAUSE'
    const peutPause = etat === 'EN_COURS'
    const peutReprendre = etat === 'EN_PAUSE'
    const peutTerminer = etat !== 'TERMINE' && etat !== 'ARRETE'
    const etatBadge = getEtatBadge(etat)
    const questionPills = totalQuestions > 0 ? Array.from({ length: totalQuestions }, (_, i) => i + 1) : []

    return (
        <div
            style={{
                width: '100%',
                maxWidth: 1100,
                margin: '0 auto',
                fontFamily: sans,
                color: '#0f1e3d',
                display: 'flex',
                flexDirection: 'column',
                gap: 14,
            }}
        >
            <div style={card}>
                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
                    <div style={{ flex: '1 1 260px' }}>
                        <p
                            style={{
                                margin: 0,
                                color: '#64748b',
                                fontSize: 12,
                                fontWeight: 700,
                                textTransform: 'uppercase',
                                letterSpacing: '0.04em',
                            }}
                        >
                            Supervision examen (professeur)
                        </p>
                        <h1
                            style={{
                                margin: '6px 0 0',
                                fontFamily: serif,
                                fontWeight: 550,
                                fontSize: 'clamp(1.25rem, 2.4vw, 1.6rem)',
                            }}
                        >
                            {meta?.titre ?? `Examen #${id}`}
                        </h1>
                        {meta?.description ? (
                            <p style={{ color: '#475569', marginTop: 8, marginBottom: 0, lineHeight: 1.5 }}>{meta.description}</p>
                        ) : null}
                    </div>
                    <span
                        style={{
                            background: etatBadge.bg,
                            color: etatBadge.fg,
                            borderRadius: 999,
                            padding: '8px 14px',
                            fontWeight: 700,
                            fontSize: 12,
                            height: 'fit-content',
                        }}
                    >
                        {formatEtat(etat)}
                    </span>
                </div>
            </div>

            {wsNotice ? (
                <div
                    style={{
                        background: '#fff7ed',
                        border: '1px solid #fed7aa',
                        borderRadius: 10,
                        color: '#9a3412',
                        fontSize: 13,
                        padding: '10px 12px',
                    }}
                >
                    {wsNotice}
                </div>
            ) : null}

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))', gap: 10 }}>
                <div style={{ ...card, borderRadius: 12, padding: 12 }}>
                    <div style={{ color: '#64748b', fontSize: 12 }}>État</div>
                    <div style={{ fontWeight: 700 }}>{formatEtat(etat)}</div>
                </div>
                <div style={{ ...card, borderRadius: 12, padding: 12 }}>
                    <div style={{ color: '#64748b', fontSize: 12 }}>Créneau de lancement (jour et heure saisis)</div>
                    <div style={{ fontWeight: 700 }}>{formatDateTime(meta?.dateDebut)}</div>
                </div>
                <div style={{ ...card, borderRadius: 12, padding: 12 }}>
                    <div style={{ color: '#64748b', fontSize: 12 }}>Durée</div>
                    <div style={{ fontWeight: 700 }}>{meta?.duree ?? '-'} min</div>
                </div>
                <div style={{ ...card, borderRadius: 12, padding: 12 }}>
                    <div style={{ color: '#64748b', fontSize: 12 }}>Question en cours</div>
                    <div style={{ fontWeight: 700 }}>
                        {totalQuestions > 0 ? `${questionNumero} / ${totalQuestions}` : '-'}
                    </div>
                </div>
                <div style={{ ...card, borderRadius: 12, padding: 12 }}>
                    <div style={{ color: '#64748b', fontSize: 12 }}>Temps restant</div>
                    <div style={{ fontWeight: 700 }}>{snap?.tempsRestantMinutes ?? '-'} min</div>
                </div>
                <div style={{ ...card, borderRadius: 12, padding: 12 }}>
                    <div style={{ color: '#64748b', fontSize: 12 }}>Étudiants en attente</div>
                    <div style={{ fontWeight: 700 }}>{snap?.participantsEnAttente ?? 0}</div>
                </div>
            </div>

            {connectesLabels.length > 0 ? (
                <div style={card}>
                    <h3 style={{ marginTop: 0, marginBottom: 8, fontFamily: serif, fontWeight: 550 }}>Présents (salle d’attente / session)</h3>
                    <ul style={{ margin: 0, paddingLeft: 18, color: '#334155', lineHeight: 1.6 }}>
                        {connectesLabels.map((label, i) => (
                            <li key={`${i}-${label}`}>{label}</li>
                        ))}
                    </ul>
                </div>
            ) : null}

            <div style={card}>
                <h3 style={{ marginTop: 0, marginBottom: 10, fontFamily: serif, fontWeight: 550 }}>Pilotage de session</h3>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 10, marginBottom: 10 }}>
                    <button style={btn} disabled={isSubmitting || !peutLancer} onClick={() => action(() => examenApi.lancer(id))}>Lancer</button>
                    <button style={btn} disabled={isSubmitting || !peutPause} onClick={() => action(() => examenApi.pause(id))}>Pause</button>
                    <button style={btn} disabled={isSubmitting || !peutReprendre} onClick={() => action(() => examenApi.reprendre(id))}>Reprendre</button>
                    <button
                        style={btn}
                        disabled={isSubmitting || !estEnCours || questionNumero <= 1}
                        onClick={() => action(() => examenApi.questionPrecedente(id))}
                    >
                        Question précédente
                    </button>
                    <button
                        style={btn}
                        disabled={isSubmitting || !estEnCours || totalQuestions <= 0 || questionNumero >= totalQuestions}
                        onClick={() => action(() => examenApi.questionSuivante(id))}
                    >
                        Question suivante
                    </button>
                    <button
                        style={{ ...btn, border: '1px solid #fecaca', color: '#b91c1c', background: '#fff' }}
                        disabled={isSubmitting || !peutTerminer}
                        onClick={() => action(() => examenApi.terminer(id))}
                    >
                        Terminer
                    </button>
                </div>

                <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                    <button style={btn} disabled={isSubmitting} onClick={() => action(() => examenApi.ajusterTemps(id, -1))}>-1 min</button>
                    <button style={btn} disabled={isSubmitting} onClick={() => action(() => examenApi.ajusterTemps(id, 1))}>+1 min</button>
                    <input value={bareme} onChange={(e) => setBareme(e.target.value)} style={{ ...btn, width: 90, cursor: 'text' }} />
                    <button style={btn} disabled={isSubmitting} onClick={() => action(() => examenApi.definirBareme(id, Number(bareme) || 20))}>
                        Définir barème
                    </button>
                </div>

                {feedback ? (
                    <p style={{ marginTop: 10, marginBottom: 0, color: '#475569', fontSize: 13 }}>{feedback}</p>
                ) : null}
            </div>

            <div style={card}>
                <h3 style={{ marginTop: 0, marginBottom: 8, fontFamily: serif, fontWeight: 550 }}>Supervision question par question</h3>
                <p style={{ marginTop: 0, color: '#64748b', fontSize: 14, lineHeight: 1.5 }}>
                    Le pilotage avance question par question comme côté étudiant. La question active ci-dessous est celle en cours de diffusion.
                </p>
                <div
                    style={{
                        background: '#f8fafc',
                        border: '1px solid #e2e8f0',
                        borderRadius: 10,
                        padding: 12,
                        marginBottom: 12,
                    }}
                >
                    <div style={{ color: '#64748b', fontSize: 12, marginBottom: 4 }}>Question active</div>
                    <div style={{ fontWeight: 700, marginBottom: 6 }}>
                        {totalQuestions > 0 ? `Question ${questionNumero} / ${totalQuestions}` : 'Aucune question disponible'}
                    </div>
                    <div style={{ color: '#334155', lineHeight: 1.5 }}>
                        {snap?.questionCourante?.enonce?.trim() || 'L’énoncé de la question active sera affiché ici.'}
                    </div>
                </div>
                {questionPills.length > 0 ? (
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                        {questionPills.map((num) => (
                            <button
                                key={num}
                                type="button"
                                disabled={isSubmitting || !estEnCours || num === questionNumero}
                                onClick={() => action(() => examenApi.allerAQuestion(id, num))}
                                style={{
                                    minWidth: 32,
                                    height: 32,
                                    borderRadius: 999,
                                    display: 'inline-flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    border: `1px solid ${num === questionNumero ? '#93c5fd' : '#dbe3f1'}`,
                                    background: num === questionNumero ? '#dbeafe' : '#fff',
                                    color: num === questionNumero ? '#1d4ed8' : '#475569',
                                    fontWeight: 700,
                                    fontSize: 12,
                                    cursor: isSubmitting || !estEnCours || num === questionNumero ? 'default' : 'pointer',
                                    opacity: isSubmitting || !estEnCours ? 0.7 : 1,
                                }}
                            >
                                {num}
                            </button>
                        ))}
                    </div>
                ) : null}
            </div>

            <button style={{ ...btn, alignSelf: 'flex-start' }} onClick={() => navigate('/')}>Retour</button>
        </div>
    )
}
