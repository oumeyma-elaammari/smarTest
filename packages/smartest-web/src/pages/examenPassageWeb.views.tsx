import type { CSSProperties, ReactElement, ReactNode } from 'react'
import { getEtatSessionLabel as getEtatLabel } from '../utils/examenDisplay'
import type { ExamenMeta, ExamenSnapshot } from '../api/quizSchemas'

const sans = "'DM Sans', system-ui, sans-serif"
const serif = "'DM Serif Display', Georgia, serif"

export type ReponseLigne = { id?: number; contenu?: string }

const card: CSSProperties = {
    background: '#fff',
    border: '1px solid #e2e8f4',
    borderRadius: 14,
    padding: '1.25rem 1.35rem',
    boxShadow: '0 1px 2px rgba(15, 30, 61, 0.04)',
}

const metaTile: CSSProperties = {
    background: '#f8fafc',
    border: '1px solid #e2e8f0',
    borderRadius: 10,
    padding: '0.75rem 0.85rem',
}

function EtatBadge({ canStart, etat }: { canStart: boolean; etat: string }): ReactElement {
    return (
        <span
            style={{
                background: canStart ? '#dcfce7' : '#fff7ed',
                color: canStart ? '#166534' : '#b45309',
                borderRadius: 999,
                padding: '8px 14px',
                fontWeight: 700,
                fontSize: 12,
                whiteSpace: 'nowrap',
                alignSelf: 'flex-start',
            }}
        >
            {getEtatLabel(etat)}
        </span>
    )
}

function StatusWsNotices({ status, wsNotice }: { status: string; wsNotice: string | null }): ReactElement {
    return (
        <>
            {status ? <p style={{ margin: 0, color: '#64748b', fontSize: 14 }}>{status}</p> : null}
            {wsNotice ? <p style={{ margin: 0, color: '#9a3412', fontSize: 13 }}>{wsNotice}</p> : null}
        </>
    )
}

export function ExamenPassageInvalid(): ReactElement {
    return <p style={{ fontFamily: sans, color: '#64748b' }}>Examen invalide.</p>
}

export function ExamenPassageLoading(): ReactElement {
    return (
        <div
            style={{
                maxWidth: 920,
                margin: '0 auto',
                fontFamily: sans,
                color: '#64748b',
                padding: '2rem 0',
                textAlign: 'center',
            }}
        >
            Chargement des informations de l’examen…
        </div>
    )
}

export function ExamenPassageTerminee(opts: {
    shell: CSSProperties
    meta: ExamenMeta | null
    id: number
    onDashboard: () => void
}): ReactElement {
    const { shell, meta, id, onDashboard } = opts
    const titre = meta?.titre?.trim() ? meta.titre : `Examen #${id}`

    return (
        <div style={shell}>
            <div style={card}>
                <p
                    style={{
                        margin: 0,
                        fontSize: 12,
                        fontWeight: 600,
                        color: '#64748b',
                        letterSpacing: '0.04em',
                        textTransform: 'uppercase',
                    }}
                >
                    Examen terminé
                </p>
                <h1
                    style={{
                        margin: '8px 0 0',
                        fontFamily: serif,
                        fontSize: 'clamp(1.25rem, 2.2vw, 1.55rem)',
                        fontWeight: 550,
                        lineHeight: 1.25,
                    }}
                >
                    {titre}
                </h1>
                <p style={{ margin: '14px 0 0', color: '#475569', lineHeight: 1.6, fontSize: 15 }}>
                    Cette session est close : vous ne pouvez plus ouvrir l’épreuve ni modifier vos réponses. Votre note sera
                    communiquée ultérieurement par votre professeur (validation des résultats sur la plateforme).
                </p>
                <button
                    type="button"
                    onClick={onDashboard}
                    style={{
                        marginTop: 18,
                        padding: '10px 18px',
                        borderRadius: 10,
                        border: 'none',
                        background: '#0f1e3d',
                        color: '#fff',
                        fontWeight: 600,
                        fontFamily: sans,
                        fontSize: 14,
                        cursor: 'pointer',
                    }}
                >
                    Retour au tableau de bord
                </button>
            </div>
        </div>
    )
}

type BlocQuestionProps = {
    enPause: boolean
    canStart: boolean
    snap: ExamenSnapshot | null
    questionCourante: ExamenSnapshot['questionCourante']
    reponses: ReponseLigne[]
    questionId: number | null
    selectedResponseId: number | null
    setSelectedResponseId: (v: number | null) => void
    submittingAnswer: boolean
    onValider: () => void
}

