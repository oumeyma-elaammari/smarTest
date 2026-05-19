import type { CSSProperties, ReactElement, ReactNode } from 'react'
import { getEtatSessionLabel as getEtatLabel, resolveExamenDisplayTitre } from '../utils/examenDisplay'
import type { ExamenMeta, ExamenSnapshot } from '../api/quizSchemas'
import type { ExamenMinuteurQuestionLive } from '../hooks/useExamenMinuteurQuestionLive'
import { coerceEntityId, examPassQuestionKind } from './examenPassageWeb.shared'

const sans = "'DM Sans', system-ui, sans-serif"
const serif = "'DM Serif Display', Georgia, serif"

type QuestionImageFields = {
    imageBase64?: string | null
    imageType?: string | null
}

function questionImageDataUrl(q: QuestionImageFields | null | undefined): string | null {
    const raw = q?.imageBase64?.trim()
    if (!raw) return null
    const mime = q?.imageType?.trim() || 'image/jpeg'
    return `data:${mime};base64,${raw}`
}

function QuestionImageBlock({ question }: { question: QuestionImageFields | null | undefined }): ReactElement | null {
    const src = questionImageDataUrl(question)
    if (!src) return null
    return (
        <img
            src={src}
            alt="Illustration de la question"
            className="max-w-full rounded-lg my-3"
            style={{ maxWidth: '100%', borderRadius: 12, margin: '12px 0', display: 'block' }}
        />
    )
}

export type ReponseLigne = { id?: number; contenu?: string }

const card: CSSProperties = {
    background: '#fff',
    border: '1px solid #e8edf5',
    borderRadius: 12,
    padding: '1rem 1.1rem',
    boxShadow: '0 1px 2px rgba(15, 30, 61, 0.03)',
}

const metaTile: CSSProperties = {
    background: '#f8fafc',
    border: '1px solid #e8edf5',
    borderRadius: 8,
    padding: '0.55rem 0.65rem',
}

const optionRowBase: CSSProperties = {
    display: 'flex',
    alignItems: 'flex-start',
    gap: 8,
    borderRadius: 8,
    padding: '8px 10px',
    fontSize: 14,
    lineHeight: 1.45,
}

function ValiderReponseButton(props: {
    onValider: () => void
    submittingAnswer: boolean
    reponseVerrouillee: boolean
    submitDisabled: boolean
}): ReactElement {
    const { onValider, submittingAnswer, reponseVerrouillee, submitDisabled } = props
    return (
        <button
            type="button"
            onClick={onValider}
            disabled={submitDisabled}
            style={{
                background: reponseVerrouillee ? '#166534' : '#0f1e3d',
                color: '#fff',
                border: `1px solid ${reponseVerrouillee ? '#166534' : '#0f1e3d'}`,
                borderRadius: 999,
                padding: '9px 18px',
                fontWeight: 700,
                fontSize: 13,
                fontFamily: sans,
                cursor: submitDisabled ? 'default' : 'pointer',
                opacity: submitDisabled ? 0.65 : 1,
                whiteSpace: 'nowrap',
                flexShrink: 0,
            }}
        >
            {submittingAnswer
                ? 'Validation…'
                : reponseVerrouillee
                  ? 'Réponse validée'
                  : 'Valider ma réponse'}
        </button>
    )
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
    const titre = resolveExamenDisplayTitre(meta, null, id)

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

type ExamPassQuestionKind = ReturnType<typeof examPassQuestionKind>

type BlocQuestionProps = {
    enPause: boolean
    canStart: boolean
    snap: ExamenSnapshot | null
    questionCourante: ExamenSnapshot['questionCourante']
    questionKind: ExamPassQuestionKind
    reponses: ReponseLigne[]
    questionId: number | null
    selectedResponseId: number | null
    setSelectedResponseId: (v: number | null) => void
    selectedResponseIds: number[]
    setSelectedResponseIds: (v: number[]) => void
    essayText: string
    setEssayText: (v: string) => void
    submittingAnswer: boolean
    reponseVerrouillee: boolean
    tempsQuestionExpire: boolean
    onValider: () => void
}

type QuestionActiveBlockProps = {
    snap: ExamenSnapshot | null
    questionCourante: ExamenSnapshot['questionCourante']
    questionKind: ExamPassQuestionKind
    reponses: ReponseLigne[]
    questionId: number | null
    selectedResponseId: number | null
    setSelectedResponseId: (v: number | null) => void
    selectedResponseIds: number[]
    setSelectedResponseIds: (v: number[]) => void
    essayText: string
    setEssayText: (v: string) => void
    submittingAnswer: boolean
    reponseVerrouillee: boolean
    tempsQuestionExpire: boolean
    onValider: () => void
}

function PauseReadonlyBlock(props: {
    snap: ExamenSnapshot | null
    questionCourante: ExamenSnapshot['questionCourante']
    questionKind: ExamPassQuestionKind
    reponses: ReponseLigne[]
}): ReactElement {
    const { snap, questionCourante, questionKind, reponses } = props
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
                    <QuestionImageBlock question={questionCourante} />
                    {questionKind === 'essay' ? (
                        <p style={{ margin: 0, color: '#64748b', fontSize: 14 }}>
                            Type rédaction : les étudiants répondent dans une zone de texte libre (non modifiable pendant la pause).
                        </p>
                    ) : (
                        <ul style={{ margin: 0, paddingLeft: 18, color: '#475569' }}>
                            {reponses.map((r, idx) => (
                                <li key={coerceEntityId(r.id) ?? `r-${idx}`} style={{ marginBottom: 6 }}>
                                    {r.contenu || '—'}
                                </li>
                            ))}
                        </ul>
                    )}
                </>
            ) : null}
        </>
    )
}

