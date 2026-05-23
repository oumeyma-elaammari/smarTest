import { CalendarClock, Clock, Loader2, Trash2, User } from 'lucide-react'
import type { ReactNode } from 'react'
import type { ExamenListeItem } from '../../api/quizSchemas'
import { formatDateTimeUnknown, formatStatutExamen } from '../../utils/examenDisplay'
import { couleurNoteSur20 } from '../../utils/noteAffichage'

const sans = "'DM Sans', system-ui, sans-serif"

function statutPillStyle(statut?: string | null): { background: string; color: string; border: string } {
    const u = (statut ?? '').toUpperCase()
    if (u === 'EN_COURS') {
        return { background: '#dcfce7', color: '#166534', border: '1px solid #bbf7d0' }
    }
    if (u === 'EN_PAUSE') {
        return { background: '#dbeafe', color: '#1d4ed8', border: '1px solid #bfdbfe' }
    }
    if (u === 'TERMINE') {
        return { background: '#f1f5f9', color: '#475569', border: '1px solid #e2e8f0' }
    }
    if (u === 'ANNULE') {
        return { background: '#fee2e2', color: '#b91c1c', border: '1px solid #fecaca' }
    }
    if (u === 'PLANIFIE') {
        return { background: '#fff7ed', color: '#b45309', border: '1px solid #fed7aa' }
    }
    return { background: '#f1f5f9', color: '#475569', border: '1px solid #e2e8f0' }
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
    /** Si défini, affichage « Note : X / Y » à la place du message « session terminée ». */
    readonly noteEtudiantPubliee?: { valeur: number; sur: number } | null
    /** Si défini : affichage superviseur avec pilotage dédié. */
    readonly superviseurProps?: {
        readonly onOuvrirPilotage: () => void
        /** Supprime l'examen sur le serveur (prof + étudiants). */
        readonly onSupprimer?: () => void | Promise<void>
        readonly suppressionEnCours?: boolean
    }
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
            }}
        >
            L'examen doit être accessible lorsque l'heure prévue du créneau est atteinte.
        </p>
    )
}

type SuperviseurBlockProps = {
    readonly superviseurProps: NonNullable<ExamenListeCardProps['superviseurProps']>
    readonly creneauAtteint: boolean
}

function SuperviseurBlock({ superviseurProps, creneauAtteint }: SuperviseurBlockProps) {
    return (
        <button
            type="button"
            disabled={!creneauAtteint}
            title={
                creneauAtteint
                    ? 'Piloter l’examen en temps réel (questions, pause, fin)'
                    : "Disponible uniquement à partir de l'heure prévue du créneau"
            }
            onClick={() => {
                if (creneauAtteint) superviseurProps.onOuvrirPilotage()
            }}
            style={{
                padding: '7px 16px',
                borderRadius: 8,
                border: '1px solid #dbe3f1',
                background: creneauAtteint ? '#0f1e3d' : '#94a3b8',
                color: '#fff',
                fontWeight: 600,
                fontFamily: sans,
                fontSize: 13,
                cursor: creneauAtteint ? 'pointer' : 'not-allowed',
                opacity: creneauAtteint ? 1 : 0.88,
                width: 'auto',
                minWidth: 120,
                transition: 'background 0.15s, opacity 0.15s',
            }}
        >
            Superviser
        </button>
    )
}

type ProfStatutRowProps = {
    readonly statutAffichable: boolean
    readonly statut?: string | null
    readonly pill: { background: string; color: string; border: string }
    readonly superviseurProps: NonNullable<ExamenListeCardProps['superviseurProps']>
}

function ProfStatutRow({ statutAffichable, statut, pill, superviseurProps }: ProfStatutRowProps) {
    if (!statutAffichable && !superviseurProps.onSupprimer) return null
    return (
        <div
            style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'flex-end',
                gap: 4,
                flexShrink: 0,
            }}
        >
            {statutAffichable ? (
                <span
                    style={{
                        fontSize: 11,
                        fontWeight: 600,
                        padding: '5px 11px',
                        borderRadius: 999,
                        whiteSpace: 'nowrap',
                        ...pill,
                    }}
                >
                    {formatStatutExamen(statut)}
                </span>
            ) : null}
            {superviseurProps.onSupprimer ? (
                <SuppressionIconButton
                    onSupprimer={superviseurProps.onSupprimer}
                    suppressionEnCours={superviseurProps.suppressionEnCours}
                />
            ) : null}
        </div>
    )
}