type QuestionActiveBlockProps = {
    snap: ExamenSnapshot | null
    questionCourante: ExamenSnapshot['questionCourante']
    reponses: ReponseLigne[]
    questionId: number | null
    selectedResponseId: number | null
    setSelectedResponseId: (v: number | null) => void
    submittingAnswer: boolean
    onValider: () => void
}

function PauseReadonlyBlock(props: {
    snap: ExamenSnapshot | null
    questionCourante: ExamenSnapshot['questionCourante']
    reponses: ReponseLigne[]
}): ReactElement {
    const { snap, questionCourante, reponses } = props
    return (
        <>
            <p style={{ margin: '0 0 12px', color: '#92400e', lineHeight: 1.5 }}>
                Examen en pause. Attendez la reprise par le professeur — vous ne pouvez pas modifier vos réponses pendant la
                pause.
            </p>
            {questionCourante ? (
                <>
                    <p style={{ margin: '0 0 8px', color: '#64748b', fontSize: 13 }}>
                        Question {(snap?.questionCouranteIndex ?? 0) + 1} / {snap?.totalQuestions ?? '—'}{' '}
                        <span style={{ color: '#94a3b8' }}>(affichage seul)</span>
                    </p>
                    <p style={{ margin: '0 0 10px', fontWeight: 600, lineHeight: 1.55 }}>
                        {questionCourante.enonce || 'Question'}
                    </p>
                    <ul style={{ margin: 0, paddingLeft: 18, color: '#475569' }}>
                        {reponses.map((r, idx) => (
                            <li key={typeof r.id === 'number' ? r.id : `r-${idx}`} style={{ marginBottom: 6 }}>
                                {r.contenu || '—'}
                            </li>
                        ))}
                    </ul>
                </>
            ) : null}
        </>
    )
}

function QuestionActiveBlock(props: QuestionActiveBlockProps): ReactElement {
    const {
        snap,
        questionCourante,
        reponses,
        questionId,
        selectedResponseId,
        setSelectedResponseId,
        submittingAnswer,
        onValider,
    } = props

    const submitDisabled = submittingAnswer || selectedResponseId == null

    return (
        <>
            <p style={{ margin: '0 0 8px', color: '#64748b', fontSize: 13 }}>
                Question {(snap?.questionCouranteIndex ?? 0) + 1} / {snap?.totalQuestions ?? '—'}
            </p>
            <p style={{ margin: '0 0 14px', fontWeight: 600, lineHeight: 1.55 }}>
                {questionCourante?.enonce || 'Question en cours'}
            </p>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                {reponses.map((r) => {
                    const rid = typeof r.id === 'number' ? r.id : -1
                    const checked = rid > 0 && selectedResponseId === rid
                    return (
                        <label
                            key={rid}
                            style={{
                                display: 'flex',
                                alignItems: 'flex-start',
                                gap: 10,
                                border: `1px solid ${checked ? '#93c5fd' : '#e2e8f0'}`,
                                borderRadius: 10,
                                background: checked ? '#eff6ff' : '#fff',
                                padding: '10px 12px',
                                cursor: 'pointer',
                            }}
                        >
                            <input
                                type="radio"
                                name={`question-${questionId ?? 'x'}`}
                                checked={checked}
                                onChange={() => setSelectedResponseId(rid > 0 ? rid : null)}
                            />
                            <span style={{ lineHeight: 1.5, color: '#0f1e3d' }}>{r.contenu || 'Réponse'}</span>
                        </label>
                    )
                })}
            </div>

            <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', marginTop: 14 }}>
                <button
                    type="button"
                    onClick={onValider}
                    disabled={submitDisabled}
                    style={{
                        background: '#0f1e3d',
                        color: '#fff',
                        border: '1px solid #0f1e3d',
                        borderRadius: 10,
                        padding: '10px 14px',
                        fontWeight: 700,
                        cursor: submitDisabled ? 'default' : 'pointer',
                        opacity: submitDisabled ? 0.65 : 1,
                    }}
                >
                    {submittingAnswer ? 'Validation...' : 'Valider ma réponse'}
                </button>
            </div>

            <p style={{ margin: '12px 0 0', color: '#64748b', fontSize: 13 }}>
                Le passage entre les questions est imposé par le professeur. Aucune correction ni score pendant l’épreuve.
            </p>
        </>
    )
}

function EnAttenteQuestionContenu(): ReactElement {
    return (
        <p style={{ margin: 0, color: '#64748b', lineHeight: 1.5 }}>
            En attente du contenu de la question… Si cela dure, vérifiez votre connexion. Le professeur pilote l’épreuve : une
            seule question à la fois s’affiche pour toute la classe, comme un quiz guidé.
        </p>
    )
}

