import { useEffect, useMemo, useState } from 'react'
import type { CSSProperties, ReactElement, ReactNode } from 'react'
import { useParams, useSearchParams } from 'react-router-dom'
import axios from 'axios'
import { Client } from '@stomp/stompjs'
import { ClipboardList, Copy, Percent, Power, QrCode, Users } from 'lucide-react'

type StatQuestion = {
    questionId: number
    numeroQuestion: number
    enonce: string
    nombreReponses: number
    pourcentageReussite: number
    pourcentageEchec: number
}

type StatsQuiz = {
    quizId: number
    quizTitre: string
    nombreParticipants: number
    totalSoumissionsQuestions?: number
    tauxReussiteGlobal: number
    moyenneNoteSur20?: number | null
    statistiquesParQuestion: StatQuestion[]
}

type QuizPassageInfo = {
    titre: string
}

/** Indique si la session QR a déjà des statistiques exploitables (affichage du panneau de droite). */
function hasQrLiveStatsData(stats: StatsQuiz | null): boolean {
    if (stats == null) return false
    if ((stats.nombreParticipants ?? 0) > 0) return true
    if ((stats.totalSoumissionsQuestions ?? 0) > 0) return true
    return (stats.statistiquesParQuestion ?? []).some((q) => (q.nombreReponses ?? 0) > 0)
}

const sans = "'DM Sans', system-ui, sans-serif"
const serif = "'DM Serif Display', Georgia, serif"
const API_BASE_URL = 'http://localhost:8081'
const WS_BASE_URL = 'ws://localhost:8081/ws'

const QR_PIXEL_HERO = 420
const QR_PIXEL_HERO_NARROW = 300
const QR_PIXEL_COMPACT = 300
const QR_PIXEL_COMPACT_NARROW = 260

const colors = {
    pageBg: 'linear-gradient(165deg, #eef2f9 0%, #f8fafc 45%, #f0f4fc 100%)',
    card: '#ffffff',
    text: '#0f1e3d',
    muted: '#64748b',
    border: 'rgba(15, 30, 61, 0.07)',
    accent: '#2563eb',
    accentSoft: 'rgba(37, 99, 235, 0.1)',
    danger: '#b91c1c',
    dangerSoft: '#fff1f2',
    dangerBorder: '#fecaca',
    barTrack: '#e2e8f0',
}

/**
 * Couleur unie de toute la barre remplie : du rouge (0 %) au vert (100 %).
 * Teinte HSL 0° → 120° quand le pourcentage de réussite augmente.
 */
function couleurBarreRougeVertSelonPourcentage(pourcent: number): string {
    const t = Math.max(0, Math.min(100, pourcent)) / 100
    const hue = 120 * t
    return `hsl(${hue}, 78%, 42%)`
}

function cardShadow(elevated: boolean): string {
    return elevated
        ? '0 18px 40px rgba(15, 30, 61, 0.08), 0 2px 8px rgba(15, 30, 61, 0.04)'
        : '0 4px 24px rgba(15, 30, 61, 0.06), 0 1px 3px rgba(15, 30, 61, 0.04)'
}

