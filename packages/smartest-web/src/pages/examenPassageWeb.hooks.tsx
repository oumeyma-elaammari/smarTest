import type { Dispatch, MutableRefObject, SetStateAction } from 'react'
import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Client } from '@stomp/stompjs'
import { examenApi } from '../api/examenApi'
import {
    examenMetaSchema,
    examenSnapshotSchema,
    mapQuestionStateToSnapshot,
    type ExamenMeta,
    type ExamenSnapshot,
} from '../api/quizSchemas'
import { parseDebutExamenMs } from '../utils/examenDisplay'
import useAuth from '../hooks/useAuth'
import { stompBrokerUrl } from '../config/runtimeBackend'
import {
    joinedStorageKey,
    readEtudiantEmail,
    readEtudiantId,
    resolveEtudiantId,
    extractApiMessage,
    isSessionPhaseTerminee,
    soumettreExamenFinalAvecRetry,
} from './examenPassageWeb.shared'

export function useExamenMetaLoad(
    id: number,
    setMeta: (m: ExamenMeta | null) => void,
    setLoading: (v: boolean) => void,
    setStatus: (v: string) => void,
): void {
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
                    const raw = r.data as Record<string, unknown> | null
                    const idRaw = raw?.id
                    if (raw && typeof idRaw === 'number' && Number.isFinite(idRaw)) {
                        const titreRaw = raw.titre
                        setMeta({
                            id: idRaw,
                            titre: typeof titreRaw === 'string' ? titreRaw.trim() : '',
                            description:
                                typeof raw.description === 'string' ? raw.description : undefined,
                            dateDebut: raw.dateDebut as ExamenMeta['dateDebut'],
                            duree: typeof raw.duree === 'number' ? raw.duree : undefined,
                            totalQuestions:
                                typeof raw.totalQuestions === 'number' ? raw.totalQuestions : undefined,
                            statut: typeof raw.statut === 'string' ? raw.statut : undefined,
                            bareme: typeof raw.bareme === 'number' ? raw.bareme : undefined,
                            professeurNom:
                                typeof raw.professeurNom === 'string' ? raw.professeurNom : undefined,
                        })
                    } else {
                        setMeta(null)
                        setStatus('Informations de l’examen invalides.')
                    }
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
    }, [id, setMeta, setLoading, setStatus])
}

export function useResetAutoJoinOnId(id: number, autoJoinReussi: MutableRefObject<boolean>): void {
    useEffect(() => {
        autoJoinReussi.current = false
    }, [id, autoJoinReussi])
}