function AvantDemarrageMessage(): ReactElement {
    return (
        <p style={{ margin: 0, color: '#64748b', lineHeight: 1.5 }}>
            Les questions apparaîtront une par une dès que le professeur lance l’examen ; la question affichée est la même pour tout
            le monde, contrôlée par le professeur.
        </p>
    )
}

export function ExamenBlocQuestion(props: BlocQuestionProps): ReactElement {
    const {
        enPause,
        canStart,
        questionCourante,
        snap,
        reponses,
        questionId,
        selectedResponseId,
        setSelectedResponseId,
        submittingAnswer,
        onValider,
    } = props

    if (enPause) {
        return <PauseReadonlyBlock snap={snap} questionCourante={questionCourante} reponses={reponses} />
    }

    if (canStart && questionCourante) {
        return (
            <QuestionActiveBlock
                snap={snap}
                questionCourante={questionCourante}
                reponses={reponses}
                questionId={questionId}
                selectedResponseId={selectedResponseId}
                setSelectedResponseId={setSelectedResponseId}
                submittingAnswer={submittingAnswer}
                onValider={onValider}
            />
        )
    }

    if (canStart) {
        return <EnAttenteQuestionContenu />
    }

    return <AvantDemarrageMessage />
}

type EpreuveHeaderProps = {
    meta: ExamenMeta | null
    id: number
    tempsRestantAffiche: string | null
    canStart: boolean
    etat: string
}

function EpreuveHeader({ meta, id, tempsRestantAffiche, canStart, etat }: EpreuveHeaderProps): ReactElement {
    return (
        <div style={card}>
            <div
                style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'flex-start',
                    gap: 16,
                    flexWrap: 'wrap',
                }}
            >
                <div style={{ flex: '1 1 240px' }}>
                    <p
                        style={{
                            margin: 0,
                            fontSize: 12,
                            fontWeight: 600,
                            color: '#64748b',
                            letterSpacing: '0.04em',
                            textTransform: 'uppercase',
                        }}
                    >
                        Épreuve en cours
                    </p>
                    <h1
                        style={{
                            margin: '6px 0 0',
                            fontFamily: serif,
                            fontSize: 'clamp(1.25rem, 2.2vw, 1.6rem)',
                            fontWeight: 550,
                            lineHeight: 1.2,
                        }}
                    >
                        {meta?.titre ?? `Examen #${id}`}
                    </h1>
                    {tempsRestantAffiche != null ? (
                        <p style={{ margin: '10px 0 0', color: '#475569', fontSize: 14 }} aria-live="polite" aria-atomic="true">
                            Temps restant : <strong>{tempsRestantAffiche}</strong>
                        </p>
                    ) : null}
                </div>
                <EtatBadge canStart={canStart} etat={etat} />
            </div>
        </div>
    )
}

export function ExamenPassageEpreuveLayout(props: {
    shell: CSSProperties
    meta: ExamenMeta | null
    id: number
    tempsRestantAffiche: string | null
    canStart: boolean
    etat: string
    status: string
    wsNotice: string | null
    blocQuestion: ReactNode
}): ReactElement {
    const { shell, meta, id, tempsRestantAffiche, canStart, etat, status, wsNotice, blocQuestion } = props

    return (
        <div style={shell}>
            <EpreuveHeader
                meta={meta}
                id={id}
                tempsRestantAffiche={tempsRestantAffiche}
                canStart={canStart}
                etat={etat}
            />
            <StatusWsNotices status={status} wsNotice={wsNotice} />
            <div style={card}>
                <h2
                    style={{
                        margin: '0 0 10px',
                        fontFamily: serif,
                        fontSize: '1.25rem',
                        fontWeight: 550,
                    }}
                >
                    Question en cours
                </h2>
                {blocQuestion}
            </div>
        </div>
    )
}