function KpiCard(props: {
    icon: ReactNode
    label: string
    value?: string | number
    /** Couleur du chiffre affiché (ex. taux global en rouge → vert). */
    valueColor?: string
    /** Si défini, affiche une jauge : couleur unie rouge → vert selon le taux. */
    colorBarPercent?: number
    hint?: string
    /** Une carte par ligne sur mobile. */
    stackOnNarrow?: boolean
}): ReactElement {
    const { icon, label, value, valueColor, colorBarPercent, hint, stackOnNarrow } = props
    const p = colorBarPercent != null ? Math.max(0, Math.min(100, colorBarPercent)) : null

    return (
        <div
            style={{
                flex: stackOnNarrow ? '0 0 auto' : '1 1 120px',
                minWidth: stackOnNarrow ? 0 : 120,
                width: stackOnNarrow ? '100%' : undefined,
                borderRadius: 14,
                padding: stackOnNarrow ? '12px 14px' : '14px 16px',
                background: 'linear-gradient(145deg, #fafbfd 0%, #f4f6fb 100%)',
                border: `1px solid ${colors.border}`,
                boxSizing: 'border-box',
            }}
        >
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 8 }}>
                <div
                    style={{
                        width: 36,
                        height: 36,
                        borderRadius: 10,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        background: colors.accentSoft,
                        color: colors.accent,
                    }}
                >
                    {icon}
                </div>
                <span style={{ color: colors.muted, fontSize: 12, fontWeight: 600, letterSpacing: '0.02em' }}>
                    {label}
                </span>
            </div>
            {p != null ? (
                <div
                    style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: 10,
                        marginTop: 2,
                    }}
                >
                    <div
                        title={`${Math.round(p)} % de réussite`}
                        style={{
                            flex: 1,
                            minWidth: 0,
                            background: colors.barTrack,
                            borderRadius: 999,
                            height: 12,
                            overflow: 'hidden',
                        }}
                    >
                        <div
                            style={{
                                width: `${p}%`,
                                height: '100%',
                                background: couleurBarreRougeVertSelonPourcentage(p),
                                borderRadius: 999,
                                transition: 'width 0.35s ease, background-color 0.35s ease',
                            }}
                        />
                    </div>
                    <span
                        style={{
                            flexShrink: 0,
                            fontSize: 14,
                            fontWeight: 700,
                            color: couleurBarreRougeVertSelonPourcentage(p),
                            minWidth: 44,
                            textAlign: 'right',
                            fontVariantNumeric: 'tabular-nums',
                        }}
                    >
                        {Math.round(p)}%
                    </span>
                </div>
            ) : (
                <div
                    style={{
                        fontSize: stackOnNarrow ? 'clamp(1.15rem, 4.5vw, 1.375rem)' : 22,
                        fontWeight: 700,
                        color: valueColor ?? colors.text,
                        lineHeight: 1.15,
                        fontVariantNumeric: 'tabular-nums',
                    }}
                >
                    {value}
                </div>
            )}
            {hint ? <div style={{ marginTop: 4, fontSize: 11, color: colors.muted }}>{hint}</div> : null}
        </div>
    )
}

