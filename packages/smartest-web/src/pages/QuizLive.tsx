import { useCallback, useEffect, useMemo, useState } from 'react'
import { Power } from 'lucide-react'
import { useParams } from 'react-router-dom'
import axios from 'axios'
import { QRCodeSVG } from 'qrcode.react'
import { StatsBarre } from '../components/qr/StatsBarre'
import { useQuizLiveStats } from '../hooks/useQuizLiveStats'
import type { StreamEnvelope } from '../types/quizLive'
import { resolveHttpApiBase } from '../config/runtimeBackend'
import styles from '../styles/QuizLive.module.css'

const API_BASE = resolveHttpApiBase()

function InvalidTokenView() {
    return (
        <div className={styles.page}>
            <p>Lien invalide.</p>
        </div>
    )
}

function LoadErrorView({ message }: { readonly message: string }) {
    return (
        <div className={styles.page}>
            <p role="alert" style={{ color: '#b91c1c', fontWeight: 600 }}>
                {message}
            </p>
        </div>
    )
}

function IntrouvableView() {
    return (
        <div className={styles.page}>
            <h1 style={{ fontSize: '1.25rem', marginTop: 0 }}>Session introuvable ou terminée</h1>
            <p style={{ color: '#64748b' }}>Cette session QR n&apos;est plus disponible.</p>
        </div>
    )
}

function SessionClosedSummary(props: {
    readonly fermeeLocale: boolean
    readonly totalSoumissions: number
    readonly tauxReussiteGlobal: number | null
}) {
    return (
        <div className={styles.sessionEndedShell}>
            <div className={styles.sessionEndedInner}>
                <h1>
                    {props.fermeeLocale
                        ? 'Session clôturée.'
                        : 'Session terminée par le professeur.'}
                </h1>
                <div className={styles.endSummary}>
                    <p style={{ margin: '0 0 8px', color: '#64748b', fontSize: 14 }}>
                        Réponses enregistrées (total) :{' '}
                        <strong style={{ color: '#0f172a' }}>{props.totalSoumissions}</strong>
                    </p>
                    <p style={{ margin: 0, color: '#64748b', fontSize: 14 }}>
                        Score moyen (réussite sur les questions) :{' '}
                        <strong style={{ color: '#0f172a' }}>
                            {props.tauxReussiteGlobal != null ? `${props.tauxReussiteGlobal.toFixed(1)} %` : '—'}
                        </strong>
                    </p>
                </div>
            </div>
        </div>
    )
}

