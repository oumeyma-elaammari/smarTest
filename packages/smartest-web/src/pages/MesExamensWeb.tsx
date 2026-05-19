import axios from 'axios'
import { useCallback, useEffect, useMemo, useState, type CSSProperties } from 'react'
import { Loader2 } from 'lucide-react'
import toast from 'react-hot-toast'
import { useNavigate } from 'react-router-dom'
import api from '../api/axiosConfig'
import { examenApi } from '../api/examenApi'
import { ExamenListeCard } from '../components/examen/ExamenListeCard'
import { examenListeItemSchema, type ExamenListeItem, type ExamenMeta } from '../api/quizSchemas'
import { parseDebutExamenMs } from '../utils/examenDisplay'
import useAuth from '../hooks/useAuth'

const sans = "'DM Sans', system-ui, sans-serif"
const RECENTS_COUNT = 5

type ExamenFiltre = 'recents' | 'a_faire' | 'en_cours' | 'termine' | 'tous'

const FILTRE_OPTIONS: { id: ExamenFiltre; label: string }[] = [
    { id: 'recents', label: 'Récents' },
    { id: 'a_faire', label: 'À faire' },
    { id: 'en_cours', label: 'En cours' },
    { id: 'termine', label: 'Terminés' },
    { id: 'tous', label: 'Tous' },
]

function normaliserStatutExamen(statut?: string | null): string {
    return (statut ?? '').trim().toUpperCase()
}

function trierExamensParDateRecente(liste: ExamenMeta[]): ExamenMeta[] {
    return [...liste].sort((a, b) => {
        const ta = parseDebutExamenMs(a.dateDebut) ?? 0
        const tb = parseDebutExamenMs(b.dateDebut) ?? 0
        if (tb !== ta) return tb - ta
        return b.id - a.id
    })
}

function filtrerExamensParCategorie(liste: ExamenMeta[], filtre: ExamenFiltre): ExamenMeta[] {
    const tries = trierExamensParDateRecente(liste)
    if (filtre === 'recents') return tries.slice(0, RECENTS_COUNT)
    if (filtre === 'tous') return tries
    return tries.filter((m) => {
        const s = normaliserStatutExamen(m.statut)
        if (filtre === 'a_faire') return s === 'PLANIFIE'
        if (filtre === 'en_cours') return s === 'EN_COURS' || s === 'EN_PAUSE'
        if (filtre === 'termine') return s === 'TERMINE'
        return true
    })
}

type MesExamensWebProps = {
    readonly accentBleu?: string
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
    if (parsed.success) return parsed.data
    return {
        id: m.id,
        titre: m.titre,
        description: m.description ?? null,
        duree: m.duree ?? null,
        statut: m.statut,
        dateDebut: m.dateDebut,
        dateFin: m.dateDebut,
        dateCreation: undefined,
        professeur: m.professeurNom ? { nom: m.professeurNom } : null,
    }
}