function QuestionStatRow(props: { q: StatQuestion; compact?: boolean }): ReactElement {
    const { q, compact } = props
    const reussite = Math.max(0, Math.min(100, q.pourcentageReussite ?? 0))
    const enonceCourt = (q.enonce ?? '').replace(/\s+/g, ' ').trim()

    return (
        <div
            style={{
                borderRadius: compact ? 10 : 12,
                padding: compact ? '10px 12px' : '12px 14px',
                border: `1px solid ${colors.border}`,
                background: '#fcfdff',
            }}
        >
            <div style={{ marginBottom: 10 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
                    <span
                        style={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            minWidth: 28,
                            height: 24,
                            padding: '0 8px',
                            borderRadius: 8,
                            fontSize: 12,
                            fontWeight: 700,
                            color: colors.accent,
                            background: colors.accentSoft,
                        }}
                    >
                        Q{q.numeroQuestion}
                    </span>
                    <span style={{ fontSize: 11, color: colors.muted }}>
                        {q.nombreReponses ?? 0} réponse{(q.nombreReponses ?? 0) > 1 ? 's' : ''}
                    </span>
                </div>
                {enonceCourt ? (
                    <p
                        title={enonceCourt}
                        style={{
                            margin: 0,
                            fontSize: 13,
                            color: colors.text,
                            lineHeight: 1.45,
                            display: '-webkit-box',
                            WebkitLineClamp: 2,
                            WebkitBoxOrient: 'vertical',
                            overflow: 'hidden',
                        }}
                    >
                        {enonceCourt}
                    </p>
                ) : null}
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <div
                    title={`${Math.round(reussite)} % de réussite`}
                    style={{
                        flex: 1,
                        minWidth: 0,
                        background: colors.barTrack,
                        borderRadius: 999,
                        height: 10,
                        overflow: 'hidden',
                    }}
                >
                    <div
                        style={{
                            width: `${reussite}%`,
                            height: '100%',
                            background: couleurBarreRougeVertSelonPourcentage(reussite),
                            borderRadius: 999,
                            transition: 'width 0.35s ease, background-color 0.35s ease',
                        }}
                    />
                </div>
                <span
                    style={{
                        flexShrink: 0,
                        fontSize: 13,
                        fontWeight: 700,
                        color: couleurBarreRougeVertSelonPourcentage(reussite),
                        minWidth: 40,
                        textAlign: 'right',
                        fontVariantNumeric: 'tabular-nums',
                    }}
                >
                    {Math.round(reussite)}%
                </span>
            </div>
        </div>
    )
}

const LIVE_PILL_STYLE: CSSProperties = {
    display: 'inline-flex',
    alignItems: 'center',
    gap: 6,
    padding: '4px 10px',
    borderRadius: 999,
    fontSize: 11,
    fontWeight: 600,
    letterSpacing: '0.04em',
    textTransform: 'uppercase',
    color: '#15803d',
    background: 'rgba(34, 197, 94, 0.12)',
    border: '1px solid rgba(34, 197, 94, 0.25)',
}

function invitePanelPadding(isHero: boolean, isNarrow: boolean | undefined): string {
    if (isNarrow) {
        return isHero ? '22px 14px' : '18px 14px'
    }
    return isHero ? '28px 24px' : '22px 20px'
}

function invitePanelHeroMaxWidth(isHero: boolean, isNarrow: boolean | undefined): number | string | undefined {
    if (!isHero) {
        return undefined
    }
    return isNarrow ? 'min(100%, 440px)' : 480
}

function QuizQrPublicLinkBlock(props: {
    isNarrow?: boolean
    studentUrl: string
    copyOk: boolean
    onCopierLien: () => void
}): ReactElement {
    const { isNarrow, studentUrl, copyOk, onCopierLien } = props
    const narrow = Boolean(isNarrow)

    return (
        <>
          
            <div
                style={{
                    display: 'flex',
                    flexDirection: narrow ? 'column' : 'row',
                    gap: narrow ? 10 : 8,
                    alignItems: 'stretch',
                    flexWrap: 'wrap',
                }}
            >
                <a
                    href={studentUrl}
                    target="_blank"
                    rel="noreferrer"
                    style={{
                        flex: narrow ? 'none' : '1 1 180px',
                        minWidth: 0,
                        color: colors.accent,
                        fontSize: narrow ? 12 : 13,
                        wordBreak: 'break-all',
                        lineHeight: 1.45,
                        padding: '10px 12px',
                        borderRadius: 10,
                        background: '#f8fafc',
                        border: `1px solid ${colors.border}`,
                        textDecoration: 'none',
                    }}
                >
                    {studentUrl}
                </a>
                <button
                    type="button"
                    onClick={onCopierLien}
                    disabled={!studentUrl}
                    style={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        gap: 8,
                        padding: narrow ? '12px 14px' : '0 14px',
                        minHeight: narrow ? 44 : undefined,
                        width: narrow ? '100%' : 'auto',
                        borderRadius: 10,
                        border: `1px solid ${copyOk ? '#86efac' : colors.border}`,
                        background: copyOk ? '#f0fdf4' : '#fff',
                        color: copyOk ? '#166534' : colors.text,
                        fontSize: 13,
                        fontWeight: 600,
                        cursor: studentUrl ? 'pointer' : 'not-allowed',
                        opacity: studentUrl ? 1 : 0.5,
                        fontFamily: sans,
                    }}
                >
                    <Copy size={16} aria-hidden />
                    {copyOk ? 'Copié' : 'Copier'}
                </button>
            </div>
        </>
    )
}

