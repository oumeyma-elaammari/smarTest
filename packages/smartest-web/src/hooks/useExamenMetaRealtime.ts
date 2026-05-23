import type { Dispatch, SetStateAction } from 'react'
import { useEffect } from 'react'
import { Client } from '@stomp/stompjs'
import { examenApi } from '../api/examenApi'
import { examenMetaSchema, type ExamenMeta } from '../api/quizSchemas'
import useAuth from './useAuth'
import { stompBrokerUrl } from '../config/runtimeBackend'

const LISTE_REFRESH_MS = 20_000
const META_POLL_MS = 15_000

function mergeExamenMeta(prev: ExamenMeta | null, incoming: Partial<ExamenMeta>): ExamenMeta | null {
    if (!prev) return null
    const next: ExamenMeta = { ...prev }
    if (incoming.dateDebut !== undefined) next.dateDebut = incoming.dateDebut
    if (incoming.duree !== undefined) next.duree = incoming.duree
    if (incoming.statut !== undefined) next.statut = incoming.statut
    if (incoming.titre !== undefined) next.titre = incoming.titre
    if (incoming.description !== undefined) next.description = incoming.description
    return next
}

function parseMetaPayload(raw: unknown): ExamenMeta | null {
    const parsed = examenMetaSchema.safeParse(raw)
    if (parsed.success) return parsed.data
    if (raw && typeof raw === 'object') {
        const o = raw as Record<string, unknown>
        if (typeof o.id === 'number' && Number.isFinite(o.id)) {
            return mergeExamenMeta(
                {
                    id: o.id,
                    titre: typeof o.titre === 'string' ? o.titre.trim() : '',
                },
                {
                    dateDebut: o.dateDebut as ExamenMeta['dateDebut'],
                    duree: typeof o.duree === 'number' ? o.duree : undefined,
                    statut: typeof o.statut === 'string' ? o.statut : undefined,
                    titre: typeof o.titre === 'string' ? o.titre.trim() : undefined,
                    description: typeof o.description === 'string' ? o.description : undefined,
                },
            )
        }
    }
    return null
}

/** Abonnement STOMP aux mises à jour de créneau / métadonnées pour un examen. */
export function useExamenMetaStomp(
    examenId: number,
    setMeta: Dispatch<SetStateAction<ExamenMeta | null>>,
): void {
    const authToken = useAuth((s) => s.token)

    useEffect(() => {
        if (!Number.isFinite(examenId) || examenId <= 0) return
        let cancelled = false

        const client = new Client({
            brokerURL: stompBrokerUrl(),
            reconnectDelay: 3000,
            connectHeaders: authToken ? { Authorization: `Bearer ${authToken}` } : {},
            onConnect: () => {
                client.subscribe(`/topic/examen/${examenId}/metadata`, (message) => {
                    try {
                        const raw = JSON.parse(message.body) as unknown
                        if (cancelled) return
                        const parsed = parseMetaPayload(raw)
                        if (parsed) {
                            setMeta((prev) => mergeExamenMeta(prev, parsed) ?? parsed)
                        }
                    } catch {
                        /* polling de repli actif */
                    }
                })
            },
        })

        client.activate()
        return () => {
            cancelled = true
            client.deactivate()
        }
    }, [examenId, authToken, setMeta])
}

/** Rafraîchit périodiquement les métadonnées (repli si WebSocket indisponible). */
export function useExamenMetaPoll(
    examenId: number,
    setMeta: Dispatch<SetStateAction<ExamenMeta | null>>,
): void {
    useEffect(() => {
        if (!Number.isFinite(examenId) || examenId <= 0) return

        const refresh = () => {
            examenApi
                .getMetadata(examenId)
                .then((r) => {
                    const parsed = parseMetaPayload(r.data)
                    if (parsed) setMeta((prev) => mergeExamenMeta(prev, parsed) ?? parsed)
                })
                .catch(() => undefined)
        }

        const t = globalThis.setInterval(refresh, META_POLL_MS)
        return () => globalThis.clearInterval(t)
    }, [examenId, setMeta])
}

/** Rafraîchit la liste des examens pour refléter les changements de créneau côté serveur. */
export function useExamensListePolling(chargerExamens: () => void | Promise<void>, actif = true): void {
    useEffect(() => {
        if (!actif) return
        const t = globalThis.setInterval(() => {
            void chargerExamens()
        }, LISTE_REFRESH_MS)
        return () => globalThis.clearInterval(t)
    }, [chargerExamens, actif])
}
