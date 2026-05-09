import { useEffect, useState } from 'react'
import { Loader2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import api from '../api/axiosConfig'
import { ExamenListeCard } from '../components/examen/ExamenListeCard'
import { examenListeItemSchema, type ExamenListeItem } from '../api/quizSchemas'
import type { ExamenMeta } from '../api/quizSchemas'
import { parseDebutExamenMs } from '../utils/examenDisplay'

const sans = "'DM Sans', system-ui, sans-serif"

type MesExamensWebProps = {
    accentBleu?: string
}

/** Convertit la métadonnée API en carte liste (professeur imbriqué). */
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
    const [items, setItems] = useState<ExamenMeta[]>([])
    const [loading, setLoading] = useState(true)
    const [err, setErr] = useState<string | null>(null)
    const [twoCol, setTwoCol] = useState(() =>
        typeof window !== 'undefined' ? window.matchMedia('(min-width: 700px)').matches : true,
    )

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
                    msg =
                        'Accès refusé : cette page est réservée aux comptes étudiant. Connectez-vous avec un compte étudiant.'
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
    }, [])

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
                Aucun examen web ne vous est assigné pour le moment.
            </p>
        )
    }

    return (
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
                return (
                    <ExamenListeCard
                        key={m.id}
                        examen={examen}
                        accentBleu={accentBleu}
                        layoutTwoCol={twoCol}
                        creneauAtteint={creneauAtteint}
                        onRejoindre={() => navigate(`/examen/${m.id}`)}
                    />
                )
            })}
        </ul>
    )
}