function QuizQrInvitePanel(props: {
    qrUrl: string
    studentUrl: string
    copyOk: boolean
    onCopierLien: () => void
    qrPixelSize: number
    /** hero = écran d’accueil (QR grand, centré) ; compact = colonne à côté des stats */
    variant: 'hero' | 'compact'
    /** Affichage étroit (mobile / tablette). */
    isNarrow?: boolean
    /** Bouton clôturer affiché tant que le panneau stats n’est pas visible. */
    showCloturer?: boolean
    onCloturer?: () => void
}): ReactElement {
    const { qrUrl, studentUrl, copyOk, onCopierLien, qrPixelSize, variant, isNarrow, showCloturer, onCloturer } = props
    const isHero = variant === 'hero'

    return (
        <section
            style={{
                background: colors.card,
                borderRadius: isNarrow ? 14 : 18,
                padding: invitePanelPadding(isHero, isNarrow),
                border: `1px solid ${colors.border}`,
                boxShadow: cardShadow(true),
                width: '100%',
                maxWidth: invitePanelHeroMaxWidth(isHero, isNarrow),
                margin: isHero ? '0 auto' : undefined,
                boxSizing: 'border-box',
            }}
        >
            <div
                style={{
                    display: 'flex',
                    alignItems: 'flex-start',
                    justifyContent: 'space-between',
                    gap: 12,
                    marginBottom: 18,
                }}
            >
                <div style={{ display: 'flex', alignItems: 'center', gap: 10, minWidth: 0 }}>
                    <div
                        style={{
                            width: 40,
                            height: 40,
                            borderRadius: 12,
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            background: colors.accentSoft,
                            color: colors.accent,
                            flexShrink: 0,
                            
                        }}
                    >
                        <QrCode size={22} strokeWidth={2} aria-hidden />
                    </div>
                    <div style={{ minWidth: 0 }}>
                        <h2
                            style={{
                                margin: 0,
                                color: colors.text,
                                fontSize: isNarrow ? 'clamp(1rem, 4vw, 1.125rem)' : 18,
                                fontWeight: 700,
                                lineHeight: 1.25,
                            }}
                        >
                            Inviter les participants
                        </h2>
                        <p
                            style={{
                                margin: '4px 0 0',
                                color: colors.muted,
                                fontSize: isNarrow ? 12 : 13,
                                lineHeight: 1.4,
                            }}
                        >
                            Scannez le code ou ouvrez le lien.
                        </p>
                    </div>
                </div>
               
            </div>

            {qrUrl ? (
                <div
                    style={{
                        display: 'flex',
                        justifyContent: 'center',
                        padding: isHero ? '20px 16px 24px' : '16px 12px 20px',
                        borderRadius: 15,
                        marginBottom: 10,
                        background: 'linear-gradient(180deg, #f8fafc 0%, #ffffff 100%)',
                        border: `1px dashed ${colors.border}`,
                    }}
                >
                    <img
                        src={qrUrl}
                        alt="Code QR du quiz"
                        width={qrPixelSize}
                        height={qrPixelSize}
                        style={{
                            borderRadius: 12,
                            boxShadow: '0 8px 28px rgba(15, 30, 61, 0.12)',
                            display: 'block',
                            width: `min(100%, ${qrPixelSize}px)`,
                            height: 'auto',
                        }}
                    />
                </div>
            ) : null}

            <QuizQrPublicLinkBlock
                isNarrow={isNarrow}
                studentUrl={studentUrl}
                copyOk={copyOk}
                onCopierLien={onCopierLien}
            />
        </section>
    )
}

