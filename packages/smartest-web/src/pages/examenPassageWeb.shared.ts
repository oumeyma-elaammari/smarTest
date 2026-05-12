import axios from 'axios'

export function joinedStorageKey(examenId: number): string {
    return `smartest.examen.web.joined.${examenId}`
}

/** Id entité (question / réponse) : le JSON peut envoyer nombre ou chaîne selon la source. */
export function coerceEntityId(value: unknown): number | null {
    if (typeof value === 'number' && Number.isFinite(value) && value > 0) return Math.trunc(value)
    const n = Number(value)
    return Number.isFinite(n) && n > 0 ? Math.trunc(n) : null
}

/**
 * Identifiant étudiant pour les appels `/passage/*` (paramètre `etudiantId`).
 * Le JWT SmarTest met l’email dans `sub` (pas un id) : il faut le claim `userId` ou le stockage post-login.
 */
export function readEtudiantId(): number {
    try {
        const token = localStorage.getItem('token')
        if (token) {
            const payload = token.split('.')[1]
            if (payload) {
                const decoded = JSON.parse(
                    atob(payload.replaceAll('-', '+').replaceAll('_', '/')),
                ) as { userId?: unknown; id?: unknown }
                const fromJwt = Number(decoded.userId ?? decoded.id)
                if (Number.isFinite(fromJwt) && fromJwt > 0) return fromJwt
            }
        }
    } catch {
        /* JWT illisible */
    }
    try {
        const raw = localStorage.getItem('userId')
        const n = Number(raw)
        if (Number.isFinite(n) && n > 0) return n
    } catch {
        /* ignore */
    }
    return 0
}

export function extractApiMessage(e: unknown, fallback: string): string {
    if (axios.isAxiosError(e)) {
        const d = e.response?.data
        if (typeof d === 'string' && d.trim()) return d.trim()
        if (d && typeof d === 'object' && 'message' in d) {
            const m = (d as { message?: unknown }).message
            if (typeof m === 'string' && m.trim()) return m.trim()
        }
        if (d && typeof d === 'object' && 'error' in d) {
            const err = (d as { error?: unknown }).error
            if (typeof err === 'string' && err.trim()) return err.trim()
        }
    }
    if (e instanceof Error && e.message.trim()) return e.message.trim()
    return fallback
}
