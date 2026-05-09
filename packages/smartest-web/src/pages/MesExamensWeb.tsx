import axios from 'axios'
import { useEffect, useState } from 'react'
import { Loader2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import api from '../api/axiosConfig'
import { examenApi } from '../api/examenApi'
import { ExamenListeCard } from '../components/examen/ExamenListeCard'
import { examenListeItemSchema, type ExamenListeItem } from '../api/quizSchemas'
import type { ExamenMeta } from '../api/quizSchemas'
import { parseDebutExamenMs } from '../utils/examenDisplay'
import useAuth from '../hooks/useAuth'

const sans = "'DM Sans', system-ui, sans-serif"

type MesExamensWebProps = {
    accentBleu?: string
}

/** Convertit la métadonnée API en carte liste (professeur imbriqué). */
function extractActionError(e: unknown, fallback: string): string {
    if (axios.isAxiosError(e)) {
        const data = e.response?.data
        if (data && typeof data === 'object' && 'message' in data) {
            const msg = (data as { message?: unknown }).message
            if (typeof msg === 'string' && msg.trim()) return msg.trim()
        }
        if (typeof data === 'string' && data.trim()) return data.trim()
    }
    if (e instanceof Error && e.message.trim()) return e.message.trim()
    return fallback
}

function metaToListeItem(m: ExamenMeta): ExamenListeItem {
    const parsed = examenListeItemSchema.safeParse({
        id: m.id,
        titre: m.titre,
        description: m.description ?? null,
        duree: m.duree ?? null,
        statut: m.statut ?? null,
        dateDebut: m.dateDebut,
        dateFin: m.dateDebut,
        dateCreation: undefined,
        professeur: m.professeurNom ? { nom: m.professeurNom } : null,
    })
    return parsed.success ? parsed.data : ({} as ExamenListeItem)
}

export default function MesExamensWeb({ accentBleu = '#4f8ef7' }: MesExamensWebProps) {
    const navigate = useNavigate()
    const role = useAuth((s) => s.role)
    const isProfesseurWeb = (role ?? '').trim().toUpperCase() === 'PROFESSEUR'
    const [items, setItems] = useState<ExamenMeta[]>([])
    const [loading, setLoading] = useState(true)
    const [err, setErr] = useState<string | null>(null)
    const [twoCol, setTwoCol] = useState(() =>
        typeof window !== 'undefined' ? window.matchMedia('(min-width: 700px)').matches : true,
    )
    const [lancementExamenId, setLancementExamenId] = useState<number | null>(null)
    const [actionErr, setActionErr] = useState<string | null>(null)

    useEffect(() => {
        const mq = window.matchMedia('(min-width: 700px)')
        const apply = () => setTwoCol(mq.matches)
        apply()
        mq.addEventListener('change', apply)
        return () => mq.removeEventListener('change', apply)
    }, [])

    useEffect(() => {
        let cancelled = false
        ;(async () => {
            try {
                const { data } = await api.get<ExamenMeta[]>('/api/examens-publies/mes-publications-web')
                if (!cancelled) setItems(Array.isArray(data) ? data : [])
            } catch (e: unknown) {
                const res = (e as { response?: { status?: number; data?: unknown } })?.response
                const data = res?.data
                let msg: string
                if (typeof data === 'string' && data.trim()) {
                    msg = data.trim()
                } else if (data && typeof data === 'object' && 'message' in data) {
                    const m = (data as { message?: unknown }).message
                    msg = typeof m === 'string' && m.trim() ? m.trim() : 'Impossible de charger vos examens.'
                } else if (res?.status === 403) {
                    msg = isProfesseurWeb
                        ? 'Accès refusé : impossible de charger vos examens avec ce compte.'
                        : 'Accès refusé : cette liste est réservée aux comptes autorisés pour ces examens.'
                } else if (res?.status === 401) {
                    msg = 'Session expirée. Reconnectez-vous.'
                } else {
                    msg = 'Impossible de charger vos examens.'
                }
                if (!cancelled) setErr(msg)
            } finally {
                if (!cancelled) setLoading(false)
            }
        })()
        return () => {
            cancelled = true
        }
    }, [isProfesseurWeb])

    const lancerEtPiloter = async (m: ExamenMeta) => {
        setActionErr(null)
        setLancementExamenId(m.id)
        try {
            await examenApi.lancer(m.id)
            navigate(`/supervision/examen/${m.id}?started=1`, { replace: true })
        } catch (e: unknown) {
            setActionErr(
                extractActionError(
                    e,
                    'Impossible de lancer la session. Vérifiez le créneau ou ouvrez l’espace superviseur pour plus de détails.',
                ),
            )
        } finally {
            setLancementExamenId(null)
        }
    }

    if (loading) {
        return (
            <div
                style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    gap: 10,
                    fontFamily: sans,
                    color: '#64748b',
                    width: '100%',
                    padding: '1rem 0',
                }}
            >
                <Loader2 size={20} className="animate-spin" aria-hidden />
                Chargement…
            </div>
        )
    }

    if (err) {
        return (
            <p
                style={{
                    fontFamily: sans,
                    color: '#b91c1c',
                    margin: 0,
                    textAlign: 'center',
                    maxWidth: 520,
                    alignSelf: 'center',
                }}
            >
                {err}
            </p>
        )
    }

    if (items.length === 0) {
        return (
            <p style={{ fontFamily: sans, color: '#64748b', margin: 0, textAlign: 'center' }}>
                {isProfesseurWeb
                    ? 'Aucun examen publié sur le web depuis votre espace bureau. Créez et publiez avec les emails invités pour les voir ici.'
                    : 'Aucun examen web ne vous est assigné pour le moment.'}
            </p>
        )
    }

    return (
        <>
            {actionErr ? (
                <p
                    style={{
                        fontFamily: sans,
                        color: '#b45309',
                        background: '#fffbeb',
                        border: '1px solid #fde68a',
                        borderRadius: 10,
                        padding: '12px 14px',
                        margin: '0 0 14px',
                        width: '100%',
                        boxSizing: 'border-box',
                    }}
                >
                    {actionErr}
                </p>
            ) : null}
            <ul
                style={{
                    listStyle: 'none',
                    margin: 0,
                    padding: 0,
                    width: '100%',
                    display: 'grid',
                    gridTemplateColumns: twoCol ? 'repeat(2, minmax(0, 1fr))' : '1fr',
                    gap: 14,
                    alignItems: 'stretch',
                }}
            >
                {items.map((m) => {
                    const examen = metaToListeItem(m)
                    const debutMs = parseDebutExamenMs(m.dateDebut)
                    const creneauAtteint = debutMs == null ? true : Date.now() >= debutMs
                    const statutDb = (m.statut ?? '').trim().toUpperCase()
                    const peutLancerListe = statutDb === 'PLANIFIE'

                    return (
                        <ExamenListeCard
                            key={m.id}
                            examen={examen}
                            accentBleu={accentBleu}
                            layoutTwoCol={twoCol}
                            creneauAtteint={creneauAtteint}
                            onRejoindre={() =>
                                navigate(isProfesseurWeb ? `/supervision/examen/${m.id}` : `/examen/${m.id}`)
                            }
                            superviseurProps={
                                isProfesseurWeb
                                    ? {
                                          onOuvrirPilotage: () => navigate(`/supervision/examen/${m.id}`),
                                          onLancerSession: () => lancerEtPiloter(m),
                                          lancementEnCours: lancementExamenId === m.id,
                                          peutLancerSession: peutLancerListe,
                                      }
                                    : undefined
                            }
                        />
                    )
                })}
            </ul>
        </>
    )
}