function QuizQrLiveStatsPanel(props: {
    titreAffiche: string
    quizIdNum: number
    lastRefresh: Date | null
    error: string | null
    stats: StatsQuiz
    onCloturer: () => void
    isNarrow: boolean
}): ReactElement {
    const { titreAffiche, quizIdNum, lastRefresh, error, stats, onCloturer, isNarrow } = props

    return (
        <section
            style={{
                background: colors.card,
                borderRadius: isNarrow ? 14 : 18,
                padding: isNarrow ? '16px 14px 18px' : '22px 22px 24px',
                border: `1px solid ${colors.border}`,
                boxShadow: cardShadow(false),
                minWidth: 0,
            }}
        >
            <div
                style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'flex-start',
                    gap: 14,
                    marginBottom: 6,
                }}
            >
                <div style={{ minWidth: 0, flex: 1 }}>
                    <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 10, marginBottom: 8 }}>
                        <span style={LIVE_PILL_STYLE}>
                            <span
                                style={{
                                    width: 6,
                                    height: 6,
                                    borderRadius: '50%',
                                    background: '#22c55e',
                                    animation: 'smartest-live-pulse 1.6s ease-in-out infinite',
                                }}
                            />
                            En direct
                        </span>
                    </div>
                    <h2
                        style={{
                            margin: 0,
                            color: colors.text,
                            fontSize: 'clamp(1.15rem, 2vw, 1.45rem)',
                            fontWeight: 700,
                            lineHeight: 1.3,
                            wordBreak: 'break-word',
                        }}
                    >
                        {titreAffiche || `Quiz · #${quizIdNum}`}
                    </h2>
                </div>
                <button
                    type="button"
                    onClick={onCloturer}
                    title="Clôturer la session QR"
                    aria-label="Clôturer la session QR"
                    style={{
                        flexShrink: 0,
                        width: 40,
                        height: 40,
                        borderRadius: 12,
                        border: `1px solid ${colors.dangerBorder}`,
                        background: colors.dangerSoft,
                        color: colors.danger,
                        cursor: 'pointer',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        transition: 'transform 0.15s, box-shadow 0.15s',
                        boxShadow: '0 1px 2px rgba(185, 28, 28, 0.08)',
                    }}
                >
                    <Power size={20} strokeWidth={2} aria-hidden />
                </button>
            </div>
            {lastRefresh ? (
                <p style={{ margin: '0 0 18px', color: colors.muted, fontSize: 12 }}>
                    Dernière mise à jour · {lastRefresh.toLocaleTimeString()}
                </p>
            ) : (
                <div style={{ height: 8 }} />
            )}

            {error ? (
                <p
                    style={{
                        margin: '8px 0 0',
                        color: colors.danger,
                        fontSize: 14,
                        padding: '12px 14px',
                        borderRadius: 12,
                        background: colors.dangerSoft,
                        border: `1px solid ${colors.dangerBorder}`,
                    }}
                >
                    {error}
                </p>
            ) : null}

            <div
                style={{
                    display: 'flex',
                    flexDirection: isNarrow ? 'column' : 'row',
                    gap: isNarrow ? 10 : 12,
                    marginBottom: 22,
                    marginTop: error ? 16 : 0,
                    flexWrap: 'wrap',
                }}
            >
                <KpiCard
                    stackOnNarrow={isNarrow}
                    icon={<Users size={18} strokeWidth={2} aria-hidden />}
                    label="Participants"
                    value={stats.nombreParticipants ?? 0}
                />
                <KpiCard
                    stackOnNarrow={isNarrow}
                    icon={<Percent size={18} strokeWidth={2} aria-hidden />}
                    label="Taux de réussite"
                    value={`${Math.round(stats.tauxReussiteGlobal ?? 0)} %`}
                    valueColor={couleurBarreRougeVertSelonPourcentage(stats.tauxReussiteGlobal ?? 0)}
                    hint="Sur l’ensemble du quiz"
                />
                <KpiCard
                    stackOnNarrow={isNarrow}
                    icon={<ClipboardList size={18} strokeWidth={2} aria-hidden />}
                    label="Réponses enregistrées"
                    value={stats.totalSoumissionsQuestions ?? 0}
                    hint="Par question (cumul)"
                />
            </div>
            <div
                style={{
                    display: 'flex',
                    alignItems: isNarrow ? 'flex-start' : 'baseline',
                    flexDirection: isNarrow ? 'column' : 'row',
                    justifyContent: 'space-between',
                    gap: isNarrow ? 6 : 12,
                    marginBottom: 14,
                    flexWrap: 'wrap',
                }}
            >
                <h3
                    style={{
                        margin: 0,
                        color: colors.text,
                        fontSize: isNarrow ? 'clamp(0.95rem, 3.8vw, 1rem)' : 16,
                        fontWeight: 700,
                    }}
                >
                    Taux de réussite par question
                </h3>
                <span style={{ fontSize: 12, color: colors.muted }}>
                    {(stats.statistiquesParQuestion ?? []).length} question
                    {(stats.statistiquesParQuestion ?? []).length > 1 ? 's' : ''}
                </span>
            </div>
            <div style={{ display: 'grid', gap: isNarrow ? 8 : 10, paddingRight: isNarrow ? 0 : 4 }}>
                {(stats.statistiquesParQuestion ?? []).map((q) => (
                    <QuestionStatRow key={q.questionId} q={q} compact={isNarrow} />
                ))}
            </div>
        </section>
    )
}

function QuizQrLivePageHeader(): ReactElement {
    return (
        <div
            style={{
                maxWidth: 1120,
                margin: '0 auto clamp(16px, 4vw, 24px)',
                width: '100%',
                boxSizing: 'border-box',
            }}
        >
            <p
                style={{
                    margin: '0 0 6px',
                    fontSize: 12,
                    fontWeight: 600,
                    letterSpacing: '0.12em',
                    textTransform: 'uppercase',
                    color: colors.muted,
                }}
            >
                SmarTest · Session QR
            </p>
            <h1
                style={{
                    margin: 0,
                    fontFamily: serif,
                    fontSize: 'clamp(1.55rem, 2.8vw, 2rem)',
                    fontWeight: 550,
                    color: colors.text,
                    lineHeight: 1.2,
                }}
            >
                Suivi en direct
            </h1>
        </div>
    )
}

function QuizQrLiveErrorBanner(props: { message: string }): ReactElement {
    return (
        <div
            role="alert"
            style={{
                borderRadius: 14,
                padding: '14px 16px',
                color: colors.danger,
                fontSize: 14,
                background: colors.dangerSoft,
                border: `1px solid ${colors.dangerBorder}`,
            }}
        >
            {props.message}
        </div>
    )
}