export default function QuizLive() {
    const { sessionToken } = useParams<{ sessionToken: string }>()
    const token = sessionToken?.trim() ?? ''

    const [copieOk, setCopieOk] = useState(false)
    const [chargement, setChargement] = useState(true)
    const [introuvable, setIntrouvable] = useState(false)
    const [erreurCharge, setErreurCharge] = useState<string | null>(null)
    const [tempsReelOk, setTempsReelOk] = useState(false)
    const [isMobileLayout, setIsMobileLayout] = useState(false)

    const lienPassageQuiz = useMemo(() => {
        if (!token || typeof globalThis.window === 'undefined') return ''
        return `${globalThis.location.origin}/quiz-live/${encodeURIComponent(token)}/participer`
    }, [token])

    const peutCharger = useMemo(() => token.length > 0, [token])

    const {
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
    } = useQuizLiveStats(token, tempsReelOk && peutCharger && !introuvable)

    const [fermeeDepuisLaPage, setFermeeDepuisLaPage] = useState(false)
    const [clotureEnCours, setClotureEnCours] = useState(false)

    useEffect(() => {
        const mq = globalThis.window.matchMedia('(max-width: 767px)')
        const sync = () => setIsMobileLayout(mq.matches)
        sync()
        mq.addEventListener('change', sync)
        return () => mq.removeEventListener('change', sync)
    }, [])

    const qrSize = isMobileLayout ? 280 : 340

    const fetchSnapshot = useCallback(async () => {
        if (!token) return
        setChargement(true)
        setIntrouvable(false)
        setErreurCharge(null)
        try {
            const { data } = await axios.get<StreamEnvelope>(
                `${API_BASE}/api/qr-live/public/${encodeURIComponent(token)}/snapshot`,
            )
            seedFromSnapshot(data)
            setTempsReelOk(true)
        } catch (e: unknown) {
            setTempsReelOk(false)
            if (axios.isAxiosError(e) && e.response?.status === 404) {
                setIntrouvable(true)
            } else if (axios.isAxiosError(e) && !e.response) {
                setErreurCharge(
                    'Impossible de joindre le serveur. Vérifiez que le backend est démarré.',
                )
            } else {
                setErreurCharge('Erreur lors du chargement.')
            }
        } finally {
            setChargement(false)
        }
    }, [token, seedFromSnapshot])

    useEffect(() => {
        if (peutCharger) {
            Promise.resolve(fetchSnapshot()).catch(() => {})
        }
    }, [peutCharger, fetchSnapshot])

    const copierLien = async () => {
        const u = lienPassageQuiz
        if (!u) return
        try {
            await navigator.clipboard.writeText(u)
            setCopieOk(true)
            globalThis.setTimeout(() => setCopieOk(false), 2000)
        } catch {
            /* ignore */
        }
    }

    const handleFermerSession = async () => {
        if (
            globalThis.confirm('Clôturer la session ? Les participants ne pourront plus répondre.')
        ) {
            setClotureEnCours(true)
            try {
                await closeSession()
                setFermeeDepuisLaPage(true)
            } catch {
                globalThis.alert('Impossible de clôturer la session. Vérifiez la connexion au serveur.')
            } finally {
                setClotureEnCours(false)
            }
        }
    }

    const titreAffiche = quizTitre?.trim() || 'Quiz en direct'

    const sidebarStyle = useMemo(
        () =>
            isMobileLayout
                ? ({ width: '100%', minWidth: 0 } as const)
                : ({ minWidth: 420, width: 420, flexShrink: 0 } as const),
        [isMobileLayout],
    )

    if (!peutCharger) {
        return <InvalidTokenView />
    }

    if (erreurCharge && !introuvable) {
        return <LoadErrorView message={erreurCharge} />
    }

    if (introuvable) {
        return <IntrouvableView />
    }

    if (sessionClosed) {
        return (
            <SessionClosedSummary
                fermeeLocale={fermeeDepuisLaPage}
                totalSoumissions={totalSoumissionsQuestions}
                tauxReussiteGlobal={tauxReussiteGlobal}
            />
        )
    }

    const closeBtn = (
        <button
            type="button"
            className={styles.closeSessionBtn}
            title="Clôturer la session"
            aria-label="Clôturer la session"
            disabled={clotureEnCours}
            onClick={() => {
                Promise.resolve(handleFermerSession()).catch(() => {})
            }}
        >
            <Power
                size={isMobileLayout ? 20 : 22}
                strokeWidth={2}
                aria-hidden
                className={styles.closeSessionIcon}
            />
        </button>
    )

    return (
        <div className={styles.page}>
            <aside className={styles.sidebar} style={sidebarStyle}>
                <div className={styles.sidebarInner} style={{ width: '100%', maxWidth: '100%' }}>
                    <div
                        style={{
                            display: 'flex',
                            justifyContent: 'center',
                            alignItems: 'center',
                            width: '100%',
                            boxSizing: 'border-box',
                            padding: '1rem 1.125rem',
                            borderRadius: 12,
                            border: '1px solid #e2e8f0',
                            background: '#fff',
                            boxShadow: '0 4px 18px rgba(15,23,42,0.06)',
                        }}
                    >
                        {lienPassageQuiz ? (
                            <QRCodeSVG
                                value={lienPassageQuiz}
                                size={qrSize}
                                level="M"
                                title="Lien vers le passage du quiz"
                            />
                        ) : null}
                    </div>
                    <input
                        type="text"
                        readOnly
                        className={styles.urlField}
                        value={lienPassageQuiz}
                        aria-label="URL de passage du quiz"
                    />
                    <button
                        type="button"
                        className={styles.copyBtn}
                        onClick={() => {
                            Promise.resolve(copierLien()).catch(() => {})
                        }}
                    >
                        {copieOk ? 'Lien copié !' : 'Copier le lien'}
                    </button>
                </div>
            </aside>

            <div className={styles.main}>
                <header className={styles.titleRow}>
                    {chargement ? (
                        <div className={styles.titleSkeleton} style={{ flex: 1, maxWidth: '100%' }} />
                    ) : (
                        <h1 className={styles.pageTitle}>{titreAffiche}</h1>
                    )}
                    {closeBtn}
                </header>

                {chargement ? (
                    <p style={{ color: '#64748b', marginTop: 16 }}>Chargement…</p>
                ) : (
                    <>
                        <div className={styles.badgeParticipants}>
                            <span className={styles.liveDot} aria-hidden />
                            <span>
                                <strong>{participantsConnectes}</strong> participants connectés
                                {wsConnected ? null : ' (connexion temps réel…)'}
                            </span>
                        </div>

                        {wsError ? (
                            <p style={{ color: '#b45309', fontSize: 14 }}>{wsError}</p>
                        ) : null}

                        <div id="quiz-live-stats">
                        
                            {stats.length > 0 ? (
                                stats.map((s) => (
                                    <StatsBarre
                                        key={s.questionId}
                                        stat={s}
                                        nombreParticipants={participantsConnectes}
                                    />
                                ))
                            ) : (
                                <p style={{ color: '#64748b', fontSize: 14 }}>
                                    Les statistiques s&apos;affichent lorsque le quiz est lancé et que des
                                    réponses arrivent.
                                </p>
                            )}
                        </div>
                    </>
                )}
            </div>
        </div>
    )
}
