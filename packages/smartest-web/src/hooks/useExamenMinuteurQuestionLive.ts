import { useEffect, useRef, useState } from 'react'

function formatMmSs(totalSeconds: number): string {
    const s = Math.max(0, Math.floor(totalSeconds))
    const m = Math.floor(s / 60)
    const r = s % 60
    return `${m}:${String(r).padStart(2, '0')}`
}

export type ExamenMinuteurQuestionLive = {
    /** Libellé mm:ss pour l’affichage, ou null si le serveur ne fournit pas de minuteur question. */
    formatted: string | null
    /** Secondes restantes (décompte local entre synchros), null si N/A. */
    secondsRemaining: number | null
    /**
     * EN_COURS et temps question épuisé : l’étudiant ne doit plus pouvoir enregistrer ou changer de réponse
     * jusqu’à ce que le professeur ajoute du temps au minuteur (ou change de question).
     */
    isExpired: boolean
}

/**
 * Minuteur de la question courante (secondes serveur), décompte local entre les synchros.
 * Pause : fige l’affichage comme le minuteur global d’examen.
 */
export function useExamenMinuteurQuestionLive(
    tempsQuestionRestantSeconds: number | undefined | null,
    etatPhase: string | undefined | null,
    enPause: boolean,
): ExamenMinuteurQuestionLive {
    const [tick, setTick] = useState(0)
    const deadlineMsRef = useRef<number | null>(null)
    const frozenRemainingMsRef = useRef<number | null>(null)
    const lastServerSecRef = useRef<number | undefined>(undefined)
    const skipNextServerDeadlineSyncRef = useRef(false)
    const prevActifRef = useRef(false)

    const phase = (etatPhase ?? '').trim().toUpperCase()
    const actif = phase === 'EN_COURS' && !enPause

    useEffect(() => {
        const wasActive = prevActifRef.current
        prevActifRef.current = actif

        const entreEnPause = wasActive && !actif && enPause

        if (entreEnPause && deadlineMsRef.current != null) {
            frozenRemainingMsRef.current = Math.max(0, deadlineMsRef.current - Date.now())
            setTick((n) => n + 1)
            return
        }

        const reprend = !wasActive && actif && phase === 'EN_COURS'

        if (reprend && frozenRemainingMsRef.current != null) {
            const restant = frozenRemainingMsRef.current
            deadlineMsRef.current = Date.now() + restant
            frozenRemainingMsRef.current = null
            skipNextServerDeadlineSyncRef.current = true
            setTick((n) => n + 1)
        }
    }, [actif, enPause, phase])

    useEffect(() => {
        if (typeof tempsQuestionRestantSeconds !== 'number' || !Number.isFinite(tempsQuestionRestantSeconds)) {
            deadlineMsRef.current = null
            frozenRemainingMsRef.current = null
            lastServerSecRef.current = undefined
            return
        }

        if (skipNextServerDeadlineSyncRef.current) {
            skipNextServerDeadlineSyncRef.current = false
            lastServerSecRef.current = tempsQuestionRestantSeconds
            return
        }

        const phaseSync = (etatPhase ?? '').trim().toUpperCase()
        const enPauseSync = enPause || phaseSync === 'EN_PAUSE'
        if (enPauseSync && frozenRemainingMsRef.current != null) {
            lastServerSecRef.current = tempsQuestionRestantSeconds
            return
        }

        if (lastServerSecRef.current !== tempsQuestionRestantSeconds) {
            deadlineMsRef.current = Date.now() + Math.max(0, tempsQuestionRestantSeconds) * 1000
            lastServerSecRef.current = tempsQuestionRestantSeconds
            if (!enPauseSync) {
                frozenRemainingMsRef.current = null
            }
        }
    }, [tempsQuestionRestantSeconds, enPause, etatPhase])

    useEffect(() => {
        if (!actif || deadlineMsRef.current == null) return
        const id = window.setInterval(() => setTick((n) => n + 1), 1000)
        return () => window.clearInterval(id)
    }, [actif, tempsQuestionRestantSeconds])

    void tick

    if (typeof tempsQuestionRestantSeconds !== 'number' || !Number.isFinite(tempsQuestionRestantSeconds)) {
        return { formatted: null, secondsRemaining: null, isExpired: false }
    }

    const enPauseEffective = enPause || phase === 'EN_PAUSE'

    let remainingSec: number
    if (enPauseEffective && frozenRemainingMsRef.current != null) {
        remainingSec = frozenRemainingMsRef.current / 1000
    } else if (!actif) {
        remainingSec = Math.max(0, tempsQuestionRestantSeconds)
    } else if (deadlineMsRef.current == null) {
        remainingSec = Math.max(0, tempsQuestionRestantSeconds)
    } else {
        remainingSec = Math.max(0, (deadlineMsRef.current - Date.now()) / 1000)
    }

    const formatted = formatMmSs(remainingSec)
    const isExpired = actif && remainingSec <= 0

    return {
        formatted,
        secondsRemaining: remainingSec,
        isExpired,
    }
}