export function useExamenPolling(
    id: number,
    meta: ExamenMeta | null,
    setJoined: (v: boolean) => void,
    setSnap: (v: ExamenSnapshot | null) => void,
): void {
    const authUserId = useAuth((s) => s.userId)

    useEffect(() => {
        if (!Number.isFinite(id) || id <= 0 || !meta) return

        const syncPresence = (payload: unknown) => {
            const etudiantId = resolveEtudiantId(authUserId)
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

            const etudiantIdPoll = resolveEtudiantId(authUserId)
            examenApi
                .getQuestionCourante(id, etudiantIdPoll)
                .then((r) => {
                    const mapped = mapQuestionStateToSnapshot(r.data)
                    if (mapped) {
                        setSnap(mapped)
                        if (isSessionPhaseTerminee(mapped.etat, null)) {
                            const etu = readEtudiantId()
                            if (etu > 0) void soumettreExamenFinalAvecRetry(id, etu)
                        }
                    }
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
        const t = globalThis.setInterval(runPolling, 2500)
        return () => globalThis.clearInterval(t)
    }, [id, meta, authUserId, setJoined, setSnap])
}

export function useExamenStomp(
    id: number,
    setSnap: (v: ExamenSnapshot | null) => void,
    setJoined: (v: boolean) => void,
    setWsNotice: (v: string | null) => void,
): void {
    const authToken = useAuth((s) => s.token)
    const authUserId = useAuth((s) => s.userId)

    useEffect(() => {
        if (!Number.isFinite(id) || id <= 0) return
        const etudiantId = resolveEtudiantId(authUserId)
        let cancelled = false

        const client = new Client({
            brokerURL: stompBrokerUrl(),
            reconnectDelay: 3000,
            connectHeaders: authToken ? { Authorization: `Bearer ${authToken}` } : {},
            onConnect: () => {
                client.subscribe(`/topic/examen/${id}/etat`, (message) => {
                    try {
                        const raw = JSON.parse(message.body) as unknown
                        const parsed = examenSnapshotSchema.safeParse(raw)
                        if (cancelled) return
                        if (parsed.success) {
                            setSnap(parsed.data)
                            setWsNotice(null)
                            if (isSessionPhaseTerminee(parsed.data.etat, null)) {
                                const etu = readEtudiantId()
                                if (etu > 0) void soumettreExamenFinalAvecRetry(id, etu)
                            }
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
    }, [id, setSnap, setJoined, setWsNotice, authToken])
}

export function useSnapClearsJoinedOnArrete(snapEtat: string | undefined, id: number, setJoined: (v: boolean) => void): void {
    useEffect(() => {
        const et = (snapEtat ?? '').toUpperCase()
        if (et !== 'ARRETE') return
        setJoined(false)
        try {
            sessionStorage.removeItem(joinedStorageKey(id))
        } catch {
            /* ignore */
        }
    }, [snapEtat, id, setJoined])
}

export function useNavigateVersEpreuveSurDemarrage(
    isEpreuve: boolean,
    snapEtat: string | undefined,
    id: number,
): void {
    const navigate = useNavigate()

    useEffect(() => {
        if (isEpreuve) return
        const et = (snapEtat ?? '').toUpperCase()
        if (et === 'EN_COURS' || et === 'EN_PAUSE') {
            navigate(`/examen/${id}/epreuve`, { replace: true })
        }
    }, [snapEtat, id, navigate, isEpreuve])
}

export function useNavigateEpreuveSiMetaDejaEnCours(
    loading: boolean,
    isEpreuve: boolean,
    metaStatut: string | undefined,
    id: number,
): void {
    const navigate = useNavigate()

    useEffect(() => {
        if (loading) return
        if (isEpreuve) return
        const s = (metaStatut ?? '').trim().toUpperCase()
        if (s === 'EN_COURS' || s === 'EN_PAUSE') {
            navigate(`/examen/${id}/epreuve`, { replace: true })
        }
    }, [loading, isEpreuve, metaStatut, id, navigate])
}

export function useRetourAttenteSiEpreuveTropTot(
    isEpreuve: boolean,
    snap: ExamenSnapshot | null,
    id: number,
): void {
    const navigate = useNavigate()

    useEffect(() => {
        if (!isEpreuve || snap == null) return
        const et = (snap.etat ?? '').toUpperCase()
        if (et === 'PLANIFIE') {
            navigate(`/examen/${id}`, { replace: true })
        }
    }, [snap, id, navigate, isEpreuve])
}

/** Envoie la copie au serveur dès que la session passe à TERMINE (persistance BDD pour Sessions actives). */
export function useSoumettreFinalSiTermine(
    id: number,
    snapEtat: string | undefined,
    metaStatut: string | undefined,
    setStatus: (v: string) => void,
): void {
    const authUserId = useAuth((s) => s.userId)
    const sessionTerminee = isSessionPhaseTerminee(snapEtat, metaStatut)

    useEffect(() => {
        if (!sessionTerminee || !Number.isFinite(id) || id <= 0) return
        const etudiantId = resolveEtudiantId(authUserId)
        if (!Number.isFinite(etudiantId) || etudiantId <= 0) {
            setStatus('Examen terminé.')
            return
        }
        let cancelled = false
        ;(async () => {
            try {
                const ok = await soumettreExamenFinalAvecRetry(id, etudiantId)
                if (!cancelled && ok) setStatus('Copie envoyée.')
            } catch (e: unknown) {
                if (!cancelled) {
                    setStatus(extractApiMessage(e, 'Impossible d’envoyer votre copie.'))
                }
            }
        })()
        return () => {
            cancelled = true
        }
    }, [id, sessionTerminee, snapEtat, metaStatut, authUserId, setStatus])
}

export function useRedirectDashboardSiArrete(snapEtat: string | undefined): void {
    const navigate = useNavigate()

    useEffect(() => {
        const et = (snapEtat ?? '').toUpperCase()
        if (et !== 'ARRETE') return
        const t = globalThis.setTimeout(() => navigate('/dashboard', { replace: true }), 700)
        return () => globalThis.clearTimeout(t)
    }, [snapEtat, navigate])
}

export function useAutoJoinSalleAttente(opts: {
    id: number
    meta: ExamenMeta | null
    creneauOkPourJoin: boolean
    creneauTick: number
    snapEtat: string | undefined
    autoJoinReussi: MutableRefObject<boolean>
    setJoined: (v: boolean) => void
    setStatus: (v: string) => void
}): void {
    const navigate = useNavigate()
    const authUserId = useAuth((s) => s.userId)
    const { id, meta, creneauOkPourJoin, creneauTick, snapEtat, autoJoinReussi, setJoined, setStatus } = opts

    useEffect(() => {
        if (!Number.isFinite(id) || id <= 0 || !meta) return
        if (!creneauOkPourJoin) return
        if (autoJoinReussi.current) return
        const phase = (snapEtat ?? '').toUpperCase()
        const metaStatut = (meta?.statut ?? '').trim().toUpperCase()
        if (phase === 'TERMINE' || phase === 'ARRETE') return
        if (metaStatut === 'TERMINE' || metaStatut === 'ANNULE') return

        const etudiantId = resolveEtudiantId(authUserId)
        if (!Number.isFinite(etudiantId) || etudiantId <= 0) {
            setStatus('Connexion incomplète. Déconnectez-vous puis reconnectez-vous.')
            return
        }

        let cancelled = false
        ;(async () => {
            try {
                const email = readEtudiantEmail()
                if (!email) {
                    setStatus('Reconnectez-vous pour continuer.')
                    return
                }
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
                if (!cancelled)
                    setStatus(extractApiMessage(e, 'Connexion à la salle d’attente en cours…'))
            }
        })()
        return () => {
            cancelled = true
        }
    }, [
        id,
        meta,
        creneauOkPourJoin,
        creneauTick,
        snapEtat,
        authUserId,
        navigate,
        autoJoinReussi,
        setJoined,
        setStatus,
    ])
}

export function useJoinedSiSessionActive(snapEtat: string | undefined, setJoined: (v: boolean) => void): void {
    useEffect(() => {
        const p = (snapEtat ?? '').toUpperCase()
        if (p === 'EN_COURS' || p === 'EN_PAUSE') {
            setJoined(true)
        }
    }, [snapEtat, setJoined])
}

export function useExamAnswerReset(questionId: number | null, reset: () => void): void {
    const prevQuestionIdRef = useRef<number | null>(null)
    useEffect(() => {
        if (questionId == null) {
            reset()
            prevQuestionIdRef.current = null
            return
        }
        const prev = prevQuestionIdRef.current
        if (prev != null && prev !== questionId) {
            reset()
        }
        prevQuestionIdRef.current = questionId
    }, [questionId, reset])
}

export function useCreneauTicker(setCreneauTick: Dispatch<SetStateAction<number>>): void {
    useEffect(() => {
        const timer = globalThis.setInterval(() => setCreneauTick((n) => n + 1), 4000)
        return () => globalThis.clearInterval(timer)
    }, [setCreneauTick])
}

export function deriveIsEpreuvePath(pathname: string): boolean {
    return /\/examen\/[^/]+\/epreuve\/?$/.test(pathname)
}
