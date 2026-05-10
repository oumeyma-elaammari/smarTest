import type { CSSProperties } from 'react'
import { useMemo, useRef, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { examenApi } from '../api/examenApi'
import type { ExamenMeta, ExamenSnapshot } from '../api/quizSchemas'
import {
    formatDateTime,
    formatTime,
    parseDebutExamenMs,
} from '../utils/examenDisplay'
import { useExamenMinuteurQuestionLive } from '../hooks/useExamenMinuteurQuestionLive'
import { useExamenTempsRestantLive } from '../hooks/useExamenTempsRestantLive'
import {
    useAutoJoinSalleAttente,
    useCreneauTicker,
    useExamenMetaLoad,
    useExamenPolling,
    useExamenStomp,
    useJoinedSiSessionActive,
    useNavigateEpreuveSiMetaDejaEnCours,
    useNavigateVersEpreuveSurDemarrage,
    useQuestionSelectionClear,
    useRedirectDashboardSiArrete,
    useResetAutoJoinOnId,
    useRetourAttenteSiEpreuveTropTot,
    useSnapClearsJoinedOnArrete,
    deriveIsEpreuvePath,
} from './examenPassageWeb.hooks'
import { extractApiMessage, readEtudiantId } from './examenPassageWeb.shared'
import {
    ExamenBlocQuestion,
    ExamenPassageAttenteLayout,
    ExamenPassageEpreuveLayout,
    ExamenPassageInvalid,
    ExamenPassageLoading,
    ExamenPassageTerminee,
} from './examenPassageWeb.views'

const sans = "'DM Sans', system-ui, sans-serif"

async function soumettreReponseVersApi(opts: {
    id: number
    etudiantId: number
    questionId: number | null
    selectedResponseId: number | null
    canStart: boolean
    enPause: boolean
    setSubmittingAnswer: (v: boolean) => void
    setLastAnsweredQuestionId: (v: number) => void
    setStatus: (v: string) => void
}): Promise<boolean> {
    const {
        id,
        etudiantId,
        questionId,
        selectedResponseId,
        canStart,
        enPause,
        setSubmittingAnswer,
        setLastAnsweredQuestionId,
        setStatus,
    } = opts

    if (!Number.isFinite(id) || id <= 0) return false
    if (!canStart || enPause) return false
    if (questionId === null || selectedResponseId === null) {
        setStatus('Sélectionnez une réponse avant de valider.')
        return false
    }

    try {
        setSubmittingAnswer(true)
        await examenApi.repondreQuestionCourante(id, etudiantId, questionId, selectedResponseId)
        setLastAnsweredQuestionId(questionId)
        setStatus('Réponse enregistrée. Aucune correction immédiate n’est affichée pendant l’examen.')
        return true
    } catch (e: unknown) {
        setStatus(extractApiMessage(e, 'Impossible d’enregistrer la réponse pour la question active.'))
        return false
    } finally {
        setSubmittingAnswer(false)
    }
}

export default function ExamenPassageWeb() {
    const navigate = useNavigate()
    const location = useLocation()
    const { examenId } = useParams()
    const id = Number(examenId)
    const isEpreuve = deriveIsEpreuvePath(location.pathname)

    const [meta, setMeta] = useState<ExamenMeta | null>(null)
    const [snap, setSnap] = useState<ExamenSnapshot | null>(null)
    const [joined, setJoined] = useState(false)
    const [status, setStatus] = useState<string>('')
    const [loading, setLoading] = useState(true)
    const [selectedResponseId, setSelectedResponseId] = useState<number | null>(null)
    const [lastAnsweredQuestionId, setLastAnsweredQuestionId] = useState<number | null>(null)
    const [submittingAnswer, setSubmittingAnswer] = useState(false)
    const [wsNotice, setWsNotice] = useState<string | null>(null)
    const [creneauTick, setCreneauTick] = useState(0)
    const autoJoinReussi = useRef(false)

    useCreneauTicker(setCreneauTick)
    useExamenMetaLoad(id, setMeta, setLoading, setStatus)
    useResetAutoJoinOnId(id, autoJoinReussi)
    useExamenPolling(id, meta, setJoined, setSnap)
    useExamenStomp(id, setSnap, setJoined, setWsNotice)
    useSnapClearsJoinedOnArrete(snap?.etat, id, setJoined)
    useNavigateVersEpreuveSurDemarrage(isEpreuve, snap?.etat, id)
    useNavigateEpreuveSiMetaDejaEnCours(loading, isEpreuve, meta?.statut, id)
    useRetourAttenteSiEpreuveTropTot(isEpreuve, snap, id)
    useRedirectDashboardSiArrete(snap?.etat)

    const debutMsPourJoin = meta ? parseDebutExamenMs(meta.dateDebut) : null
    const creneauOkPourJoin = debutMsPourJoin == null || Date.now() >= debutMsPourJoin

    useAutoJoinSalleAttente({
        id,
        meta,
        creneauOkPourJoin,
        creneauTick,
        snapEtat: snap?.etat,
        autoJoinReussi,
        setJoined,
        setStatus,
    })
    useJoinedSiSessionActive(snap?.etat, setJoined)

    const enPausePourTemps =
        Boolean(snap?.enPause) || (snap?.etat ?? '').trim().toUpperCase() === 'EN_PAUSE'
    const tempsRestantAffiche = useExamenTempsRestantLive(
        snap?.tempsRestantMinutes,
        snap?.etat ?? meta?.statut ?? '',
        enPausePourTemps,
    )
    const minuteurQuestion = useExamenMinuteurQuestionLive(
        snap?.tempsQuestionRestantSeconds,
        snap?.etat ?? meta?.statut ?? '',
        enPausePourTemps,
    )

    const debutMsAffichage = parseDebutExamenMs(meta?.dateDebut)
    const creneauAtteint = useMemo(
        () => debutMsAffichage == null || Date.now() >= debutMsAffichage,
        [debutMsAffichage, creneauTick],
    )

    const phaseSession = (snap?.etat ?? '').trim().toUpperCase()
    const canStart = phaseSession === 'EN_COURS'
    const enPause = phaseSession === 'EN_PAUSE'
    const sessionTerminee =
        phaseSession === 'TERMINE' || (meta?.statut ?? '').trim().toUpperCase() === 'TERMINE'
    const etat = snap?.etat ?? meta?.statut ?? 'PLANIFIE'
    const etudiantId = readEtudiantId()
    const questionCourante = snap?.questionCourante
    const questionCouranteAvecReponses = questionCourante as
        | (typeof questionCourante & { reponses?: Array<{ id?: number; contenu?: string }> })
        | undefined
    const questionId = typeof questionCourante?.id === 'number' ? questionCourante.id : null
    const reponses = Array.isArray(questionCouranteAvecReponses?.reponses)
        ? (questionCouranteAvecReponses.reponses as Array<{ id?: number; contenu?: string }>)
        : []

    useQuestionSelectionClear(questionId, lastAnsweredQuestionId, setSelectedResponseId)

    if (!Number.isFinite(id) || id <= 0) {
        return <ExamenPassageInvalid />
    }

    const soumettreReponseCourante = async () => {
        if (minuteurQuestion.isExpired) {
            setStatus(
                'Le temps pour cette question est écoulé. Vous ne pouvez plus enregistrer de réponse tant que le professeur n’a pas ajouté du temps au minuteur.',
            )
            return
        }
        const ok = await soumettreReponseVersApi({
            id,
            etudiantId,
            questionId,
            selectedResponseId,
            canStart,
            enPause,
            setSubmittingAnswer,
            setLastAnsweredQuestionId,
            setStatus,
        })
        if (ok) {
            try {
                const bc = new BroadcastChannel(`smartest.examen.${id}`)
                bc.postMessage({ type: 'reponse-enregistree', examenId: id })
                bc.close()
            } catch {
                /* BroadcastChannel indisponible */
            }
        }
    }

    const heureLancement = formatTime(meta?.dateDebut)
    const dateExamen = formatDateTime(meta?.dateDebut)

    if (loading) {
        return <ExamenPassageLoading />
    }

    const shell: CSSProperties = {
        width: '100%',
        maxWidth: 920,
        margin: '0 auto',
        fontFamily: sans,
        color: '#0f1e3d',
        display: 'flex',
        flexDirection: 'column',
        gap: 18,
    }

    if (sessionTerminee) {
        return (
            <ExamenPassageTerminee
                shell={shell}
                meta={meta}
                id={id}
                onDashboard={() => navigate('/dashboard', { replace: true })}
            />
        )
    }

    const questionHeading =
        snap?.questionCourante?.numero != null && typeof snap.questionCourante.numero === 'number'
            ? `Question ${snap.questionCourante.numero}`
            : 'Question en cours'

    const blocQuestionEl = (
        <ExamenBlocQuestion
            enPause={enPause}
            canStart={canStart}
            snap={snap}
            questionCourante={questionCourante}
            reponses={reponses}
            questionId={questionId}
            selectedResponseId={selectedResponseId}
            setSelectedResponseId={setSelectedResponseId}
            submittingAnswer={submittingAnswer}
            tempsQuestionExpire={minuteurQuestion.isExpired}
            onValider={soumettreReponseCourante}
        />
    )

    if (isEpreuve) {
        return (
            <ExamenPassageEpreuveLayout
                shell={shell}
                meta={meta}
                id={id}
                tempsRestantAffiche={tempsRestantAffiche}
                canStart={canStart}
                etat={etat}
                status={status}
                wsNotice={wsNotice}
                questionHeading={questionHeading}
                minuteurQuestion={minuteurQuestion}
                blocQuestion={blocQuestionEl}
            />
        )
    }

    return (
        <ExamenPassageAttenteLayout
            shell={shell}
            meta={meta}
            id={id}
            canStart={canStart}
            etat={etat}
            dateExamen={dateExamen}
            creneauAtteint={creneauAtteint}
            joined={joined}
            heureLancement={heureLancement}
            status={status}
            wsNotice={wsNotice}
        />
    )
}
