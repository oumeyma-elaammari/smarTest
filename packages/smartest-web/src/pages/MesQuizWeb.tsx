import { useEffect, useState } from 'react'
import { Loader2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import api from '../api/axiosConfig'

const sans = "'DM Sans', system-ui, sans-serif"

type MesQuizWebProps = {
    /** Même bleu que « Test » dans SmarTest */
    accentBleu?: string
}

export type QuizWebItem = {
    id: number
    titre: string
    duree: number
    statut?: string
    datePublication?: string
    professeurNom?: string
    nombreQuestions?: number
    /** false dès qu’une soumission a été enregistrée pour ce quiz */
    premiereTentative?: boolean
    /** % ; présent si plus première tentative */
    meilleurScore?: number | null
}

function formatScorePourcent(v: number): string {
    const x = Math.round(v * 10) / 10
    return Number.isInteger(x) ? String(Math.round(v)) : x.toFixed(1).replace('.', ',')
}

function useTwoColumnGrid() {
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
    return twoCol
}

export default function MesQuizWeb({ accentBleu = '#4f8ef7' }: MesQuizWebProps) {
    const navigate = useNavigate()
    const [items, setItems] = useState<QuizWebItem[]>([])
    const [loading, setLoading] = useState(true)
    const [err, setErr] = useState<string | null>(null)
    const twoCol = useTwoColumnGrid()

    useEffect(() => {
        let cancelled = false
        ;(async () => {
            try {
                const { data } = await api.get<QuizWebItem[]>('/api/quizs/mes-publications-web')
                if (!cancelled) setItems(Array.isArray(data) ? data : [])
            } catch (e: unknown) {
                const res = (e as { response?: { status?: number; data?: unknown } })?.response
                const data = res?.data
                let msg: string
                if (typeof data === 'string' && data.trim()) {
                    msg = data.trim()
                } else if (data && typeof data === 'object' && 'message' in data) {
                    const m = (data as { message?: unknown }).message
                    msg = typeof m === 'string' && m.trim() ? m.trim() : 'Impossible de charger vos quiz.'
                } else if (res?.status === 403) {
                    msg =
                        'Accès refusé : cette page est réservée aux comptes étudiant. Connectez-vous avec un compte étudiant.'
                } else if (res?.status === 401) {
                    msg = 'Session expirée. Reconnectez-vous.'
                } else {
                    msg = 'Impossible de charger vos quiz.'
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
                    lineHeight: 1.5,
                    width: '100%',
                    alignSelf: 'center',
                }}
            >
                {err}
            </p>
        )
    }

    if (items.length === 0) {
        return (
            <p
                style={{
                    fontFamily: sans,
                    color: '#64748b',
                    margin: 0,
                    maxWidth: 520,
                    lineHeight: 1.6,
                    textAlign: 'center',
                    width: '100%',
                    alignSelf: 'center',
                }}
            >
                Aucun quiz ne vous est proposé pour le moment. Lorsqu’un professeur vous aura ajouté à la liste
                d’emails autorisés, il apparaîtra ici.
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
                gridTemplateColumns: twoCol ? 'repeat(2, minmax(0, 1fr))' : 'minmax(0, 1fr)',
                gap: 20,
                boxSizing: 'border-box',
            }}
        >
            {items.map((q) => {
                const premiere = q.premiereTentative !== false
                return (
                <li
                    key={q.id}
                    style={{
                        fontFamily: sans,
                        border: '1px solid #e2e8f4',
                        borderRadius: 12,
                        padding: '18px 18px',
                        background: '#fff',
                        boxShadow: '0 1px 2px rgba(15, 30, 61, 0.04)',
                        textAlign: 'left',
                        width: '100%',
                        minWidth: 0,
                        boxSizing: 'border-box',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'flex-start',
                    }}
                >
                    <div style={{ fontWeight: 700, color: '#0f1e3d', fontSize: '1.05rem', marginBottom: 8 }}>
                        {q.titre.trim().toLowerCase().startsWith('quiz') ? q.titre : `Quiz — ${q.titre}`}
                    </div>
                    <div
                        style={{
                            fontSize: 13,
                            color: '#64748b',
                            display: 'flex',
                            flexWrap: 'wrap',
                            gap: '5px 8px',
                            justifyContent: 'flex-start',
                            alignItems: 'center',
                            marginBottom: 2,
                        }}
                    >
                        {q.professeurNom ? (
                            <span style={{ fontWeight: 500 }}>
                                <span style={{ color: '#64748b' }}>Prof. </span>
                                <span style={{ fontWeight: 600 }}>{q.professeurNom}</span>
                            </span>
                        ) : null}
                        {q.professeurNom && q.nombreQuestions != null ? (
                            <span style={{ color: '#cbd5e1', userSelect: 'none' }} aria-hidden>
                                ·
                            </span>
                        ) : null}
                        {q.nombreQuestions != null ? <span>{q.nombreQuestions} question(s)</span> : null}
                    </div>
                    <p
                        style={{
                            margin: '10px 0 0',
                            fontSize: 13,
                            color: '#94a3b8',
                            lineHeight: 1.5,
                            width: '100%',
                            textAlign: 'left',
                        }}
                    >
                        {premiere ? (
                            <><span style={{ color: accentBleu, fontWeight: 600 }}> Nouveau quiz — Lancez le quand vous êtes prêt !  </span></>
                        ) : q.meilleurScore != null ? (
                            <>
                                Meilleur score :{' '}
                                <span style={{ color: accentBleu, fontWeight: 600 }}>
                                    {formatScorePourcent(q.meilleurScore)} %
                                </span>
                            </>
                        ) : (
                            'Meilleur score : —'
                        )}
                    </p>
                    <button
                        type="button"
                        onClick={() => navigate(`/quiz/${q.id}`)}
                        style={{
                            marginTop: 12,
                            alignSelf: 'flex-start',
                            padding: '4px 7px',
                            borderRadius: 5,
                            border: 'none',
                            cursor: 'pointer',
                            background: '#0f1e3d',
                            color: '#fff',
                            fontWeight: 400,
                            fontSize: 13,
                            fontFamily: sans,
                            width: '30%'
                        }}
                    >
                        Commencer
                    </button>
                </li>
                )
            })}
        </ul>
    )
}