type SuppressionIconProps = {
    readonly onSupprimer: () => void | Promise<void>
    readonly suppressionEnCours?: boolean
}

function SuppressionIconButton({ onSupprimer, suppressionEnCours }: SuppressionIconProps) {
    return (
        <button
            type="button"
            disabled={suppressionEnCours}
            title="Supprimer cet examen pour vous et pour tous les étudiants"
            aria-label="Supprimer l'examen"
            onClick={() => {
                if (suppressionEnCours) return
                void onSupprimer()
            }}
            style={{
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                width: 28,
                height: 28,
                padding: 0,
                borderRadius: 6,
                border: '1px solid transparent',
                background: 'transparent',
                color: suppressionEnCours ? '#cbd5e1' : '#dc2626',
                cursor: suppressionEnCours ? 'not-allowed' : 'pointer',
                flexShrink: 0,
                transition: 'color 0.15s, background 0.15s, border-color 0.15s',
            }}
            onMouseEnter={(ev) => {
                if (suppressionEnCours) return
                ev.currentTarget.style.color = '#b91c1c'
                ev.currentTarget.style.background = '#fef2f2'
                ev.currentTarget.style.borderColor = '#fecaca'
            }}
            onMouseLeave={(ev) => {
                ev.currentTarget.style.color = suppressionEnCours ? '#cbd5e1' : '#dc2626'
                ev.currentTarget.style.background = 'transparent'
                ev.currentTarget.style.borderColor = 'transparent'
            }}
        >
            {suppressionEnCours ? (
                <Loader2 size={15} strokeWidth={2} className="animate-spin" aria-hidden />
            ) : (
                <Trash2 size={15} strokeWidth={2} aria-hidden />
            )}
        </button>
    )
}

type EtudiantActionsProps = {
    readonly layoutTwoCol: boolean
    readonly creneauAtteint: boolean
    readonly onRejoindre: () => void
}

function EtudiantRejoindreButton({ creneauAtteint, onRejoindre }: Omit<EtudiantActionsProps, 'layoutTwoCol'>) {
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
                padding: '7px 16px',
                borderRadius: 8,
                border: '1px solid #dbe3f1',
                background: creneauAtteint ? '#0f1e3d' : '#94a3b8',
                color: '#fff',
                fontWeight: 600,
                fontFamily: sans,
                fontSize: 13,
                cursor: creneauAtteint ? 'pointer' : 'not-allowed',
                opacity: creneauAtteint ? 1 : 0.88,
                width: 'auto',
                minWidth: 120,
                transition: 'background 0.15s, opacity 0.15s',
            }}
        >
            Rejoindre
        </button>
    )
}

function StatutPill({ statut, pill }: { readonly statut?: string | null; readonly pill: { background: string; color: string; border: string } }) {
    return (
        <span
            style={{
                fontSize: 11,
                fontWeight: 600,
                padding: '5px 11px',
                borderRadius: 999,
                whiteSpace: 'nowrap',
                ...pill,
            }}
        >
            {formatStatutExamen(statut)}
        </span>
    )
}

function NoteEtudiantPubliee({ note }: { readonly note: { valeur: number; sur: number } }) {
    const v = Number.isFinite(note.valeur) ? note.valeur.toFixed(2) : String(note.valeur)
    const s = Number.isFinite(note.sur) ? note.sur.toFixed(2) : String(note.sur)
    const couleur =
        typeof note.valeur === 'number' && Number.isFinite(note.valeur)
            ? couleurNoteSur20(note.valeur, note.sur)
            : '#0f1e3d'
    return (
        <p
            style={{
                margin: 0,
                fontSize: 14,
                lineHeight: 1.5,
                color: couleur,
                fontWeight: 700,
                textAlign: 'right',
                whiteSpace: 'nowrap',
            }}
        >
            Note : {v} / {s}
        </p>
    )
}

