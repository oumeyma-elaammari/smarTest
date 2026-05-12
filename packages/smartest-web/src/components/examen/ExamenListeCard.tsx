import { CalendarClock, Clock, User } from 'lucide-react'
import type { CSSProperties, ReactNode } from 'react'
import type { ExamenListeItem } from '../../api/quizSchemas'
import { formatDateTimeUnknown, formatStatutExamen } from '../../utils/examenDisplay'

const sans = "'DM Sans', system-ui, sans-serif"

function statutPillStyle(statut?: string | null): { background: string; color: string; border: string } {
    const u = (statut ?? '').toUpperCase()
    if (u === 'EN_COURS') {
        return { background: '#dcfce7', color: '#166534', border: '1px solid #bbf7d0' }
    }
    if (u === 'TERMINE') {
        return { background: '#f1f5f9', color: '#475569', border: '1px solid #e2e8f0' }
    }
    return { background: '#fff7ed', color: '#b45309', border: '1px solid #fed7aa' }
}

export type ExamenListeCardProps = {
    readonly examen: ExamenListeItem
    readonly accentBleu: string
    readonly layoutTwoCol: boolean
    readonly creneauAtteint: boolean
    /** Passage étudiant : rejoindre salle / épreuve. */
    readonly onRejoindre: () => void
    /** Session terminée côté serveur : plus d'accès, message sur la note à venir. */
    readonly sessionTermineeEtudiant?: boolean
    /** Si défini : affichage superviseur avec pilotage dédié. */
    readonly superviseurProps?: {
        readonly onOuvrirPilotage: () => void
        /** Démarrer la session sur le serveur puis redirection (ex. vers /supervision/...). */
        readonly onLancerSession?: () => void | Promise<void>
        /** Désactive le bouton pendant l'appel API. */
        readonly lancementEnCours?: boolean
        /** Affiche « Démarrer » seulement tant que la session n'a pas été démarrée côté serveur (métadonnée). */
        readonly peutLancerSession: boolean
    }
}

function fireSessionLaunch(handler: (() => void | Promise<void>) | undefined): void {
    const h = handler
    if (h) Promise.resolve(h()).catch(() => {})
}

type CreneauHintProps = { readonly creneauAtteint: boolean }

function CreneauHint({ creneauAtteint }: CreneauHintProps) {
    if (creneauAtteint) return null
    return (
        <p
            title="L'examen doit être accessible lorsque l'heure prévue du créneau est atteinte."
            style={{
                margin: '8px 0 0',
                padding: 0,
                fontSize: 10,
                color: '#64748b',
                fontWeight: 400,
                lineHeight: 1.2,
                maxWidth: '100%',
                whiteSpace: 'nowrap',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
            }}
        >
            L'examen doit être accessible lorsque l'heure prévue du créneau est atteinte.
        </p>
    )
}

type SuperviseurBlockProps = {
    readonly superviseurProps: NonNullable<ExamenListeCardProps['superviseurProps']>
    readonly layoutTwoCol: boolean
    readonly creneauAtteint: boolean
    readonly accentBleu: string
}

function SuperviseurBlock({
    superviseurProps,
    layoutTwoCol,
    creneauAtteint,
    accentBleu,
}: SuperviseurBlockProps) {
    const titreLancer = creneauAtteint
        ? 'Démarre la session pour les étudiants et ouvre votre espace de pilotage'
        : 'Disponible à partir du créneau affiché'
    const lancerEnabledVisuel = creneauAtteint && !superviseurProps.lancementEnCours
    const styleLancerBtn: CSSProperties = {
        padding: '8px 18px',
        borderRadius: 8,
        border: 'none',
        background: lancerEnabledVisuel ? accentBleu : '#94a3b8',
        color: '#fff',
        fontWeight: 600,
        fontFamily: sans,
        fontSize: 13,
        cursor: lancerEnabledVisuel ? 'pointer' : 'not-allowed',
        width: '100%',
        transition: 'background 0.15s, opacity 0.15s',
    }

    return (
        <div
            style={{
                display: 'flex',
                flexDirection: 'column',
                gap: 8,
                width: layoutTwoCol ? 'auto' : '100%',
                minWidth: layoutTwoCol ? 150 : undefined,
            }}
        >
            {superviseurProps.peutLancerSession && superviseurProps.onLancerSession ? (
                <button
                    type="button"
                    disabled={!creneauAtteint || superviseurProps.lancementEnCours}
                    title={titreLancer}
                    onClick={() => {
                        if (!creneauAtteint || superviseurProps.lancementEnCours) return
                        fireSessionLaunch(superviseurProps.onLancerSession)
                    }}
                    style={styleLancerBtn}
                >
                    {superviseurProps.lancementEnCours ? 'Démarrage…' : 'Démarrer et piloter'}
                </button>
            ) : null}
            <button
                type="button"
                disabled={!creneauAtteint}
                title={
                    creneauAtteint
                        ? 'Contrôles en temps réel (questions, pause, fin) — même vue que les étudiants'
                        : "Disponible uniquement à partir de l'heure prévue du créneau"
                }
                onClick={() => {
                    if (creneauAtteint) superviseurProps.onOuvrirPilotage()
                }}
                style={{
                    padding: '8px 18px',
                    borderRadius: 8,
                    border: '1px solid #dbe3f1',
                    background: creneauAtteint ? '#0f1e3d' : '#94a3b8',
                    color: '#fff',
                    fontWeight: 600,
                    fontFamily: sans,
                    fontSize: 13,
                    cursor: creneauAtteint ? 'pointer' : 'not-allowed',
                    opacity: creneauAtteint ? 1 : 0.88,
                    width: '100%',
                    transition: 'background 0.15s, opacity 0.15s',
                }}
            >
                Espace superviseur
            </button>
        </div>
    )
}

