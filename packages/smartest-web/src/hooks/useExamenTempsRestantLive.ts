import { useEffect, useRef, useState } from 'react'

function formatMmSs(totalSeconds: number): string {
    const s = Math.max(0, Math.floor(totalSeconds))
    const m = Math.floor(s / 60)
    const r = s % 60
    return `${m}:${String(r).padStart(2, '0')}`
}

/**
 * Temps restant basé sur une échéance locale ; décrémente chaque seconde en EN_COURS.
 * À la pause : fige la valeur affichée (pas les minutes entières serveur qui peuvent sembler « reset »).
 * À la reprise : continue depuis ce temps figé (sans repartir de la durée initiale serveur).
 */
export function useExamenTempsRestantLive(
    tempsRestantMinutes: number | undefined | null,
    etatPhase: string | undefined | null,
    enPause: boolean,
): string | null {
    const [tick, setTick] = useState(0)
    /** Date limite du décompte (timestamps absolus). */
    const deadlineMsRef = useRef<number | null>(null)
    /** Temps restant figé en ms quand l’examen est en pause (affiche mm:ss stable). */
    const frozenRemainingMsRef = useRef<number | null>(null)
    const lastServerMinRef = useRef<number | undefined>(undefined)
    /** Ignore un sync serveur juste après reprise (évite d’écraser avec la durée pleine). */
    const skipNextServerDeadlineSyncRef = useRef(false)
    const prevActifRef = useRef(false)

    const phase = (etatPhase ?? '').trim().toUpperCase()
    const actif = phase === 'EN_COURS' && !enPause

    /** Pause → fige le restant réel ; reprise → réinjecte dans l’échéance locale (avant sync serveur). */
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

    /** Sync échéance quand le serveur envoie une nouvelle valeur en minutes (lancement, ajustement prof). */
    useEffect(() => {
        if (typeof tempsRestantMinutes !== 'number' || !Number.isFinite(tempsRestantMinutes)) {
            deadlineMsRef.current = null
            frozenRemainingMsRef.current = null
            lastServerMinRef.current = undefined
            return
        }

        if (skipNextServerDeadlineSyncRef.current) {
            skipNextServerDeadlineSyncRef.current = false
            lastServerMinRef.current = tempsRestantMinutes
            return
        }

        const phaseSync = (etatPhase ?? '').trim().toUpperCase()
        const enPauseSync = enPause || phaseSync === 'EN_PAUSE'
        /** Pendant la pause : ne pas réinjecter les minutes serveur (souvent la durée pleine). */
        if (enPauseSync && frozenRemainingMsRef.current != null) {
            lastServerMinRef.current = tempsRestantMinutes
            return
        }

        if (lastServerMinRef.current !== tempsRestantMinutes) {
            deadlineMsRef.current = Date.now() + tempsRestantMinutes * 60 * 1000
            lastServerMinRef.current = tempsRestantMinutes
            if (!enPauseSync) {
                frozenRemainingMsRef.current = null
            }
        }
    }, [tempsRestantMinutes, enPause, etatPhase])

    useEffect(() => {
        if (!actif || deadlineMsRef.current == null) return
        const id = window.setInterval(() => setTick((n) => n + 1), 1000)
        return () => window.clearInterval(id)
    }, [actif, tempsRestantMinutes])

    void tick

    if (typeof tempsRestantMinutes !== 'number' || !Number.isFinite(tempsRestantMinutes)) {
        return null
    }

    const enPauseEffective = enPause || phase === 'EN_PAUSE'

    if (enPauseEffective && frozenRemainingMsRef.current != null) {
        return formatMmSs(frozenRemainingMsRef.current / 1000)
    }

    if (!actif) {
        return `${tempsRestantMinutes} min`
    }

    if (deadlineMsRef.current == null) {
        return `${tempsRestantMinutes} min`
    }

    const remainingSec = Math.max(0, (deadlineMsRef.current - Date.now()) / 1000)
    return formatMmSs(remainingSec)
}
