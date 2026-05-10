import axios from 'axios'
import { useCallback, useEffect, useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { Loader2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { quizApi, type QuizWebItem } from '../api/quizApi'
import { quizWebListSchema } from '../api/quizSchemas'
import { QuizCard } from '../components/quiz/QuizCard'

const sans = "'DM Sans', system-ui, sans-serif"

type MesQuizWebProps = {
    /** Même bleu que « Test » dans SmarTest */
    accentBleu?: string
}

export type { QuizWebItem }

const PAGE_SIZE = 10

/** Rafraîchit la liste pour refléter publications / suppressions côté serveur (désactivé en tests Vitest). */
const MES_QUIZ_POLL_MS = import.meta.env.MODE === 'test' ? 999999999 : 10_000

function mesPublicationsWebErrorMessage(e: unknown): string {
    const res = (e as { response?: { status?: number; data?: unknown } })?.response
    const data = res?.data
    if (typeof data === 'string' && data.trim()) return data.trim()
    if (data && typeof data === 'object' && 'message' in data) {
        const m = (data as { message?: unknown }).message
        return typeof m === 'string' && m.trim() ? m.trim() : 'Impossible de charger vos quiz.'
    }
    if (res?.status === 403) {
        return 'Accès refusé : cette page est réservée aux comptes étudiant. Connectez-vous avec un compte étudiant.'
    }
    if (res?.status === 401) return 'Non autorisé'
    return 'Impossible de charger vos quiz.'
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
    const [search, setSearch] = useState('')
    const [page, setPage] = useState(1)
    const twoCol = useTwoColumnGrid()

    const filteredItems = useMemo(() => {
        const q = search.trim().toLowerCase()
        if (!q) return items
        return items.filter((it) => it.titre.toLowerCase().includes(q))
    }, [items, search])

    const totalPages = Math.max(1, Math.ceil(filteredItems.length / PAGE_SIZE))

    useEffect(() => {
        setPage(1)
    }, [search])

    useEffect(() => {
        if (page > totalPages) setPage(totalPages)
    }, [page, totalPages])

    const pageSlice = useMemo(
        () => filteredItems.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
        [filteredItems, page],
    )

    const fetchMesPublications = useCallback(async (opts?: { silent?: boolean; signal?: AbortSignal }) => {
        const silent = opts?.silent ?? false
        if (!silent) setLoading(true)
        try {
            const { data } = await quizApi.getMesPublicationsWeb(opts?.signal)
            const parsed = quizWebListSchema.safeParse(data)
            if (!parsed.success) {
                if (!silent) setErr('Format de réponse inattendu du serveur.')
                return
            }
            setItems(parsed.data)
            setErr(null)
        } catch (e: unknown) {
            if (axios.isCancel(e)) return
            if (!silent) setErr(mesPublicationsWebErrorMessage(e))
        } finally {
            if (!silent) setLoading(false)
        }
    }, [])

    useEffect(() => {
        const controller = new AbortController()
        void fetchMesPublications({ silent: false, signal: controller.signal })
        return () => controller.abort()
    }, [fetchMesPublications])

    useEffect(() => {
        const id = window.setInterval(() => void fetchMesPublications({ silent: true }), MES_QUIZ_POLL_MS)
        return () => window.clearInterval(id)
    }, [fetchMesPublications])

    const enveloppe = (corps: ReactNode) => (
        <div
            style={{
                width: '100%',
                maxWidth: 1040,
                margin: '0 auto',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'stretch',
                boxSizing: 'border-box',
            }}
        >
            <div
                style={{
                    width: '100%',
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'stretch',
                }}
            >
                {corps}
            </div>
        </div>
    )

    if (loading) {
        return enveloppe(
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
            </div>,
        )
    }

    if (err) {
        return enveloppe(
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
            </p>,
        )
    }

    if (items.length === 0) {
        return enveloppe(
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
                Aucun quiz ne vous est proposé pour le moment. Lorsqu’un professeur vous aura ajouté a un quiz, il vous sera proposé.
            </p>,
        )
    }

    const searchWrap = (
        <div style={{ width: '100%', marginBottom: 6 }}>
        
            <input
                id="mes-quiz-search"
                type="search"
                autoComplete="off"
                placeholder="Titre du quiz…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                aria-label="Rechercher un quiz"
                style={{
                    width: '100%',
                    maxWidth: 400,
                    height: 40,
                    padding: '0 12px',
                    borderRadius: 10,
                    border: '1px solid #e2e8f4',
                    fontFamily: sans,
                    fontSize: 14,
                    color: '#0f1e3d',
                    background: '#fff',
                    outline: 'none',
                    boxSizing: 'border-box',
                }}
            />
        </div>
    )

    if (filteredItems.length === 0) {
        return enveloppe(
            <div style={{ width: '100%', display: 'flex', flexDirection: 'column', alignItems: 'stretch' }}>
                {searchWrap}
                <p
                    style={{
                        fontFamily: sans,
                        color: '#64748b',
                        margin: '12px 0 0',
                        lineHeight: 1.6,
                        textAlign: 'left',
                        width: '100%',
                    }}
                >
                    Aucun quiz ne correspond à «{' '}
                    <strong style={{ color: '#0f1e3d' }}>{search.trim()}</strong> ».
                </p>
                <button
                    type="button"
                    onClick={() => setSearch('')}
                    style={{
                        alignSelf: 'flex-start',
                        marginTop: 12,
                        fontFamily: sans,
                        fontSize: 13,
                        fontWeight: 600,
                        color: accentBleu,
                        background: 'none',
                        border: 'none',
                        cursor: 'pointer',
                        padding: 0,
                        textDecoration: 'underline',
                    }}
                >
                    Effacer la recherche
                </button>
            </div>,
        )
    }

    const btnPager: CSSProperties = {
        padding: '6px 14px',
        borderRadius: 8,
        border: '1px solid #e2e8f4',
        background: '#fff',
        color: '#0f1e3d',
        fontFamily: sans,
        fontSize: 13,
        cursor: 'pointer',
        fontWeight: 600,
    }

    return enveloppe(
        <div style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: 0 }}>
            {searchWrap}
            <ul
                style={{
                    listStyle: 'none',
                    margin: '12px 0 0',
                    padding: 0,
                    width: '100%',
                    display: 'grid',
                    gridTemplateColumns: twoCol ? 'repeat(2, minmax(0, 1fr))' : 'minmax(0, 1fr)',
                    gap: 20,
                    boxSizing: 'border-box',
                }}
            >
            {pageSlice.map((q) => (
                <QuizCard
                    key={q.id}
                    item={q}
                    accentBleu={accentBleu}
                    onStart={(id: number) => navigate(`/quiz/${id}`)}
                />
            ))}
            </ul>
            {totalPages > 1 ? (
                <nav
                    role="navigation"
                    aria-label="Pagination des quiz"
                    style={{
                        marginTop: 24,
                        display: 'flex',
                        flexWrap: 'wrap',
                        alignItems: 'center',
                        justifyContent: 'center',
                        gap: 14,
                        fontFamily: sans,
                        fontSize: 13,
                        color: '#64748b',
                        width: '100%',
                        boxSizing: 'border-box',
                        textAlign: 'center',
                    }}
                >
                    <button
                        type="button"
                        style={{ ...btnPager, opacity: page <= 1 ? 0.45 : 1 }}
                        disabled={page <= 1}
                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                    >
                        Précédent
                    </button>
                    <span style={{ textAlign: 'center', minWidth: 0 }}>
                        Page {page} / {totalPages}
                        {' · '}
                        {filteredItems.length} quiz
                        {filteredItems.length !== items.length ? ` (${items.length} au total)` : ''}
                    </span>
                    <button
                        type="button"
                        style={{ ...btnPager, opacity: page >= totalPages ? 0.45 : 1 }}
                        disabled={page >= totalPages}
                        onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    >
                        Suivant
                    </button>
                </nav>
            ) : null}
        </div>,
    )
}