function QuestionActiveBlock(props: QuestionActiveBlockProps): ReactElement {
    const {
        snap,
        questionCourante,
        questionKind,
        reponses,
        questionId,
        selectedResponseId,
        setSelectedResponseId,
        selectedResponseIds,
        setSelectedResponseIds,
        essayText,
        setEssayText,
        reponseVerrouillee,
        tempsQuestionExpire,
    } = props

    const inputsLocked = reponseVerrouillee || tempsQuestionExpire

    const toggleCheckbox = (rid: number) => {
        if (inputsLocked) return
        setSelectedResponseIds(
            selectedResponseIds.includes(rid)
                ? selectedResponseIds.filter((x) => x !== rid)
                : [...selectedResponseIds, rid],
        )
    }

    return (
        <>
            <p style={{ margin: '0 0 8px', color: '#64748b', fontSize: 13 }}>
                Question {(snap?.questionCouranteIndex ?? 0) + 1} / {snap?.totalQuestions ?? '—'}
            </p>
            <p style={{ margin: '0 0 14px', fontWeight: 600, lineHeight: 1.55 }}>
                {questionCourante?.enonce || 'Question en cours'}
            </p>
            <QuestionImageBlock question={questionCourante} />

            {questionKind === 'essay' ? (
                <textarea
                    value={essayText}
                    onChange={(e) => {
                        if (!inputsLocked) setEssayText(e.target.value)
                    }}
                    disabled={inputsLocked}
                    readOnly={reponseVerrouillee}
                    rows={8}
                    placeholder="Rédigez votre réponse ici…"
                    style={{
                        width: '100%',
                        boxSizing: 'border-box',
                        minHeight: 120,
                        padding: '10px 12px',
                        borderRadius: 8,
                        border: '1px solid #e8edf5',
                        fontFamily: sans,
                        fontSize: 14,
                        lineHeight: 1.5,
                        resize: 'vertical',
                        opacity: inputsLocked ? 0.65 : 1,
                    }}
                />
            ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                    {reponses.map((r, idx) => {
                        const rid = coerceEntityId(r.id)
                        if (questionKind === 'checkbox') {
                            const checked = rid != null && selectedResponseIds.includes(rid)
                            return (
                                <label
                                    key={rid ?? `opt-${idx}`}
                                    style={{
                                        ...optionRowBase,
                                        border: `1px solid ${checked ? '#93c5fd' : '#e8edf5'}`,
                                        background: checked ? '#f0f7ff' : '#fafbfd',
                                        cursor: inputsLocked ? 'not-allowed' : 'pointer',
                                        opacity: inputsLocked ? 0.65 : 1,
                                    }}
                                >
                                    <input
                                        type="checkbox"
                                        checked={checked}
                                        disabled={inputsLocked}
                                        onChange={() => {
                                            if (!inputsLocked && rid != null) toggleCheckbox(rid)
                                        }}
                                    />
                                    <span style={{ lineHeight: 1.5, color: '#0f1e3d' }}>{r.contenu || 'Réponse'}</span>
                                </label>
                            )
                        }
                        const checked = rid != null && selectedResponseId === rid
                        return (
                            <label
                                key={rid ?? `opt-${idx}`}
                                style={{
                                    ...optionRowBase,
                                    border: `1px solid ${checked ? '#93c5fd' : '#e8edf5'}`,
                                    background: checked ? '#f0f7ff' : '#fafbfd',
                                    cursor: inputsLocked ? 'not-allowed' : 'pointer',
                                    opacity: inputsLocked ? 0.65 : 1,
                                }}
                            >
                                <input
                                    type="radio"
                                    name={`question-${questionId ?? 'x'}`}
                                    checked={checked}
                                    disabled={inputsLocked}
                                    onChange={() => {
                                        if (!inputsLocked && rid != null) setSelectedResponseId(rid)
                                    }}
                                />
                                <span style={{ lineHeight: 1.5, color: '#0f1e3d' }}>{r.contenu || 'Réponse'}</span>
                            </label>
                        )
                    })}
                </div>
            )}

            {reponseVerrouillee ? (
                <p style={{ margin: '12px 0 0', color: '#166534', fontSize: 14, lineHeight: 1.5 }}>
                    Réponse validée : vous ne pouvez plus la modifier pour cette question.
                </p>
            ) : tempsQuestionExpire ? (
                <p style={{ margin: '12px 0 0', color: '#92400e', fontSize: 14, lineHeight: 1.5 }}>
                    Temps écoulé pour cette question : vous ne pouvez plus modifier votre réponse tant que le professeur n’a pas
                    ajouté du temps au minuteur.
                </p>
            ) : null}

            {!reponseVerrouillee && !tempsQuestionExpire ? (
                <p
                    style={{
                        margin: '12px 0 0',
                        padding: '10px 12px',
                        borderRadius: 10,
                        background: '#fffbeb',
                        border: '1px solid #fcd34d',
                        color: '#92400e',
                        fontSize: 14,
                        lineHeight: 1.5,
                    }}
                    role="note"
                >
                    <strong>Important :</strong> si vous ne cliquez pas sur « Valider ma réponse », votre choix ne sera pas
                    compté pour cette question.
                </p>
            ) : null}

            <p style={{ margin: '10px 0 0', color: '#64748b', fontSize: 12 }}>
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
        questionKind,
        reponses,
        questionId,
        selectedResponseId,
        setSelectedResponseId,
        selectedResponseIds,
        setSelectedResponseIds,
        essayText,
        setEssayText,
        submittingAnswer,
        reponseVerrouillee,
        tempsQuestionExpire,
        onValider,
    } = props

    if (enPause) {
        return (
            <PauseReadonlyBlock
                snap={snap}
                questionCourante={questionCourante}
                questionKind={questionKind}
                reponses={reponses}
            />
        )
    }

    if (canStart && questionCourante) {
        return (
            <QuestionActiveBlock
                snap={snap}
                questionCourante={questionCourante}
                questionKind={questionKind}
                reponses={reponses}
                questionId={questionId}
                selectedResponseId={selectedResponseId}
                setSelectedResponseId={setSelectedResponseId}
                selectedResponseIds={selectedResponseIds}
                setSelectedResponseIds={setSelectedResponseIds}
                essayText={essayText}
                setEssayText={setEssayText}
                submittingAnswer={submittingAnswer}
                reponseVerrouillee={reponseVerrouillee}
                tempsQuestionExpire={tempsQuestionExpire}
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
    snap: ExamenSnapshot | null
    id: number
    canStart: boolean
    etat: string
}

function EpreuveHeader({ meta, snap, id, canStart, etat }: EpreuveHeaderProps): ReactElement {
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
                        {resolveExamenDisplayTitre(meta, snap, id)}
                    </h1>
                </div>
                <EtatBadge canStart={canStart} etat={etat} />
            </div>
        </div>
    )
}

export function ExamenPassageEpreuveLayout(props: {
    shell: CSSProperties
    meta: ExamenMeta | null
    snap: ExamenSnapshot | null
    id: number
    canStart: boolean
    etat: string
    status: string
    wsNotice: string | null
    questionHeading: string
    minuteurQuestion: ExamenMinuteurQuestionLive
    blocQuestion: ReactNode
    afficherValider: boolean
    onValider: () => void
    submittingAnswer: boolean
    reponseVerrouillee: boolean
    submitDisabled: boolean
}): ReactElement {
    const {
        shell,
        meta,
        snap,
        id,
        canStart,
        etat,
        status,
        wsNotice,
        questionHeading,
        minuteurQuestion,
        blocQuestion,
        afficherValider,
        onValider,
        submittingAnswer,
        reponseVerrouillee,
        submitDisabled,
    } = props

    return (
        <div style={shell}>
            <EpreuveHeader meta={meta} snap={snap} id={id} canStart={canStart} etat={etat} />
            <StatusWsNotices status={status} wsNotice={wsNotice} />
            <div style={card}>
                <div
                    style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'flex-start',
                        gap: 12,
                        flexWrap: 'wrap',
                        marginBottom: 10,
                    }}
                >
                    <h2
                        style={{
                            margin: 0,
                            fontFamily: serif,
                            fontSize: '1.1rem',
                            fontWeight: 550,
                            flex: '1 1 200px',
                        }}
                    >
                        {questionHeading}
                    </h2>
                    {afficherValider ? (
                        <ValiderReponseButton
                            onValider={onValider}
                            submittingAnswer={submittingAnswer}
                            reponseVerrouillee={reponseVerrouillee}
                            submitDisabled={submitDisabled}
                        />
                    ) : null}
                </div>
                {minuteurQuestion.formatted != null ? (
                    <p style={{ margin: '0 0 12px', color: '#64748b', fontSize: 13 }} aria-live="polite">
                        Décompte (indication) : <strong>{minuteurQuestion.formatted}</strong>
                        {minuteurQuestion.isExpired ? (
                            <span style={{ color: '#92400e', fontWeight: 600 }}>
                                {' '}
                                — temps écoulé ; vos réponses sont verrouillées jusqu’à ajout de temps par le professeur.
                            </span>
                        ) : null}
                    </p>
                ) : null}
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
                            {resolveExamenDisplayTitre(meta, null, id)}
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