type EtudiantActionsProps = {
    readonly layoutTwoCol: boolean
    readonly creneauAtteint: boolean
    readonly onRejoindre: () => void
}

function EtudiantRejoindreButton({ layoutTwoCol, creneauAtteint, onRejoindre }: EtudiantActionsProps) {
    return (
        <button
            type="button"
            disabled={!creneauAtteint}
            title={
                creneauAtteint
                    ? "Rejoindre la session web (salle d'attente ou épreuve)"
                    : "Disponible uniquement à partir de l'heure prévue du créneau"
            }
            onClick={() => {
                if (creneauAtteint) onRejoindre()
            }}
            style={{
                padding: '8px 18px',
                borderRadius: 8,
                border: 'none',
                background: creneauAtteint ? '#0f1e3d' : '#94a3b8',
                color: '#fff',
                fontWeight: 600,
                fontFamily: sans,
                fontSize: 13,
                cursor: creneauAtteint ? 'pointer' : 'not-allowed',
                opacity: creneauAtteint ? 1 : 0.88,
                width: layoutTwoCol ? 'auto' : '100%',
                minWidth: layoutTwoCol ? 120 : undefined,
                transition: 'background 0.15s, opacity 0.15s',
            }}
        >
            Rejoindre
        </button>
    )
}

function TermineeMessageEtudiant({ layoutTwoCol }: Pick<EtudiantActionsProps, 'layoutTwoCol'>) {
    return (
        <p
            style={{
                margin: 0,
                maxWidth: layoutTwoCol ? 260 : undefined,
                fontSize: 13,
                lineHeight: 1.5,
                color: '#64748b',
                textAlign: layoutTwoCol ? 'right' : 'left',
            }}
        >
            Session terminée. Votre note sera communiquée ultérieurement par votre professeur.
        </p>
    )
}

function renderColonDroiteActions(opts: {
    superviseurProps: ExamenListeCardProps['superviseurProps']
    sessionTermineeEtudiant: boolean
    layoutTwoCol: boolean
    creneauAtteint: boolean
    accentBleu: string
    onRejoindre: () => void
}): ReactNode {
    if (opts.superviseurProps) {
        return (
            <SuperviseurBlock
                superviseurProps={opts.superviseurProps}
                layoutTwoCol={opts.layoutTwoCol}
                creneauAtteint={opts.creneauAtteint}
                accentBleu={opts.accentBleu}
            />
        )
    }
    if (opts.sessionTermineeEtudiant) {
        return <TermineeMessageEtudiant layoutTwoCol={opts.layoutTwoCol} />
    }
    return (
        <EtudiantRejoindreButton
            layoutTwoCol={opts.layoutTwoCol}
            creneauAtteint={opts.creneauAtteint}
            onRejoindre={opts.onRejoindre}
        />
    )
}

