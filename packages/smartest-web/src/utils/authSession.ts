/** Clé unique du JWT côté web (alignée avec useAuth et axiosConfig). */
export const TOKEN_KEY = 'token' as const

export const AUTH_STORAGE_KEYS = ['token', 'role', 'nom', 'email', 'userId'] as const

export function readAccessToken(): string | null {
    try {
        return localStorage.getItem(TOKEN_KEY)
    } catch {
        return null
    }
}

export function writeAccessToken(token: string): void {
    try {
        localStorage.setItem(TOKEN_KEY, token)
    } catch {
        /* quota / navigation privée : l’état mémoire (Zustand) peut continuer */
    }
}

export function clearAuthStorage(): void {
    for (const key of AUTH_STORAGE_KEYS) {
        try {
            localStorage.removeItem(key)
        } catch {
            /* quota / navigation privée */
        }
    }
}

function decodeJwtPayload(token: string): Record<string, unknown> | null {
    try {
        const payload = token.split('.')[1]
        if (!payload) return null
        return JSON.parse(atob(payload.replaceAll('-', '+').replaceAll('_', '/'))) as Record<string, unknown>
    } catch {
        return null
    }
}

/** Identifiant étudiant : claim JWT `userId` en priorité ; 0 seulement si pas de token. */
export function resolveEtudiantIdFromSession(authUserId?: string | null): number {
    const token = readAccessToken()
    if (!token) return 0

    const decoded = decodeJwtPayload(token)
    if (decoded) {
        const fromJwt = Number(decoded.userId ?? decoded.id)
        if (Number.isFinite(fromJwt) && fromJwt > 0) return Math.trunc(fromJwt)
    }

    const fromAuth = authUserId ? Number.parseInt(authUserId, 10) : Number.NaN
    if (Number.isFinite(fromAuth) && fromAuth > 0) return fromAuth

    try {
        const raw = localStorage.getItem('userId')
        const n = Number(raw)
        if (Number.isFinite(n) && n > 0) return n
    } catch {
        /* ignore */
    }
    return 0
}

/** Email de session : claim JWT `sub` puis localStorage ; jamais de valeur fictive. */
export function readSessionEmail(): string {
    const token = readAccessToken()
    if (token) {
        const decoded = decodeJwtPayload(token)
        const sub = decoded?.sub
        if (typeof sub === 'string' && sub.trim()) return sub.trim().toLowerCase()
        const claimEmail = decoded?.email
        if (typeof claimEmail === 'string' && claimEmail.trim()) return claimEmail.trim().toLowerCase()
    }
    try {
        const email = localStorage.getItem('email')
        if (email?.trim()) return email.trim().toLowerCase()
    } catch {
        /* ignore */
    }
    return ''
}