function QuizQrHeroOnlySection(props: {
    error: string | null
    qrUrl: string
    qrPixelSize: number
    studentUrl: string
    copyOk: boolean
    onCopierLien: () => void
    peutCloturerDepuisQrSeul: boolean
    onCloturer: () => void
    isNarrow: boolean
}): ReactElement {
    const {
        error,
        qrUrl,
        qrPixelSize,
        studentUrl,
        copyOk,
        onCopierLien,
        peutCloturerDepuisQrSeul,
        onCloturer,
        isNarrow,
    } = props

    return (
        <div
            style={{
                minHeight: isNarrow ? 'auto' : 'min(calc(100vh - clamp(168px, 26vw, 220px)), 720px)',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'center',
                alignItems: 'center',
                alignSelf: 'center',
                gap: isNarrow ? 16 : 20,
                width: '100%',
                boxSizing: 'border-box',
                paddingTop: isNarrow ? 8 : 0,
            }}
        >
            {error ? (
                <div style={{ width: '100%', maxWidth: 520 }}>
                    <QuizQrLiveErrorBanner message={error} />
                </div>
            ) : null}
            <QuizQrInvitePanel
                variant="hero"
                isNarrow={isNarrow}
                qrUrl={qrUrl}
                qrPixelSize={qrPixelSize}
                studentUrl={studentUrl}
                copyOk={copyOk}
                onCopierLien={onCopierLien}
                showCloturer={peutCloturerDepuisQrSeul}
                onCloturer={onCloturer}
            />
        </div>
    )
}

function QuizQrStatsTwoColumnSection(props: {
    isNarrow: boolean
    grilleColonnes: string
    grilleGap: number
    error: string | null
    stats: StatsQuiz
    titreAffiche: string
    quizIdNum: number
    lastRefresh: Date | null
    qrUrl: string
    qrPixelSize: number
    studentUrl: string
    copyOk: boolean
    onCopierLien: () => void
    onCloturer: () => void
}): ReactElement {
    const {
        isNarrow,
        grilleColonnes,
        grilleGap,
        error,
        stats,
        titreAffiche,
        quizIdNum,
        lastRefresh,
        qrUrl,
        qrPixelSize,
        studentUrl,
        copyOk,
        onCopierLien,
        onCloturer,
    } = props

    const desktopStatsLayout = !isNarrow

    return (
        <>
            {error && isNarrow ? <QuizQrLiveErrorBanner message={error} /> : null}
            <div
                style={{
                    display: 'grid',
                    gridTemplateColumns: grilleColonnes,
                    gap: grilleGap,
                    alignItems: 'start',
                }}
            >
                <div
                    style={{
                        position: desktopStatsLayout ? 'sticky' : 'relative',
                        top: desktopStatsLayout ? 24 : undefined,
                        alignSelf: 'start',
                        zIndex: 2,
                        maxWidth: '100%',
                        height: desktopStatsLayout ? 'fit-content' : undefined,
                        maxHeight: desktopStatsLayout ? 'calc(100vh - 48px)' : undefined,
                        overflowY: desktopStatsLayout ? 'auto' : undefined,
                    }}
                >
                    <QuizQrInvitePanel
                        variant="compact"
                        isNarrow={isNarrow}
                        qrUrl={qrUrl}
                        qrPixelSize={qrPixelSize}
                        studentUrl={studentUrl}
                        copyOk={copyOk}
                        onCopierLien={onCopierLien}
                        showCloturer={false}
                    />
                </div>
                <div style={{ minWidth: 0, paddingRight: desktopStatsLayout ? 6 : 0 }}>
                    {desktopStatsLayout && error ? (
                        <div style={{ marginBottom: 14 }}>
                            <QuizQrLiveErrorBanner message={error} />
                        </div>
                    ) : null}
                    <QuizQrLiveStatsPanel
                        titreAffiche={titreAffiche}
                        quizIdNum={quizIdNum}
                        lastRefresh={lastRefresh}
                        error={isNarrow ? error : null}
                        stats={stats}
                        onCloturer={onCloturer}
                        isNarrow={isNarrow}
                    />
                </div>
            </div>
        </>
    )
}