export function ExamenListeCard({
    examen: e,
    accentBleu,
    layoutTwoCol,
    creneauAtteint,
    onRejoindre,
    sessionTermineeEtudiant = false,
    superviseurProps,
}: ExamenListeCardProps) {
    const titreBrut = (e.titre ?? '').trim() || 'Examen'
    const titreAffiche =
        titreBrut.toLowerCase().startsWith('examen') ? titreBrut : `Examen — ${titreBrut}`
    const profNom = e.professeur?.nom?.trim() || null
    const afficheDuree = typeof e.duree === 'number'
    const dateLigne = formatDateTimeUnknown(e.dateDebut)
    const pill = statutPillStyle(e.statut)
    const statutCode = (e.statut ?? '').trim().toUpperCase()
    const statutAffichable = statutCode !== '' && statutCode !== 'PLANIFIE'

    const colonActions = renderColonDroiteActions({
        superviseurProps,
        sessionTermineeEtudiant,
        layoutTwoCol,
        creneauAtteint,
        accentBleu,
        onRejoindre,
    })

    return (
        <li
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
                position: 'relative',
                overflow: 'hidden',
            }}
        >
            <div
                aria-hidden
                style={{
                    position: 'absolute',
                    left: 0,
                    top: 0,
                    bottom: 0,
                    width: 4,
                    background: `linear-gradient(180deg, ${accentBleu} 0%, #0f1e3d 100%)`,
                    borderRadius: '12px 0 0 12px',
                }}
            />
            <div
                style={{
                    display: 'flex',
                    flexDirection: layoutTwoCol ? 'row' : 'column',
                    alignItems: layoutTwoCol ? 'flex-start' : 'stretch',
                    justifyContent: 'space-between',
                    gap: 16,
                    paddingLeft: 6,
                }}
            >
                <div style={{ flex: 1, minWidth: 0 }}>
                    <p
                        style={{
                            margin: 0,
                            fontSize: 11,
                            fontWeight: 600,
                            letterSpacing: '0.06em',
                            textTransform: 'uppercase',
                            color: '#64748b',
                        }}
                    >
                        {superviseurProps ? 'Votre épreuve (pilotage web)' : 'Épreuve supervisée'}
                    </p>
                    <div
                        style={{
                            fontWeight: 700,
                            color: '#0f1e3d',
                            fontSize: '1.05rem',
                            marginTop: 6,
                            marginBottom: 10,
                            lineHeight: 1.35,
                        }}
                    >
                        {titreAffiche}
                    </div>

                    <div
                        style={{
                            fontSize: 13,
                            color: '#64748b',
                            display: 'flex',
                            flexWrap: 'wrap',
                            gap: '6px 10px',
                            alignItems: 'center',
                            marginBottom: 10,
                        }}
                    >
                        {profNom ? (
                            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}>
                                <User size={15} strokeWidth={2} aria-hidden style={{ color: '#94a3b8' }} />
                                <span>
                                    <span style={{ color: '#94a3b8' }}>Prof. </span>
                                    <span style={{ fontWeight: 600, color: '#475569' }}>{profNom}</span>
                                </span>
                            </span>
                        ) : null}
                        {profNom && afficheDuree ? (
                            <span style={{ color: '#cbd5e1', userSelect: 'none' }} aria-hidden>
                                ·
                            </span>
                        ) : null}
                        {afficheDuree ? (
                            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}>
                                <Clock size={15} strokeWidth={2} aria-hidden style={{ color: '#94a3b8' }} />
                                <span>{e.duree} min</span>
                            </span>
                        ) : null}
                    </div>

                    <div
                        style={{
                            display: 'flex',
                            alignItems: 'flex-start',
                            gap: 10,
                            fontSize: 13,
                            color: '#475569',
                            background: '#f8fafc',
                            border: '1px solid #e2e8f0',
                            borderRadius: 10,
                            padding: '10px 12px',
                            maxWidth: '100%',
                            boxSizing: 'border-box',
                        }}
                    >
                        <CalendarClock
                            size={18}
                            strokeWidth={2}
                            aria-hidden
                            style={{ color: accentBleu, flexShrink: 0, marginTop: 2 }}
                        />
                        <div>
                            <span style={{ display: 'block', color: '#64748b', fontSize: 12, marginBottom: 2 }}>
                                Créneau de démarrage
                            </span>
                            <span style={{ fontWeight: 600, color: '#0f1e3d', lineHeight: 1.45 }}>{dateLigne}</span>
                        </div>
                    </div>

                    <CreneauHint creneauAtteint={creneauAtteint} />
                </div>

                <div
                    style={{
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: layoutTwoCol ? 'flex-end' : 'stretch',
                        gap: 10,
                        flexShrink: 0,
                        alignSelf: layoutTwoCol ? 'center' : 'stretch',
                    }}
                >
                    {statutAffichable ? (
                        <span
                            style={{
                                fontSize: 11,
                                fontWeight: 600,
                                padding: '5px 11px',
                                borderRadius: 999,
                                ...pill,
                            }}
                        >
                            {formatStatutExamen(e.statut)}
                        </span>
                    ) : null}
                    {colonActions}
                </div>
            </div>
        </li>
    )
}
