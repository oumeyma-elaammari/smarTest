import { examenApi } from '../api/examenApi'
import { resolveEtudiantIdFromSession, readSessionEmail } from '../utils/authSession'
import { getUserErrorMessage } from '../utils/userErrorMessage'

export function joinedStorageKey(examenId: number): string {
    return `smartest.examen.web.joined.${examenId}`
}

export function soumisFinalStorageKey(examenId: number): string {
    return `smartest.examen.web.soumis-final.${examenId}`
}

export function reponseVerrouilleeStorageKey(examenId: number, questionId: number): string {
    return `smartest.examen.web.reponse-validated.${examenId}.${questionId}`
}

/** Id entité (question / réponse) : le JSON peut envoyer nombre ou chaîne selon la source. */
export function coerceEntityId(value: unknown): number | null {
    if (typeof value === 'number' && Number.isFinite(value) && value > 0) return Math.trunc(value)
    const n = Number(value)
    return Number.isFinite(n) && n > 0 ? Math.trunc(n) : null
}

/** Type de question pour l’UI de passage (aligné sur l’énumération backend / le bureau). */
export function examPassQuestionKind(type: string | null | undefined): 'qcm' | 'vf' | 'checkbox' | 'essay' {
    const t = (type ?? '').toUpperCase()
    if (t === 'VRAI_FAUX' || t === 'VF') return 'vf'
    if (t === 'CASES_A_COCHER' || t === 'CHECKBOX') return 'checkbox'
    if (t === 'REDACTION' || t === 'REPONSE_COURTE' || t === 'DISSERTATION' || t === 'ESSAY' || t === 'LIBRE') {
        return 'essay'
    }
    return 'qcm'
}

/**
 * Identifiant étudiant pour les appels `/passage/*` (paramètre `etudiantId`).
 * Le JWT SmarTest met l’email dans `sub` (pas un id) : il faut le claim `userId` ou le stockage post-login.
 */
/** Priorité : claim `userId` du store auth, puis JWT / localStorage. */
export function resolveEtudiantId(authUserId?: string | null): number {
    return resolveEtudiantIdFromSession(authUserId)
}

export function readEtudiantId(): number {
    return resolveEtudiantIdFromSession(null)
}

export function readEtudiantEmail(): string {
    return readSessionEmail()
}

export function extractApiMessage(e: unknown, fallback: string): string {
    return getUserErrorMessage(e, fallback)
}

function sleep(ms: number): Promise<void> {
    return new Promise((resolve) => {
        globalThis.setTimeout(resolve, ms)
    })
}

function isDejaSoumisMessage(msg: string): boolean {
    const m = msg.toLowerCase()
    return m.includes('déjà') || m.includes('deja') || m.includes('already') || m.includes('dejasoumis')
}

function isAttenteFinSessionMessage(msg: string): boolean {
    const m = msg.toLowerCase()
    return (
        m.includes('fin de session') ||
        m.includes('après la fin') ||
        m.includes('apres la fin') ||
        m.includes('pas encore') ||
        m.includes('not yet')
    )
}

export function isSessionPhaseTerminee(snapEtat?: string | null, metaStatut?: string | null): boolean {
    const phase = (snapEtat ?? '').trim().toUpperCase()
    const meta = (metaStatut ?? '').trim().toUpperCase()
    return phase === 'TERMINE' || meta === 'TERMINE'
}

const soumissionEnCours = new Map<string, Promise<boolean>>()

/**
 * POST soumettre-final avec reprises si le snapshot WS arrive avant la phase runtime TERMINE.
 */
export async function soumettreExamenFinalAvecRetry(examenId: number, etudiantId: number): Promise<boolean> {
    if (!Number.isFinite(examenId) || examenId <= 0 || !Number.isFinite(etudiantId) || etudiantId <= 0) {
        return false
    }
    const key = `${examenId}:${etudiantId}`
    const existing = soumissionEnCours.get(key)
    if (existing) return existing

    const task = (async () => {
        try {
            if (sessionStorage.getItem(soumisFinalStorageKey(examenId)) === '1') return true
        } catch {
            /* ignore */
        }

        for (let attempt = 0; attempt < 20; attempt++) {
            try {
                await examenApi.soumettreFinal(examenId, etudiantId)
                try {
                    sessionStorage.setItem(soumisFinalStorageKey(examenId), '1')
                } catch {
                    /* ignore */
                }
                return true
            } catch (e: unknown) {
                const msg = extractApiMessage(e, '')
                if (isDejaSoumisMessage(msg)) {
                    try {
                        sessionStorage.setItem(soumisFinalStorageKey(examenId), '1')
                    } catch {
                        /* ignore */
                    }
                    return true
                }
                if (attempt < 19 && isAttenteFinSessionMessage(msg)) {
                    await sleep(1500)
                    continue
                }
                throw e
            }
        }
        return false
    })().finally(() => {
        soumissionEnCours.delete(key)
    })

    soumissionEnCours.set(key, task)
    return task
}