export default function MesExamensWeb({ accentBleu = '#4f8ef7' }: MesExamensWebProps) {
    const navigate = useNavigate()
    const role = useAuth((s) => s.role)
    const isProfesseurWeb = (role ?? '').trim().toUpperCase() === 'PROFESSEUR'
    const [items, setItems] = useState<ExamenMeta[]>([])
    const [loading, setLoading] = useState(true)
    const [err, setErr] = useState<string | null>(null)
    const [twoCol, setTwoCol] = useState(() =>
        typeof globalThis.window !== 'undefined'
            ? globalThis.window.matchMedia('(min-width: 700px)').matches
            : true,
    )
    const [lancementExamenId, setLancementExamenId] = useState<number | null>(null)
    const [suppressionExamenId, setSuppressionExamenId] = useState<number | null>(null)
    const [actionErr, setActionErr] = useState<string | null>(null)
    const [notesVisibles, setNotesVisibles] = useState<Record<number, { note: number; bareme: number }>>({})
    const [filtreExamen, setFiltreExamen] = useState<ExamenFiltre>('recents')
    const userId = useAuth((s) => s.userId)

    const examensAffiches = useMemo(
        () => filtrerExamensParCategorie(items, filtreExamen),
        [items, filtreExamen],
    )

    const totalTries = useMemo(() => trierExamensParDateRecente(items).length, [items])

    const filtreBtn = (actif: boolean): CSSProperties => ({
        fontFamily: sans,
        fontSize: 13,
        fontWeight: actif ? 600 : 500,
        padding: '8px 14px',
        borderRadius: 999,
        border: actif ? `1px solid ${accentBleu}` : '1px solid #e2e8f4',
        background: actif ? '#eff6ff' : '#fff',
        color: actif ? '#1e40af' : '#64748b',
        cursor: 'pointer',
        transition: 'background 0.15s, border-color 0.15s, color 0.15s',
    })

    useEffect(() => {
        const mq = globalThis.window.matchMedia('(min-width: 700px)')
        const apply = () => setTwoCol(mq.matches)
        apply()
        mq.addEventListener('change', apply)
        return () => mq.removeEventListener('change', apply)
    }, [])

    const chargerExamens = useCallback(async () => {
        setErr(null)
        try {
            const { data } = await api.get<ExamenMeta[]>('/api/examens-publies/mes-publications-web')
            setItems(Array.isArray(data) ? data : [])
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
            setErr(msg)
            setItems([])
        }
    }, [isProfesseurWeb])

    useEffect(() => {
        let cancelled = false
        ;(async () => {
            setLoading(true)
            await chargerExamens()
            if (!cancelled) setLoading(false)
        })()
        return () => {
            cancelled = true
        }
    }, [chargerExamens])

    useEffect(() => {
        if (isProfesseurWeb || loading) return
        const etudiantId = userId ? Number.parseInt(userId, 10) : NaN
        if (!Number.isFinite(etudiantId) || etudiantId <= 0) return
        let cancelled = false
        const termines = items.filter((m) => (m.statut ?? '').trim().toUpperCase() === 'TERMINE')
        if (termines.length === 0) return
        ;(async () => {
            const next: Record<number, { note: number; bareme: number }> = {}
            await Promise.all(
                termines.map(async (ex) => {
                    try {
                        const { data } = await examenApi.getResultatVisible(ex.id, etudiantId)
                        if (data?.visible && typeof data.noteFinale === 'number') {
                            next[ex.id] = {
                                note: data.noteFinale,
                                bareme: typeof data.bareme === 'number' ? data.bareme : 20,
                            }
                        }
                    } catch {
                        /* note pas encore publiée */
                    }
                }),
            )
            if (!cancelled) setNotesVisibles((prev) => ({ ...prev, ...next }))
        })()
        return () => {
            cancelled = true
        }
    }, [items, isProfesseurWeb, loading, userId])

    const supprimerExamen = async (m: ExamenMeta) => {
        const titre = (m.titre ?? '').trim() || 'cet examen'
        if (
            !globalThis.confirm(
                `Supprimer « ${titre} » ?\n\nL'examen disparaîtra pour vous et pour tous les étudiants.`,
            )
        ) {
            return
        }
        setSuppressionExamenId(m.id)
        setActionErr(null)
        try {
            await examenApi.supprimerExamen(m.id)
            await chargerExamens()
            toast.success('Examen supprimé.')
        } catch (e) {
            const msg = extractActionError(e, 'Impossible de supprimer cet examen.')
            setActionErr(msg)
            toast.error(msg)
        } finally {
            setSuppressionExamenId(null)
        }
    }

    const lancerEtPiloter = async (m: ExamenMeta) => {
        const statut = (m.statut ?? '').trim().toUpperCase()
        if (statut === 'TERMINE' || statut === 'ANNULE') {
            toast.error('Cet examen est terminé : il ne peut plus être relancé.')
            return
        }
        setActionErr(null)
        setLancementExamenId(m.id)
        try {
            await examenApi.lancer(m.id)
            navigate(`/supervision/examen/${m.id}?started=1`, { replace: true })
        } catch (e: unknown) {
            setActionErr(
                extractActionError(
                    e,
                    'Impossible de démarrer la session. Vérifiez le créneau ou ouvrez l’espace superviseur pour plus de détails.',
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

    const libelleFiltre =
        filtreExamen === 'recents'
            ? `Les ${Math.min(RECENTS_COUNT, totalTries)} examens les plus récents`
            : filtreExamen === 'a_faire'
              ? `${examensAffiches.length} examen${examensAffiches.length > 1 ? 's' : ''} à faire`
              : filtreExamen === 'en_cours'
                ? `${examensAffiches.length} examen${examensAffiches.length > 1 ? 's' : ''} en cours`
                : filtreExamen === 'termine'
                  ? `${examensAffiches.length} examen${examensAffiches.length > 1 ? 's' : ''} terminé${examensAffiches.length > 1 ? 's' : ''}`
                  : `${examensAffiches.length} examen${examensAffiches.length > 1 ? 's' : ''} au total`

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
            <div
                style={{
                    display: 'flex',
                    flexWrap: 'wrap',
                    gap: 8,
                    marginBottom: 10,
                    width: '100%',
                }}
                role="tablist"
                aria-label="Filtrer les examens"
            >
                {FILTRE_OPTIONS.map((opt) => (
                    <button
                        key={opt.id}
                        type="button"
                        role="tab"
                        aria-selected={filtreExamen === opt.id}
                        style={filtreBtn(filtreExamen === opt.id)}
                        onClick={() => setFiltreExamen(opt.id)}
                    >
                        {opt.label}
                    </button>
                ))}
            </div>
            <p
                style={{
                    fontFamily: sans,
                    fontSize: 13,
                    color: '#64748b',
                    margin: '0 0 16px',
                    width: '100%',
                }}
            >
                {libelleFiltre}
                {filtreExamen === 'recents' && totalTries > RECENTS_COUNT ? (
                    <>
                        {' '}
                        ·{' '}
                        <button
                            type="button"
                            onClick={() => setFiltreExamen('tous')}
                            style={{
                                fontFamily: sans,
                                fontSize: 13,
                                fontWeight: 600,
                                color: accentBleu,
                                background: 'none',
                                border: 'none',
                                padding: 0,
                                cursor: 'pointer',
                                textDecoration: 'underline',
                            }}
                        >
                            Voir les {totalTries} examens
                        </button>
                    </>
                ) : null}
            </p>
            {examensAffiches.length === 0 ? (
                <p style={{ fontFamily: sans, color: '#64748b', margin: 0, textAlign: 'center' }}>
                    Aucun examen dans cette catégorie.
                    {filtreExamen !== 'recents' ? (
                        <>
                            {' '}
                            <button
                                type="button"
                                onClick={() => setFiltreExamen('recents')}
                                style={{
                                    fontFamily: sans,
                                    fontSize: 13,
                                    fontWeight: 600,
                                    color: accentBleu,
                                    background: 'none',
                                    border: 'none',
                                    padding: 0,
                                    cursor: 'pointer',
                                    textDecoration: 'underline',
                                }}
                            >
                                Revenir aux récents
                            </button>
                        </>
                    ) : null}
                </p>
            ) : (
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
                {examensAffiches.map((m) => {
                    const examen = metaToListeItem(m)
                    const debutMs = parseDebutExamenMs(m.dateDebut)
                    const creneauAtteint = debutMs == null ? true : Date.now() >= debutMs
                    const statutDb = (m.statut ?? '').trim().toUpperCase()
                    const peutLancerListe = statutDb === 'PLANIFIE'
                    const sessionTermineeEtudiant = !isProfesseurWeb && statutDb === 'TERMINE'
                    const noteApi = notesVisibles[m.id]
                    const noteFin =
                        noteApi?.note ??
                        (typeof (m as { noteFinaleAffichee?: unknown }).noteFinaleAffichee === 'number'
                            ? (m as { noteFinaleAffichee: number }).noteFinaleAffichee
                            : null)
                    const baremeNote =
                        noteApi?.bareme ??
                        (typeof (m as { baremeNoteFinale?: unknown }).baremeNoteFinale === 'number'
                            ? (m as { baremeNoteFinale: number }).baremeNoteFinale
                            : typeof m.bareme === 'number'
                              ? m.bareme
                              : 20)
                    const noteEtudiantPubliee =
                        sessionTermineeEtudiant && noteFin != null
                            ? { valeur: noteFin, sur: baremeNote }
                            : null

                    return (
                        <ExamenListeCard
                            key={m.id}
                            examen={examen}
                            accentBleu={accentBleu}
                            layoutTwoCol={twoCol}
                            creneauAtteint={creneauAtteint}
                            sessionTermineeEtudiant={sessionTermineeEtudiant}
                            noteEtudiantPubliee={noteEtudiantPubliee}
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
                                          onSupprimer: () => supprimerExamen(m),
                                          suppressionEnCours: suppressionExamenId === m.id,
                                      }
                                    : undefined
                            }
                        />
                    )
                })}
            </ul>
            )}
        </>
    )
}
