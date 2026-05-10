import { useCallback, useEffect, useRef, useState } from 'react'
import axios from 'axios'
import { Client } from '@stomp/stompjs'
import type {
    QuizQrLiveStatsPayload,
    QuizStatQuestion,
    StreamEnvelope,
} from '../types/quizLive'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:8081'
const WS_URL = `${API_BASE.replace(/^http/, 'ws')}/ws`

function mapStatRows(raw: unknown): QuizStatQuestion[] {
    if (!Array.isArray(raw)) return []
    return raw.map((r) => {
        const o = r as Record<string, unknown>
        return {
            questionId: Number(o.questionId ?? 0),
            numeroQuestion: Number(o.numeroQuestion ?? 0),
            questionEnonce: String(o.questionEnonce ?? ''),
            nombreReponses: Number(o.nombreReponses ?? 0),
            nombreCorrectes: Number(o.nombreCorrectes ?? 0),
            nombreIncorrectes: Number(o.nombreIncorrectes ?? 0),
            pourcentageReussite: Number(o.pourcentageReussite ?? 0),
            pourcentageEchec: Number(o.pourcentageEchec ?? 0),
        }
    })
}

function applyStatsPayload(p: QuizQrLiveStatsPayload | undefined, setters: {
    setStatsQuestions: (v: QuizStatQuestion[]) => void
    setNombreParticipants: (n: number) => void
    setQuizTitre: (t: string | null) => void
    setTotalSoumissions: (n: number) => void
    setTauxReussiteGlobal: (v: number | null) => void
}) {
    if (!p) return
    const rows = mapStatRows(p.statistiquesParQuestion)
    setters.setStatsQuestions(rows)
    setters.setNombreParticipants(p.nombreParticipants ?? 0)
    setters.setTotalSoumissions(p.totalSoumissionsQuestions ?? 0)
    setters.setTauxReussiteGlobal(
        typeof p.tauxReussiteGlobal === 'number' ? p.tauxReussiteGlobal : null,
    )
    if (p.quizTitre?.trim()) setters.setQuizTitre(p.quizTitre.trim())
}

/**
 * WebSocket STOMP sans JWT — topic `/topic/quiz-qr/{sessionToken}/stream`.
 * Enveloppes : STATS (stats), QUIZ (titre), CLOSED.
 */
export function useQuizLiveStats(sessionToken: string, enabled: boolean) {
    const [stats, setStats] = useState<QuizStatQuestion[]>([])
    const [quizTitre, setQuizTitre] = useState<string | null>(null)
    const [participantsConnectes, setParticipantsConnectes] = useState(0)
    const [totalSoumissionsQuestions, setTotalSoumissionsQuestions] = useState(0)
    const [tauxReussiteGlobal, setTauxReussiteGlobal] = useState<number | null>(null)
    const [sessionClosed, setSessionClosed] = useState(false)
    const [wsConnected, setWsConnected] = useState(false)
    const [wsError, setWsError] = useState<string | null>(null)

    const clientRef = useRef<Client | null>(null)

    const handleEnvelope = useCallback((msg: StreamEnvelope) => {
        if (msg.type === 'CLOSED' || msg.statut === 'TERMINE') {
            setSessionClosed(true)
            return
        }
        if (msg.stats) {
            applyStatsPayload(msg.stats, {
                setStatsQuestions: setStats,
                setNombreParticipants: setParticipantsConnectes,
                setQuizTitre,
                setTotalSoumissions: setTotalSoumissionsQuestions,
                setTauxReussiteGlobal,
            })
        }
        if (msg.type === 'QUIZ' && msg.quiz?.titre?.trim()) {
            setQuizTitre(msg.quiz.titre.trim())
        }
    }, [])

    useEffect(() => {
        if (!enabled || !sessionToken.trim() || sessionClosed) return

        setWsError(null)
        const client = new Client({
            brokerURL: WS_URL,
            reconnectDelay: 5000,
            heartbeatIncoming: 0,
            heartbeatOutgoing: 0,
            onConnect: () => {
                setWsConnected(true)
                client.subscribe(`/topic/quiz-qr/${sessionToken}/stream`, (frame) => {
                    try {
                        const msg = JSON.parse(frame.body) as StreamEnvelope
                        handleEnvelope(msg)
                    } catch {
                        /* ignore */
                    }
                })
            },
            onDisconnect: () => setWsConnected(false),
            onStompError: (frame) => {
                setWsError(frame.headers['message'] ?? 'Erreur STOMP')
            },
            onWebSocketError: () => {
                setWsError('Connexion WebSocket impossible (vérifiez que le serveur tourne).')
            },
        })

        clientRef.current = client
        client.activate()

        return () => {
            setWsConnected(false)
            clientRef.current = null
            void client.deactivate()
        }
    }, [enabled, sessionToken, sessionClosed, handleEnvelope])

    const seedFromSnapshot = useCallback((env: StreamEnvelope) => {
        handleEnvelope(env)
    }, [handleEnvelope])

    const closeSession = useCallback(async () => {
        const t = sessionToken.trim()
        if (!t) return
        try {
            await axios.delete(`${API_BASE}/api/qr-live/sessions/${encodeURIComponent(t)}`)
            setSessionClosed(true)
        } catch (e: unknown) {
            if (axios.isAxiosError(e) && e.response?.status === 404) {
                setSessionClosed(true)
                return
            }
            throw e
        }
    }, [sessionToken])

    return {
        stats,
        quizTitre,
        participantsConnectes,
        totalSoumissionsQuestions,
        tauxReussiteGlobal,
        sessionClosed,
        wsConnected,
        wsError,
        seedFromSnapshot,
        closeSession,
    }
}
