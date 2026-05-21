import type { CSSProperties } from 'react'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { examenApi } from '../api/examenApi'
import type { ExamenMeta, ExamenSnapshot } from '../api/quizSchemas'
import {
    formatDateTime,
    formatTime,
    parseDebutExamenMs,
} from '../utils/examenDisplay'
import { useExamenMinuteurQuestionLive } from '../hooks/useExamenMinuteurQuestionLive'
import { useExamenWebSocketReponse } from '../hooks/useExamenWebSocketReponse'
import {
    useAutoJoinSalleAttente,
    useCreneauTicker,
    useExamenMetaLoad,
    useExamenPolling,
    useExamenStomp,
    useJoinedSiSessionActive,
    useNavigateEpreuveSiMetaDejaEnCours,
    useNavigateVersEpreuveSurDemarrage,
    useExamAnswerReset,
    useRedirectDashboardSiArrete,
    useSoumettreFinalSiTermine,
    useResetAutoJoinOnId,
    useRetourAttenteSiEpreuveTropTot,
    useSnapClearsJoinedOnArrete,
    deriveIsEpreuvePath,
} from './examenPassageWeb.hooks'
import useAuth from '../hooks/useAuth'
import {
    coerceEntityId,
    examPassQuestionKind,
    extractApiMessage,
    reponseVerrouilleeStorageKey,
    resolveEtudiantId,
} from './examenPassageWeb.shared'
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
    questionKind: ReturnType<typeof examPassQuestionKind>
    selectedResponseId: number | null
    selectedResponseIds: number[]
    essayText: string
    canStart: boolean
    enPause: boolean
    setSubmittingAnswer: (v: boolean) => void
    setStatus: (v: string) => void
}): Promise<boolean> {
    const {
        id,
        etudiantId,
        questionId,
        questionKind,
        selectedResponseId,
        selectedResponseIds,
        essayText,
        canStart,
        enPause,
        setSubmittingAnswer,
        setStatus,
    } = opts

    if (!Number.isFinite(id) || id <= 0) return false
    if (!Number.isFinite(etudiantId) || etudiantId <= 0) {
        const msg =
            'Session incomplète : identifiant étudiant introuvable. Déconnectez-vous puis reconnectez-vous.'
        console.error('REPONSE NON ENREGISTREE: etudiantId invalide', etudiantId)
        setStatus(msg)
        return false
    }
    if (!canStart || enPause) {
        setStatus(
            enPause
                ? 'L’examen est en pause : vous ne pouvez pas valider de réponse pour le moment.'
                : 'L’épreuve n’a pas encore démarré ou est terminée. Attendez que le professeur lance la session.',
        )
        return false
    }
    if (questionId === null) {
        setStatus('Question introuvable. Actualisez la page si le problème persiste.')
        return false
    }

    let body: { questionId: number; reponseId?: number; reponseIds?: number[]; reponseTexte?: string }
    if (questionKind === 'essay') {
        const t = essayText.trim()
        if (!t) {
            setStatus('Rédigez une réponse avant de valider.')
            return false
        }
        body = { questionId, reponseTexte: t }
    } else if (questionKind === 'checkbox') {
        if (selectedResponseIds.length === 0) {
            setStatus('Cochez au moins une réponse avant de valider.')
            return false
        }
        body = { questionId, reponseIds: [...selectedResponseIds] }
    } else {
        if (selectedResponseId === null) {
            setStatus('Sélectionnez une réponse avant de valider.')
            return false
        }
        body = { questionId, reponseId: selectedResponseId }
    }

    try {
        setSubmittingAnswer(true)
        await examenApi.repondreQuestionCourante(id, etudiantId, body)
        setStatus('Réponse enregistrée. Aucune correction immédiate n’est affichée pendant l’examen.')
        return true
    } catch (e: unknown) {
        console.error('REPONSE NON ENREGISTREE:', e)
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
    const [selectedResponseIds, setSelectedResponseIds] = useState<number[]>([])
    const [essayText, setEssayText] = useState('')
    const [submittingAnswer, setSubmittingAnswer] = useState(false)
    const [reponseVerrouillee, setReponseVerrouillee] = useState(false)
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
    const authUserId = useAuth((s) => s.userId)
    const etudiantId = resolveEtudiantId(authUserId)
    useSoumettreFinalSiTermine(id, snap?.etat, meta?.statut, setStatus)
    const questionCourante = snap?.questionCourante
    const questionId = coerceEntityId(questionCourante?.id)
    const questionCouranteAvecMeta = questionCourante as
        | (typeof questionCourante & {
              reponses?: Array<{ id?: number; contenu?: string }>
              type?: string | null
          })
        | undefined
    const questionKind = examPassQuestionKind(
        typeof questionCouranteAvecMeta?.type === 'string' ? questionCouranteAvecMeta.type : undefined,
    )
    const reponses = Array.isArray(questionCouranteAvecMeta?.reponses)
        ? (questionCouranteAvecMeta.reponses as Array<{ id?: number; contenu?: string }>)
        : []

    const resetAnswerState = useCallback(() => {
        setSelectedResponseId(null)
        setSelectedResponseIds([])
        setEssayText('')
        setReponseVerrouillee(false)
    }, [])

    useExamAnswerReset(questionId, resetAnswerState)

    useEffect(() => {
        if (questionId == null) {
            setReponseVerrouillee(false)
            return
        }
        let verrou = false
        try {
            verrou = sessionStorage.getItem(reponseVerrouilleeStorageKey(id, questionId)) === '1'
        } catch {
            /* ignore */
        }
        if (snap?.reponseVerrouillee === true) {
            verrou = true
        }
        if (verrou || snap?.reponseVerrouillee === true) {
            const rid = coerceEntityId(snap?.reponseIdSelectionnee)
            if (rid != null) setSelectedResponseId(rid)
            const ids = (snap?.reponseIdsSelectionnees ?? [])
                .map((x) => coerceEntityId(x))
                .filter((x): x is number => x != null)
            if (ids.length > 0) setSelectedResponseIds(ids)
            const txt = typeof snap?.reponseTexte === 'string' ? snap.reponseTexte : ''
            if (txt.trim()) setEssayText(txt)
        }
        const locked = verrou || snap?.reponseVerrouillee === true
        setReponseVerrouillee(locked)
        if (locked) {
            try {
                sessionStorage.setItem(reponseVerrouilleeStorageKey(id, questionId), '1')
            } catch {
                /* ignore */
            }
        }
    }, [id, questionId, snap?.reponseVerrouillee, snap?.reponseIdSelectionnee, snap?.reponseIdsSelectionnees, snap?.reponseTexte])

    const { envoyerReponseWebSocket } = useExamenWebSocketReponse()

    if (!Number.isFinite(id) || id <= 0) {
        return <ExamenPassageInvalid />
    }

    const soumettreReponseCourante = async () => {
        if (!Number.isFinite(etudiantId) || etudiantId <= 0) {
            setStatus('Identifiant étudiant manquant : déconnectez-vous puis reconnectez-vous.')
            return
        }
        if (reponseVerrouillee) {
            setStatus('Vous avez déjà validé votre réponse pour cette question.')
            return
        }
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
            questionKind,
            selectedResponseId,
            selectedResponseIds,
            essayText,
            canStart,
            enPause,
            setSubmittingAnswer,
            setStatus,
        })
        if (!ok) return

        setReponseVerrouillee(true)
        if (questionId != null) {
            try {
                sessionStorage.setItem(reponseVerrouilleeStorageKey(id, questionId), '1')
            } catch {
                /* ignore */
            }
        }

        // Diffusion temps réel après persistance serveur (compteurs supervision).
        if (questionId) {
            const email =
                typeof globalThis.localStorage !== 'undefined'
                    ? globalThis.localStorage.getItem('email')
                    : null
            if (questionKind === 'essay' && essayText.trim()) {
                envoyerReponseWebSocket({
                    examenId: id,
                    etudiantId,
                    questionId,
                    email,
                    reponseTexte: essayText.trim(),
                })
            } else if (questionKind === 'checkbox' && selectedResponseIds.length > 0) {
                envoyerReponseWebSocket({
                    examenId: id,
                    etudiantId,
                    questionId,
                    email,
                    reponseIds: selectedResponseIds,
                })
            } else if (selectedResponseId) {
                envoyerReponseWebSocket({
                    examenId: id,
                    etudiantId,
                    questionId,
                    email,
                    reponseId: selectedResponseId,
                })
            }
        }

        try {
                const bc = new BroadcastChannel(`smartest.examen.${id}`)
                bc.postMessage({ type: 'reponse-enregistree', examenId: id })
                bc.close()
            } catch {
                /* BroadcastChannel indisponible */
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

    const hasAnswer =
        questionKind === 'checkbox'
            ? selectedResponseIds.length > 0
            : questionKind === 'essay'
              ? essayText.trim().length > 0
              : selectedResponseId != null
    const inputsLocked = reponseVerrouillee || minuteurQuestion.isExpired
    const submitDisabled = submittingAnswer || !hasAnswer || inputsLocked || !canStart || enPause

    const blocQuestionEl = (
        <ExamenBlocQuestion
            enPause={enPause}
            canStart={canStart}
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
            tempsQuestionExpire={minuteurQuestion.isExpired}
            onValider={soumettreReponseCourante}
        />
    )

    if (isEpreuve) {
        return (
            <ExamenPassageEpreuveLayout
                shell={shell}
                meta={meta}
                snap={snap}
                id={id}
                canStart={canStart}
                etat={etat}
                status={status}
                wsNotice={wsNotice}
                questionHeading={questionHeading}
                minuteurQuestion={minuteurQuestion}
                blocQuestion={blocQuestionEl}
                afficherValider={canStart && Boolean(questionCourante) && !enPause}
                onValider={soumettreReponseCourante}
                submittingAnswer={submittingAnswer}
                reponseVerrouillee={reponseVerrouillee}
                submitDisabled={submitDisabled}
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