export default function QuizQrLivePage() {
    const [isNarrow, setIsNarrow] = useState(() =>
        typeof window !== 'undefined' ? window.matchMedia('(max-width: 900px)').matches : false,
    )
    const { quizId } = useParams()
    const [searchParams] = useSearchParams()
    const [stats, setStats] = useState<StatsQuiz | null>(null)
    const [quizTitle, setQuizTitle] = useState('')
    const [error, setError] = useState<string | null>(null)
    const [lastRefresh, setLastRefresh] = useState<Date | null>(null)
    const [copyOk, setCopyOk] = useState(false)
    const [wsNotice, setWsNotice] = useState<string | null>(null)

    const quizIdNum = Number(quizId)
    const professeurToken = searchParams.get('token') ?? localStorage.getItem('token') ?? ''
    const studentUrl = useMemo(() => {
        if (!quizIdNum) return ''
        return `${window.location.origin}/quiz-qr/${quizIdNum}`
    }, [quizIdNum])

    const showLiveStats = hasQrLiveStatsData(stats)
    const qrPixelSize = useMemo(() => {
        if (!showLiveStats) {
            return isNarrow ? QR_PIXEL_HERO_NARROW : QR_PIXEL_HERO
        }
        return isNarrow ? QR_PIXEL_COMPACT_NARROW : QR_PIXEL_COMPACT
    }, [showLiveStats, isNarrow])
    const qrUrl = useMemo(() => {
        if (!studentUrl) return ''
        return `https://api.qrserver.com/v1/create-qr-code/?size=${qrPixelSize}x${qrPixelSize}&data=${encodeURIComponent(studentUrl)}`
    }, [studentUrl, qrPixelSize])

    const titreAffiche = quizTitle || stats?.quizTitre || ''

    useEffect(() => {
        if (!quizIdNum || Number.isNaN(quizIdNum)) {
            setError('Identifiant de quiz invalide.')
            return
        }
        if (!professeurToken) {
            setError("Token manquant. Ouvrez cette page depuis l'application desktop (bouton Code QR).")
            return
        }

        let cancelled = false
        let timer: number | undefined

        const fetchStats = async () => {
            try {
                const { data } = await axios.get<StatsQuiz>(`${API_BASE_URL}/api/quizs/${quizIdNum}/stats-qr-live`, {
                    headers: { Authorization: `Bearer ${professeurToken}` },
                })
                if (cancelled) return
                setStats(data)
                setError(null)
                setLastRefresh(new Date())
            } catch {
                if (cancelled) return
                setError("Impossible de charger les statistiques temps réel du quiz.")
            }
        }

        const tick = async () => {
            await fetchStats()
            if (!cancelled) timer = window.setTimeout(tick, 5000)
        }

        void tick()
        return () => {
            cancelled = true
            if (timer) window.clearTimeout(timer)
        }
    }, [quizIdNum, professeurToken])

    useEffect(() => {
        if (!quizIdNum || Number.isNaN(quizIdNum)) return

        let cancelled = false
        const fetchQuizTitle = async () => {
            try {
                const { data } = await axios.get<QuizPassageInfo>(`${API_BASE_URL}/api/quizs/${quizIdNum}/passage-qr`)
                if (cancelled) return
                setQuizTitle((data.titre ?? '').trim())
            } catch {
                // ignore; title can still come from live stats payload
            }
        }

        void fetchQuizTitle()
        return () => {
            cancelled = true
        }
    }, [quizIdNum])

    useEffect(() => {
        const mq = window.matchMedia('(max-width: 900px)')
        const apply = () => setIsNarrow(mq.matches)
        apply()
        mq.addEventListener('change', apply)
        return () => mq.removeEventListener('change', apply)
    }, [])

    useEffect(() => {
        if (!quizIdNum || Number.isNaN(quizIdNum)) return

        let cancelled = false

        const client = new Client({
            brokerURL: WS_BASE_URL,
            reconnectDelay: 3000,
            onConnect: () => {
                client.subscribe(`/topic/quiz/${quizIdNum}/qr-live`, (message) => {
                    try {
                        const data = JSON.parse(message.body) as StatsQuiz
                        if (cancelled) return
                        setStats(data)
                        setError(null)
                        setLastRefresh(new Date())
                        setWsNotice(null)
                    } catch (parseErr) {
                        console.warn('[QuizQrLive] Message temps réel invalide', parseErr)
                        if (!cancelled) {
                            setWsNotice('Message temps réel invalide (JSON).')
                        }
                    }
                })
            },
            onWebSocketClose: (evt) => {
                if (cancelled) return
                const detail =
                    typeof evt.code === 'number' && evt.reason
                        ? `${evt.code} — ${evt.reason}`
                        : String(evt.code ?? '')
                setWsNotice(detail ? `Connexion WebSocket fermée (${detail}).` : 'Connexion WebSocket fermée.')
            },
            onWebSocketError: () => {
                if (cancelled) return
                setWsNotice('Erreur WebSocket (temps réel).')
            },
            onStompError: (frame) => {
                if (cancelled) return
                const hint = frame.headers?.message ?? frame.body
                setWsNotice(hint ? `Erreur STOMP : ${hint}` : 'Erreur STOMP.')
            },
        })

        client.activate()
        return () => {
            cancelled = true
            client.deactivate()
        }
    }, [quizIdNum])

    const cloturerSessionQr = async () => {
        try {
            await axios.delete(`${API_BASE_URL}/api/quizs/${quizIdNum}/stats-qr-live`, {
                headers: { Authorization: `Bearer ${professeurToken}` },
            })
            setStats(null)
            setLastRefresh(new Date())
            setError(null)
        } catch {
            setError('Impossible de clôturer les statistiques QR live.')
        }
    }

    const copierLien = async () => {
        if (!studentUrl) return
        try {
            await navigator.clipboard.writeText(studentUrl)
            setCopyOk(true)
            window.setTimeout(() => setCopyOk(false), 2000)
        } catch {
            setCopyOk(false)
        }
    }

    const peutCloturerDepuisQrSeul =
        quizIdNum > 0 && !Number.isNaN(quizIdNum) && professeurToken.trim().length > 0 && !showLiveStats

    let grilleColonnes = 'minmax(280px, 380px)'
    let grilleGap = 0
    if (showLiveStats) {
        grilleColonnes = isNarrow ? '1fr' : 'minmax(260px, 360px) minmax(0, 1fr)'
        grilleGap = isNarrow ? 16 : 28
    }

    let zoneContenuPrincipal: ReactElement | null = null
    if (!showLiveStats) {
        zoneContenuPrincipal = (
            <QuizQrHeroOnlySection
                error={error}
                qrUrl={qrUrl}
                qrPixelSize={qrPixelSize}
                studentUrl={studentUrl}
                copyOk={copyOk}
                onCopierLien={copierLien}
                peutCloturerDepuisQrSeul={peutCloturerDepuisQrSeul}
                onCloturer={cloturerSessionQr}
                isNarrow={isNarrow}
            />
        )
    } else if (stats != null) {
        zoneContenuPrincipal = (
            <QuizQrStatsTwoColumnSection
                isNarrow={isNarrow}
                grilleColonnes={grilleColonnes}
                grilleGap={grilleGap}
                error={error}
                stats={stats}
                titreAffiche={titreAffiche}
                quizIdNum={quizIdNum}
                lastRefresh={lastRefresh}
                qrUrl={qrUrl}
                qrPixelSize={qrPixelSize}
                studentUrl={studentUrl}
                copyOk={copyOk}
                onCopierLien={copierLien}
                onCloturer={cloturerSessionQr}
            />
        )
    }

    return (
        <>
            <style>{`
                @keyframes smartest-live-pulse {
                    0%, 100% { opacity: 1; transform: scale(1); }
                    50% { opacity: 0.45; transform: scale(0.92); }
                }
            `}</style>
            <main
                style={{
                    minHeight: '100vh',
                    boxSizing: 'border-box',
                    display: 'flex',
                    flexDirection: 'column',
                    background: colors.pageBg,
                    padding:
                        'clamp(14px, 4vw, 32px) clamp(12px, 4vw, 28px) clamp(32px, 5vw, 48px)',
                    fontFamily: sans,
                    /** Pas de overflow-x sur main : sinon `position:sticky` du QR ne fixe plus au scroll. */
                    overflowX: 'clip',
                    width: '100%',
                }}
            >
                <div style={{ flexShrink: 0 }}>
                    <QuizQrLivePageHeader />
                </div>

                {wsNotice ? (
                    <div
                        role="alert"
                        style={{
                            maxWidth: 1120,
                            margin: '0 auto',
                            width: '100%',
                            padding: '10px 14px',
                            borderRadius: 10,
                            background: '#fff7ed',
                            border: '1px solid #fed7aa',
                            color: '#9a3412',
                            fontSize: 13,
                            lineHeight: 1.45,
                            boxSizing: 'border-box',
                        }}
                    >
                        {wsNotice}
                    </div>
                ) : null}

                <div
                    style={{
                        maxWidth: 1120,
                        margin: '0 auto',
                        width: '100%',
                        minWidth: 0,
                        display: 'flex',
                        flexDirection: 'column',
                        gap: 'clamp(14px, 3vw, 20px)',
                        boxSizing: 'border-box',
                    }}
                >
                    {zoneContenuPrincipal}
                </div>
            </main>
        </>
    )
}