export function ExamenPassageAttenteLayout(props: {
    shell: CSSProperties
    meta: ExamenMeta | null
    id: number
    canStart: boolean
    etat: string
    dateExamen: string
    creneauAtteint: boolean
    joined: boolean
    heureLancement: string
    status: string
    wsNotice: string | null
}): ReactElement {
    const {
        shell,
        meta,
        id,
        canStart,
        etat,
        dateExamen,
        creneauAtteint,
        joined,
        heureLancement,
        status,
        wsNotice,
    } = props

    const description = meta?.description?.trim()
        ? meta.description
        : 'Votre professeur a publié cet examen sur la plateforme. Les questions ne sont visibles qu’une fois la session lancée.'

    const dureeTxt = meta?.duree != null ? `${meta.duree} min` : '—'
    const enseignantNom = meta?.professeurNom?.trim() || '—'

    return (
        <div style={shell}>
            <div style={card}>
                <div
                    style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'flex-start',
                        gap: 16,
                        flexWrap: 'wrap',
                    }}
                >
                    <div style={{ flex: '1 1 240px' }}>
                        <p
                            style={{
                                margin: 0,
                                fontSize: 12,
                                fontWeight: 600,
                                color: '#64748b',
                                letterSpacing: '0.04em',
                                textTransform: 'uppercase',
                            }}
                        >
                            Examen
                        </p>
                        <h1
                            style={{
                                margin: '6px 0 0',
                                fontFamily: serif,
                                fontSize: 'clamp(1.35rem, 2.5vw, 1.75rem)',
                                fontWeight: 550,
                                lineHeight: 1.2,
                            }}
                        >
                            {meta?.titre ?? `Examen #${id}`}
                        </h1>
                        <p style={{ margin: '10px 0 0', color: '#475569', lineHeight: 1.55, fontSize: 15 }}>
                            {description}
                        </p>
                    </div>
                    <EtatBadge canStart={canStart} etat={etat} />
                </div>
            </div>

            <div style={card}>
                <h2
                    style={{
                        margin: '0 0 14px',
                        fontFamily: serif,
                        fontSize: '1.25rem',
                        fontWeight: 550,
                    }}
                >
                    Informations sur l’épreuve
                </h2>
                <div
                    style={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
                        gap: 10,
                    }}
                >
                    <div style={metaTile}>
                        <div style={{ color: '#64748b', fontSize: 12, marginBottom: 4 }}>
                            Date et heure de lancement (créneau saisi pour l’épreuve)
                        </div>
                        <div style={{ fontWeight: 600, fontSize: 14 }}>{dateExamen}</div>
                    </div>
                    <div style={metaTile}>
                        <div style={{ color: '#64748b', fontSize: 12, marginBottom: 4 }}>Durée</div>
                        <div style={{ fontWeight: 600, fontSize: 14 }}>{dureeTxt}</div>
                    </div>
                    <div style={metaTile}>
                        <div style={{ color: '#64748b', fontSize: 12, marginBottom: 4 }}>Enseignant</div>
                        <div style={{ fontWeight: 600, fontSize: 14 }}>{enseignantNom}</div>
                    </div>
                </div>
            </div>

            <div style={card}>
                <h2
                    style={{
                        margin: '0 0 8px',
                        fontFamily: serif,
                        fontSize: '1.25rem',
                        fontWeight: 550,
                    }}
                >
                    Salle d’attente
                </h2>
                <p style={{ color: '#64748b', margin: '0 0 16px', lineHeight: 1.55, fontSize: 15 }}>
                    {creneauAtteint ? (
                        <>
                            Votre inscription à la salle d’attente est automatique sur cette page. Lorsque le professeur
                            démarre l’examen depuis l’application enseignant, vous êtes redirigé vers la page d’épreuve pour
                            répondre aux questions.
                        </>
                    ) : (
                        <>
                            L’examen devient accessible lorsque l’heure prévue du créneau est atteinte : vous pourrez alors rejoindre
                            la salle d’attente sur cette page. En attendant, consultez les informations ci-dessus ; les questions ne
                            sont pas disponibles avant le lancement par le professeur.
                        </>
                    )}
                </p>

                {joined && !canStart && creneauAtteint ? (
                    <div
                        style={{
                            marginTop: 4,
                            color: '#92400e',
                            background: '#fffbeb',
                            border: '1px solid #fde68a',
                            borderRadius: 10,
                            padding: 14,
                            lineHeight: 1.5,
                        }}
                    >
                        <strong>L’examen va bientôt commencer, veuillez patienter.</strong>
                        <span style={{ display: 'block', marginTop: 8 }}>
                            Vous êtes bien connecté à la salle d’attente (créneau prévu vers <strong>{heureLancement}</strong>).
                            Dès que le professeur clique sur « Démarrer l’examen », vous passerez automatiquement à la page
                            d’épreuve.
                        </span>
                    </div>
                ) : null}

                {status ? (
                    <p style={{ marginTop: 14, marginBottom: 0, color: '#64748b', fontSize: 14 }}>{status}</p>
                ) : null}
                {wsNotice ? (
                    <p style={{ marginTop: 10, marginBottom: 0, color: '#9a3412', fontSize: 13 }}>{wsNotice}</p>
                ) : null}
            </div>
        </div>
    )
}