function TermineeMessageEtudiant() {
    return (
        <p
            style={{
                margin: '4px 0 0',
                fontSize: 13,
                lineHeight: 1.5,
                color: '#64748b',
            }}
        >
            Session terminée. Votre note sera communiquée ultérieurement par votre professeur.
        </p>
    )
}

function renderActionEtudiant(opts: {
    sessionTermineeEtudiant: boolean
    noteEtudiantPubliee: ExamenListeCardProps['noteEtudiantPubliee']
    creneauAtteint: boolean
    onRejoindre: () => void
}): ReactNode {
    if (opts.sessionTermineeEtudiant) {
        if (opts.noteEtudiantPubliee) {
            return <NoteEtudiantPubliee note={opts.noteEtudiantPubliee} />
        }
        return null
    }
    return (
        <EtudiantRejoindreButton creneauAtteint={opts.creneauAtteint} onRejoindre={opts.onRejoindre} />
    )
}

export function ExamenListeCard({
    examen: e,
    accentBleu,
    creneauAtteint,
    onRejoindre,
    sessionTermineeEtudiant = false,
    noteEtudiantPubliee = null,
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
    const statutAffichable = statutCode !== ''

    const actionEtudiant = renderActionEtudiant({
        sessionTermineeEtudiant,
        noteEtudiantPubliee,
        creneauAtteint,
        onRejoindre,
    })

    return (
        <li
            style={{
                fontFamily: sans,
                border: '1px solid #e2e8f4',
                borderRadius: 12,
                padding: 'clamp(14px, 3vw, 18px)',
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
            {superviseurProps ? (
                <div
                    style={{
                        display: 'flex',
                        flexDirection: 'column',
                        width: '100%',
                        paddingLeft: 6,
                    }}
                >
                    <div
                        style={{
                            display: 'flex',
                            alignItems: 'flex-start',
                            justifyContent: 'space-between',
                            gap: 8,
                            marginBottom: 4,
                        }}
                    >
                        <div style={{ flex: 1, minWidth: 0 }}>
                            <div
                                style={{
                                    fontWeight: 700,
                                    color: '#0f1e3d',
                                    fontSize: 'clamp(1rem, 2.8vw, 1.05rem)',
                                    lineHeight: 1.35,
                                }}
                            >
                                {titreAffiche}
                            </div>
                        </div>
                        <ProfStatutRow
                            statutAffichable={statutAffichable}
                            statut={e.statut}
                            pill={pill}
                            superviseurProps={superviseurProps}
                        />
                    </div>

                    {profNom ? (
                        <div
                            style={{
                                fontSize: 13,
                                color: '#64748b',
                                display: 'flex',
                                flexWrap: 'wrap',
                                gap: '6px 10px',
                                alignItems: 'center',
                                marginBottom: 4,
                            }}
                        >
                            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}>
                                <User size={15} strokeWidth={2} aria-hidden style={{ color: '#94a3b8' }} />
                                <span>
                                    <span style={{ color: '#94a3b8' }}>Prof. </span>
                                    <span style={{ fontWeight: 600, color: '#475569' }}>{profNom}</span>
                                </span>
                            </span>
                        </div>
                    ) : null}

                    {(dateLigne !== '—' || afficheDuree) ? (
                        <div
                            style={{
                                display: 'flex',
                                flexDirection: 'column',
                                alignItems: 'flex-start',
                                gap: 4,
                                width: '100%',
                            }}
                        >
                            {dateLigne !== '—' ? (
                                <p
                                    style={{
                                        margin: 0,
                                        fontSize: 13,
                                        color: '#64748b',
                                        display: 'flex',
                                        alignItems: 'center',
                                        gap: 5,
                                    }}
                                >
                                    <CalendarClock size={15} strokeWidth={2} aria-hidden style={{ color: '#94a3b8' }} />
                                    <span style={{ fontWeight: 600, color: '#475569' }}>{dateLigne}</span>
                                </p>
                            ) : null}

                            <div
                                style={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'space-between',
                                    gap: 8,
                                    width: '100%',
                                }}
                            >
                                {afficheDuree ? (
                                    <p
                                        style={{
                                            margin: 0,
                                            fontSize: 13,
                                            color: '#64748b',
                                            display: 'flex',
                                            alignItems: 'center',
                                            gap: 5,
                                        }}
                                    >
                                        <Clock size={15} strokeWidth={2} aria-hidden style={{ color: '#94a3b8' }} />
                                        <span style={{ fontWeight: 600, color: '#475569' }}>{e.duree} min</span>
                                    </p>
                                ) : (
                                    <span aria-hidden />
                                )}
                                <SuperviseurBlock
                                    superviseurProps={superviseurProps}
                                    creneauAtteint={creneauAtteint}
                                />
                            </div>
                        </div>
                    ) : (
                        <div style={{ display: 'flex', justifyContent: 'flex-end', width: '100%', marginTop: 2 }}>
                            <SuperviseurBlock superviseurProps={superviseurProps} creneauAtteint={creneauAtteint} />
                        </div>
                    )}

                    <CreneauHint creneauAtteint={creneauAtteint} />
                </div>
            ) : (
                <div
                    style={{
                        display: 'flex',
                        flexDirection: 'column',
                        width: '100%',
                        paddingLeft: 6,
                    }}
                >
                    <div
                        style={{
                            display: 'flex',
                            alignItems: 'flex-start',
                            justifyContent: 'space-between',
                            gap: 8,
                            marginBottom: 4,
                        }}
                    >
                        <div style={{ flex: 1, minWidth: 0 }}>
                            <div
                                style={{
                                    fontWeight: 700,
                                    color: '#0f1e3d',
                                    fontSize: 'clamp(1rem, 2.8vw, 1.05rem)',
                                    lineHeight: 1.35,
                                }}
                            >
                                {titreAffiche}
                            </div>
                        </div>
                        {statutAffichable ? <StatutPill statut={e.statut} pill={pill} /> : null}
                    </div>

                    {profNom ? (
                        <div
                            style={{
                                fontSize: 13,
                                color: '#64748b',
                                display: 'flex',
                                flexWrap: 'wrap',
                                gap: '6px 10px',
                                alignItems: 'center',
                                marginBottom: 4,
                            }}
                        >
                            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}>
                                <User size={15} strokeWidth={2} aria-hidden style={{ color: '#94a3b8' }} />
                                <span>
                                    <span style={{ color: '#94a3b8' }}>Prof. </span>
                                    <span style={{ fontWeight: 600, color: '#475569' }}>{profNom}</span>
                                </span>
                            </span>
                        </div>
                    ) : null}

                    {(dateLigne !== '—' || afficheDuree || actionEtudiant) ? (
                        <div
                            style={{
                                display: 'flex',
                                flexDirection: 'column',
                                alignItems: 'flex-start',
                                gap: 4,
                                width: '100%',
                            }}
                        >
                            {dateLigne !== '—' ? (
                                <p
                                    style={{
                                        margin: 0,
                                        fontSize: 13,
                                        color: '#64748b',
                                        display: 'flex',
                                        alignItems: 'center',
                                        gap: 5,
                                    }}
                                >
                                    <CalendarClock size={15} strokeWidth={2} aria-hidden style={{ color: '#94a3b8' }} />
                                    <span style={{ fontWeight: 600, color: '#475569' }}>{dateLigne}</span>
                                </p>
                            ) : null}

                            {(afficheDuree || actionEtudiant) ? (
                                <div
                                    style={{
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'space-between',
                                        gap: 8,
                                        width: '100%',
                                    }}
                                >
                                    {afficheDuree ? (
                                        <p
                                            style={{
                                                margin: 0,
                                                fontSize: 13,
                                                color: '#64748b',
                                                display: 'flex',
                                                alignItems: 'center',
                                                gap: 5,
                                            }}
                                        >
                                            <Clock size={15} strokeWidth={2} aria-hidden style={{ color: '#94a3b8' }} />
                                            <span style={{ fontWeight: 600, color: '#475569' }}>{e.duree} min</span>
                                        </p>
                                    ) : (
                                        <span aria-hidden />
                                    )}
                                    {actionEtudiant}
                                </div>
                            ) : null}
                        </div>
                    ) : null}

                    {sessionTermineeEtudiant && !noteEtudiantPubliee ? <TermineeMessageEtudiant /> : null}

                    <CreneauHint creneauAtteint={creneauAtteint} />
                </div>
            )}
        </li>
    )
}
